// Menu-bar app: "Ş" status item, settings menu, Accessibility onboarding, update checks.

import AppKit
import ServiceManagement
import YangiAlifboKit

final class AppDelegate: NSObject, NSApplicationDelegate {
    let settings = Settings()
    var engine: Engine!
    var watcher: KeyWatcher!
    private var item: NSStatusItem!
    private var permissionTimer: Timer?
    private var updateTimer: Timer?
    private var updateNote: String?

    func applicationDidFinishLaunching(_ n: Notification) {
        do { engine = try Engine() } catch {
            Log.write("engine: \(error)")
            alert("YangiAlifbo ishga tushmadi", "Lug‘at yuklanmadi: \(error.localizedDescription)")
            NSApp.terminate(nil); return
        }
        watcher = KeyWatcher(engine: engine, settings: settings)
        watcher.onToggle = { [weak self] in self?.toggle() }
        item = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        rebuildMenu()

        if !watcher.start() {
            let opts = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: false] as CFDictionary
            if !AXIsProcessTrustedWithOptions(opts) { askForPermission() }
            permissionTimer = Timer.scheduledTimer(withTimeInterval: 2, repeats: true) { [weak self] t in
                guard let self = self, self.watcher.start() else { return }
                t.invalidate(); self.rebuildMenu()
            }
        }
        if let note = settings.whatsNew {
            settings.whatsNew = nil
            alert("YangiAlifbo yangilandi", String(note.prefix(1500)))
        }
        updateTimer = Timer.scheduledTimer(withTimeInterval: 15, repeats: false) { [weak self] _ in self?.checkUpdates(manual: false) }
    }

    // ── Menu ─────────────────────────────────────────────────────
    func rebuildMenu() {
        item.button?.title = settings.enabled && watcher.isRunning ? "Ş" : "ş"
        item.menu = buildMenu()
    }

    func buildMenu() -> NSMenu {
        let m = NSMenu()
        func add(_ title: String, _ action: Selector?, on: Bool? = nil, key: String = "") {
            let i = NSMenuItem(title: title, action: action, keyEquivalent: key)
            i.target = self
            if let on = on { i.state = on ? .on : .off }
            m.addItem(i)
        }
        if !(watcher?.isRunning ?? false) { add("⚠ Ruxsat kerak: Maxsus imkoniyatlar (Accessibility)…", #selector(openPermission)); m.addItem(.separator()) }
        add("Konvertatsiya (Control+Shift+U)", #selector(toggle), on: settings.enabled)
        m.addItem(.separator())
        add("Qat’iy rejim (lug‘at tasdiqlagan so‘zlar)", #selector(setStrict), on: !settings.aggressive)
        add("Agressiv rejim (barcha so‘zlar)", #selector(setAggressive), on: settings.aggressive)
        add("Chat qisqartmalari", #selector(toggleSlang), on: settings.slang)
        add("Neyron model (lug‘atda yo‘q so‘zlar)", #selector(toggleNeural), on: settings.neural)
        m.addItem(.separator())
        add("Oq ro‘yxatni tahrirlash…", #selector(editWhitelist))
        add("Qora ro‘yxatni tahrirlash…", #selector(editBlacklist))
        if #available(macOS 13.0, *) { add("Kompyuter yoqilganda ishga tushirish", #selector(toggleLogin), on: SMAppService.mainApp.status == .enabled) }
        add("Avtomatik yangilash", #selector(toggleAutoUpdate), on: settings.autoUpdate)
        add(updateNote ?? "Yangilanishni tekshirish", #selector(checkNow))
        m.addItem(.separator())
        add("YangiAlifbo v\(Updater.current) — dvigatel \(engine?.version ?? "?")", nil)
        add("Chiqish", #selector(quit), key: "q")
        return m
    }

    @objc func toggle() { settings.enabled.toggle(); watcher.reset(); rebuildMenu() }
    @objc func setStrict() { settings.aggressive = false; rebuildMenu() }
    @objc func setAggressive() { settings.aggressive = true; rebuildMenu() }
    @objc func toggleSlang() { settings.slang.toggle(); rebuildMenu() }
    @objc func toggleNeural() { settings.neural.toggle(); rebuildMenu() }
    @objc func toggleAutoUpdate() { settings.autoUpdate.toggle(); rebuildMenu() }
    @objc func editWhitelist() { edit(engine.userFile(0)) }
    @objc func editBlacklist() { edit(engine.userFile(1)) }
    @objc func checkNow() { checkUpdates(manual: true) }
    @objc func quit() { NSApp.terminate(nil) }
    @objc func openPermission() { NSWorkspace.shared.open(URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility")!) }

    @objc func toggleLogin() {
        guard #available(macOS 13.0, *) else { return }
        do {
            if SMAppService.mainApp.status == .enabled { try SMAppService.mainApp.unregister() } else { try SMAppService.mainApp.register() }
        } catch { Log.write("login item: \(error)") }
        rebuildMenu()
    }

    private func edit(_ path: String) {
        if !FileManager.default.fileExists(atPath: path) { FileManager.default.createFile(atPath: path, contents: Data()) }
        NSWorkspace.shared.open(URL(fileURLWithPath: path))
    }

    private func askForPermission() {
        let a = NSAlert()
        a.messageText = "YangiAlifbo’ga ruxsat bering"
        a.informativeText = """
        So‘zlarni yangi alifboga o‘girish uchun YangiAlifbo yozayotganingizni ko‘rishi va so‘zni almashtirishi kerak. \
        macOS buning uchun “Maxsus imkoniyatlar” (Accessibility) ruxsatini so‘raydi. Matningiz hech qayerga yuborilmaydi.

        Tizim sozlamalari → Maxfiylik va xavfsizlik → Maxsus imkoniyatlar → YangiAlifbo’ni yoqing.

        To convert words, YangiAlifbo needs Accessibility permission (System Settings → Privacy & Security → Accessibility). Nothing you type leaves your Mac.
        """
        a.addButton(withTitle: "Sozlamalarni ochish / Open Settings")
        a.addButton(withTitle: "Keyinroq / Later")
        NSApp.activate(ignoringOtherApps: true)
        if a.runModal() == .alertFirstButtonReturn {
            _ = AXIsProcessTrustedWithOptions([kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary)
            openPermission()
        }
    }

    private func alert(_ title: String, _ text: String) {
        let a = NSAlert(); a.messageText = title; a.informativeText = text
        NSApp.activate(ignoringOtherApps: true); a.runModal()
    }

    // ── Updates: 15 s after start, then every 6 hours ─────────────
    func checkUpdates(manual: Bool) {
        updateTimer?.invalidate()
        updateTimer = Timer.scheduledTimer(withTimeInterval: 6 * 3600, repeats: false) { [weak self] _ in self?.checkUpdates(manual: false) }
        Task { @MainActor in
            do {
                guard let r = try await Updater.latest(), isNewer(r.tag, than: Updater.current) else {
                    if manual { alert("YangiAlifbo", "Siz eng so‘nggi versiyadasiz: v\(Updater.current).") }
                    return
                }
                if (settings.autoUpdate || manual) && Updater.canSelfUpdate {
                    updateNote = "\(r.tag) o‘rnatilmoqda…"; rebuildMenu()
                    try await Updater.install(r, settings: settings)
                } else {
                    updateNote = "⬆ \(r.tag) chiqdi — o‘rnatish uchun bosing"; rebuildMenu()
                }
            } catch {
                Log.write("update: \(error)")
                updateNote = nil; rebuildMenu()
                if manual { alert("Yangilab bo‘lmadi", error.localizedDescription) }
            }
        }
    }
}
