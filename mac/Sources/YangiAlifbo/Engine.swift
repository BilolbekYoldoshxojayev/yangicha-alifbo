// libyangialifbo.dylib (src/YangiAlifbo.Native): the Windows engine, loaded at runtime.

import Foundation

enum Log {
    static let url = FileManager.default.homeDirectoryForCurrentUser.appendingPathComponent("Library/Logs/YangiAlifbo.log")
    static func write(_ s: String) {
        let line = "[\(Date())] \(s)\n"
        if let h = try? FileHandle(forWritingTo: url) { h.seekToEndOfFile(); h.write(Data(line.utf8)); try? h.close() }
        else { try? Data(line.utf8).write(to: url) }
    }
}

final class Engine {
    private typealias InitFn = @convention(c) () -> Int32
    private typealias ConvertFn = @convention(c) (UnsafePointer<CChar>?, Int32, Int32, Int32) -> UnsafeMutablePointer<CChar>?
    private typealias StrFn = @convention(c) (Int32) -> UnsafeMutablePointer<CChar>?
    private typealias FreeFn = @convention(c) (UnsafeMutableRawPointer?) -> Void
    private typealias VersionFn = @convention(c) () -> UnsafePointer<CChar>?

    private let convertFn: ConvertFn, userFileFn: StrFn, freeFn: FreeFn
    let version: String

    /// The dylib inside the app bundle (Contents/Frameworks), or next to the executable.
    static var defaultPath: String {
        let inBundle = Bundle.main.bundleURL.appendingPathComponent("Contents/Frameworks/libyangialifbo.dylib").path
        if FileManager.default.fileExists(atPath: inBundle) { return inBundle }
        return URL(fileURLWithPath: CommandLine.arguments[0]).deletingLastPathComponent().appendingPathComponent("libyangialifbo.dylib").path
    }

    init(path: String = Engine.defaultPath) throws {
        guard let h = dlopen(path, RTLD_NOW) else { throw NSError(domain: "YangiAlifbo", code: 1, userInfo: [NSLocalizedDescriptionKey: String(cString: dlerror())]) }
        func sym<T>(_ name: String, _: T.Type) throws -> T {
            guard let p = dlsym(h, name) else { throw NSError(domain: "YangiAlifbo", code: 2, userInfo: [NSLocalizedDescriptionKey: "missing \(name)"]) }
            return unsafeBitCast(p, to: T.self)
        }
        guard try sym("ya_init", InitFn.self)() == 1 else { throw NSError(domain: "YangiAlifbo", code: 3, userInfo: [NSLocalizedDescriptionKey: "dictionaries failed to load"]) }
        convertFn = try sym("ya_convert_word", ConvertFn.self)
        userFileFn = try sym("ya_user_file", StrFn.self)
        freeFn = try sym("ya_free", FreeFn.self)
        version = String(cString: try sym("ya_version", VersionFn.self)()!)
    }

    private func take(_ p: UnsafeMutablePointer<CChar>?) -> String? {
        guard let p = p else { return nil }
        defer { freeFn(UnsafeMutableRawPointer(p)) }
        return String(cString: p)
    }

    /// The word in the new alphabet, or nil when it stays as typed. mode: 0 strict, 1 aggressive.
    func convert(_ word: String, aggressive: Bool, slang: Bool, neural: Bool) -> String? {
        word.withCString { take(convertFn($0, aggressive ? 1 : 0, slang ? 1 : 0, neural ? 1 : 0)) }
    }

    /// Path of the user whitelist (0) or blacklist (1).
    func userFile(_ kind: Int32) -> String { take(userFileFn(kind)) ?? "" }
}

/// Preferences, kept in the app's user defaults.
final class Settings {
    private let d = UserDefaults.standard
    init() { d.register(defaults: ["enabled": true, "aggressive": false, "slang": true, "neural": true, "autoUpdate": true]) }
    var enabled: Bool { get { d.bool(forKey: "enabled") } set { d.set(newValue, forKey: "enabled") } }
    var aggressive: Bool { get { d.bool(forKey: "aggressive") } set { d.set(newValue, forKey: "aggressive") } }
    var slang: Bool { get { d.bool(forKey: "slang") } set { d.set(newValue, forKey: "slang") } }
    var neural: Bool { get { d.bool(forKey: "neural") } set { d.set(newValue, forKey: "neural") } }
    var autoUpdate: Bool { get { d.bool(forKey: "autoUpdate") } set { d.set(newValue, forKey: "autoUpdate") } }
    var whatsNew: String? { get { d.string(forKey: "whatsNew") } set { d.set(newValue, forKey: "whatsNew") } }
}
