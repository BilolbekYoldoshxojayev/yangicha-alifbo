// YangiAlifbo for macOS. `--selftest` checks the installed app without needing permissions.

import AppKit
import YangiAlifboKit

func selfTest() -> Int32 {
    var failures: Int32 = 0
    func check(_ name: String, _ test: () throws -> String?) {
        do {
            if let problem = try test() { print("FAIL  \(name): \(problem)"); failures += 1 } else { print("ok    \(name)") }
        } catch { print("FAIL  \(name): \(error)"); failures += 1 }
    }
    var engine: Engine?
    check("engine loads") { engine = try Engine(); print("      engine v\(engine!.version), app v\(Updater.current)"); return nil }
    check("converts like Windows") {
        guard let e = engine else { return "no engine" }
        let cases: [(String, String?)] = [("shahar", "şahar"), ("o'zbekiston", "özbekiston"), ("5-chi", "5-çi"), ("fresh", nil), ("Choyxona", "Çoyxona")]
        for (w, want) in cases where e.convert(w, aggressive: false, slang: true, neural: true) != want { return "\(w) → \(String(describing: e.convert(w, aggressive: false, slang: true, neural: true)))" }
        return nil
    }
    check("word buffer plans the retype") {
        var b = WordBuffer()
        var last = KeyAction.pass
        for c in "shahar " { last = b.key(.character(c), shortcut: false) { engine?.convert($0, aggressive: false, slang: true, neural: true) } }
        return last == .retype(count: 6, text: "şahar") ? nil : "\(last)"
    }
    check("user lists located") { engine.map { $0.userFile(0).hasSuffix("user_whitelist.txt") ? nil : $0.userFile(0) } ?? "no engine" }
    check("event tap without permission does not crash") {
        guard let e = engine else { return "no engine" }
        let started = KeyWatcher(engine: e, settings: Settings()).start()
        print("      tap \(started ? "running" : "not allowed (no Accessibility permission)")")
        return nil
    }
    check("menu builds") {
        let d = AppDelegate(); d.engine = engine
        return d.buildMenu().items.count > 8 ? nil : "menu too short"
    }
    check("update check reads the latest release") {
        let sem = DispatchSemaphore(value: 0)
        var tag: String?
        var failure: Error?
        Task { do { tag = try await Updater.latest()?.tag } catch { failure = error }; sem.signal() }
        sem.wait()
        if let f = failure { return "\(f)" }
        print("      latest with a Mac build: \(tag ?? "none yet")")
        return nil
    }
    print(failures == 0 ? "PASS" : "FAILED: \(failures)")
    return failures
}

if CommandLine.arguments.contains("--selftest") {
    _ = NSApplication.shared
    exit(selfTest())
}

let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
app.setActivationPolicy(.accessory)
app.run()
