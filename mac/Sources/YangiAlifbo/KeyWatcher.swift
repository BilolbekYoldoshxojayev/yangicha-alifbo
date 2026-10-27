// Watches keys in all apps (Quartz event tap, needs Accessibility permission) and,
// when a word ends, deletes it and types it in the new alphabet: the Windows
// keyboard hook, on macOS. Password fields are protected by macOS itself (secure
// input stops the tap); terminals and code editors are skipped, as on Windows.

import AppKit
import Carbon.HIToolbox
import YangiAlifboKit

final class KeyWatcher {
    /// Marks events we post (same value as the Windows dwExtraInfo) so we never re-handle them.
    static let magic: Int64 = 0x5941_4E47
    static let skippedApps: [String] = [
        "com.apple.Terminal", "com.googlecode.iterm2", "dev.warp.Warp-Stable", "com.mitchellh.ghostty",
        "net.kovidgoyal.kitty", "io.alacritty", "com.github.wez.wezterm", "com.microsoft.VSCode",
        "com.microsoft.VSCodeInsiders", "com.todesktop.230313mzl4w4u92", "dev.zed.Zed", "com.apple.dt.Xcode",
        "com.sublimetext.4", "com.jetbrains.",
    ]

    private let engine: Engine
    private let settings: Settings
    private var buffer = WordBuffer()
    private var tap: CFMachPort?
    private var skipped = false
    private let source = CGEventSource(stateID: .privateState)
    var onToggle: (() -> Void)?
    private(set) var wordsConverted = 0

    init(engine: Engine, settings: Settings) {
        self.engine = engine
        self.settings = settings
        NSWorkspace.shared.notificationCenter.addObserver(forName: NSWorkspace.didActivateApplicationNotification, object: nil, queue: .main) { [weak self] n in
            let id = (n.userInfo?[NSWorkspace.applicationUserInfoKey] as? NSRunningApplication)?.bundleIdentifier ?? ""
            self?.skipped = KeyWatcher.skippedApps.contains { id.hasPrefix($0) }
            self?.buffer.reset()
        }
    }

    var isRunning: Bool { tap != nil }

    /// Creates the event tap; false while Accessibility permission is missing.
    @discardableResult
    func start() -> Bool {
        if tap != nil { return true }
        let mask = (1 << CGEventType.keyDown.rawValue) | (1 << CGEventType.leftMouseDown.rawValue)
            | (1 << CGEventType.rightMouseDown.rawValue) | (1 << CGEventType.otherMouseDown.rawValue)
        guard let t = CGEvent.tapCreate(tap: .cgSessionEventTap, place: .headInsertEventTap, options: .defaultTap,
                                        eventsOfInterest: CGEventMask(mask), callback: tapCallback,
                                        userInfo: Unmanaged.passUnretained(self).toOpaque()) else { return false }
        tap = t
        CFRunLoopAddSource(CFRunLoopGetMain(), CFMachPortCreateRunLoopSource(nil, t, 0), .commonModes)
        CGEvent.tapEnable(tap: t, enable: true)
        Log.write("event tap started")
        return true
    }

    func reset() { buffer.reset() }

    fileprivate func handle(_ type: CGEventType, _ event: CGEvent) -> Unmanaged<CGEvent>? {
        let pass = Unmanaged.passUnretained(event)
        switch type {
        case .tapDisabledByTimeout, .tapDisabledByUserInput:
            if let t = tap { CGEvent.tapEnable(tap: t, enable: true) }
            return pass
        case .keyDown: break
        default: buffer.reset(); return pass   // mouse click: the caret may have moved
        }
        if event.getIntegerValueField(.eventSourceUserData) == KeyWatcher.magic { return pass }

        let flags = event.flags
        let code = Int(event.getIntegerValueField(.keyboardEventKeycode))
        if code == kVK_ANSI_U && flags.contains(.maskControl) && flags.contains(.maskShift) && !flags.contains(.maskCommand) {
            DispatchQueue.main.async { self.onToggle?() }   // Control+Shift+U, as Ctrl+Shift+U on Windows
            return nil
        }
        guard settings.enabled, !skipped, !IsSecureEventInputEnabled() else { buffer.reset(); return pass }

        let kind: KeyKind
        switch code {
        case kVK_Delete: kind = .backspace
        case kVK_Return, kVK_ANSI_KeypadEnter, kVK_Tab: kind = .end
        default:
            var length = 0
            var chars = [UniChar](repeating: 0, count: 4)
            event.keyboardGetUnicodeString(maxStringLength: 4, actualStringLength: &length, unicodeString: &chars)
            let s = String(utf16CodeUnits: chars, count: length)
            if s.count == 1, let c = s.first, !(c.unicodeScalars.first?.properties.generalCategory == .control) { kind = .character(c) }
            else { kind = .other }
        }
        let shortcut = !flags.isDisjoint(with: [.maskCommand, .maskControl, .maskAlternate])
        let s = settings
        let action = buffer.key(kind, shortcut: shortcut) { [engine] in engine.convert($0, aggressive: s.aggressive, slang: s.slang, neural: s.neural) }
        guard case let .retype(count, text) = action, let replay = event.copy() else { return pass }

        // Hold this key, fix the word, then replay the key after it.
        for _ in 0..<count { post(key: CGKeyCode(kVK_Delete)) }
        let units = Array(text.utf16)
        for i in stride(from: 0, to: units.count, by: 20) {
            let chunk = Array(units[i..<min(i + 20, units.count)])
            for down in [true, false] {
                guard let e = CGEvent(keyboardEventSource: source, virtualKey: 0, keyDown: down) else { continue }
                e.flags = []
                e.keyboardSetUnicodeString(stringLength: chunk.count, unicodeString: chunk)
                mark(e)
            }
        }
        mark(replay)
        wordsConverted += 1
        Log.write("converted a word (\(count) → \(text.count) characters)")   // never the word itself
        return nil
    }

    private func post(key: CGKeyCode) {
        for down in [true, false] {
            guard let e = CGEvent(keyboardEventSource: source, virtualKey: key, keyDown: down) else { continue }
            e.flags = []
            mark(e)
        }
    }

    private func mark(_ e: CGEvent) {
        e.setIntegerValueField(.eventSourceUserData, value: KeyWatcher.magic)
        e.post(tap: .cghidEventTap)
    }
}

private func tapCallback(proxy: CGEventTapProxy, type: CGEventType, event: CGEvent, refcon: UnsafeMutableRawPointer?) -> Unmanaged<CGEvent>? {
    guard let refcon = refcon else { return Unmanaged.passUnretained(event) }
    return Unmanaged<KeyWatcher>.fromOpaque(refcon).takeUnretainedValue().handle(type, event)
}
