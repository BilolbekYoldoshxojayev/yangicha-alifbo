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
    private var targetPid: pid_t = NSWorkspace.shared.frontmostApplication?.processIdentifier ?? 0
    private let source = CGEventSource(stateID: .privateState)
    var onToggle: (() -> Void)?
    private(set) var wordsConverted = 0
    private var diagnostics = 0

    init(engine: Engine, settings: Settings) {
        self.engine = engine
        self.settings = settings
        NSWorkspace.shared.notificationCenter.addObserver(forName: NSWorkspace.didActivateApplicationNotification, object: nil, queue: .main) { [weak self] n in
            let app = n.userInfo?[NSWorkspace.applicationUserInfoKey] as? NSRunningApplication
            let id = app?.bundleIdentifier ?? ""
            self?.targetPid = app?.processIdentifier ?? 0
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
        if diagnostics < 3 {   // first keys after start, for support logs (no characters logged)
            diagnostics += 1
            Log.write("key: enabled=\(settings.enabled) skippedApp=\(skipped) secureInput=\(IsSecureEventInputEnabled()) user=\(event.getIntegerValueField(.eventSourceUserData))")
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
        for ch in text {   // one character per event, as if typed
            let units = Array(String(ch).utf16)
            for down in [true, false] {
                guard let e = CGEvent(keyboardEventSource: source, virtualKey: 0, keyDown: down) else { continue }
                e.flags = []
                e.keyboardSetUnicodeString(stringLength: units.count, unicodeString: units)
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

    /// Sends a corrective event straight to the app being typed in. Posting into the
    /// system-wide keyboard stream instead would let macOS treat a key the user is still
    /// holding as long-pressed and open the accent picker (ß ş ș ś š).
    private func mark(_ e: CGEvent) {
        e.setIntegerValueField(.eventSourceUserData, value: KeyWatcher.magic)
        e.setIntegerValueField(.keyboardEventAutorepeat, value: 0)
        let pid = NSWorkspace.shared.frontmostApplication?.processIdentifier ?? targetPid
        if pid > 0 { e.postToPid(pid) } else { e.post(tap: .cghidEventTap) }
    }
}

private func tapCallback(proxy: CGEventTapProxy, type: CGEventType, event: CGEvent, refcon: UnsafeMutableRawPointer?) -> Unmanaged<CGEvent>? {
    guard let refcon = refcon else { return Unmanaged.passUnretained(event) }
    return Unmanaged<KeyWatcher>.fromOpaque(refcon).takeUnretainedValue().handle(type, event)
}
