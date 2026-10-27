// Self-update from GitHub Releases, as on Windows: download YangiAlifbo-mac.zip, check
// the SHA-256 GitHub publishes, swap the app bundle, relaunch, then say what is new.

import AppKit
import CryptoKit
import YangiAlifboKit

struct Release {
    let tag: String, notes: String, url: URL, size: Int, sha256: String
}

final class Updater {
    static let api = URL(string: "https://api.github.com/repos/BilolbekYoldoshxojayev/yangicha-alifbo/releases/latest")!
    static let asset = "YangiAlifbo-mac.zip"
    static var current: String { Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "0.0.0" }

    /// Only an installed bundle we can write replaces itself (not a translocated download).
    static var canSelfUpdate: Bool {
        let path = Bundle.main.bundlePath
        return path.hasSuffix(".app") && !path.contains("/AppTranslocation/")
            && FileManager.default.isWritableFile(atPath: (path as NSString).deletingLastPathComponent)
    }

    static func latest() async throws -> Release? {
        var req = URLRequest(url: api)
        req.setValue("YangiAlifbo-mac/\(current)", forHTTPHeaderField: "User-Agent")
        let (data, _) = try await URLSession.shared.data(for: req)
        guard let o = try JSONSerialization.jsonObject(with: data) as? [String: Any],
              let tag = o["tag_name"] as? String,
              let assets = o["assets"] as? [[String: Any]],
              let a = assets.first(where: { $0["name"] as? String == asset }),
              let link = a["browser_download_url"] as? String, let url = URL(string: link),
              let digest = a["digest"] as? String, digest.hasPrefix("sha256:") else { return nil }
        return Release(tag: tag, notes: o["body"] as? String ?? "", url: url, size: a["size"] as? Int ?? 0,
                       sha256: String(digest.dropFirst(7)).lowercased())
    }

    /// Downloads, verifies and installs `r`, then relaunches. Throws on any mismatch.
    static func install(_ r: Release, settings: Settings) async throws {
        let (file, _) = try await URLSession.shared.download(from: r.url)
        let data = try Data(contentsOf: file)
        let hash = SHA256.hash(data: data).map { String(format: "%02x", $0) }.joined()
        guard data.count == r.size, hash == r.sha256 else { throw NSError(domain: "YangiAlifbo", code: 10, userInfo: [NSLocalizedDescriptionKey: "SHA-256 mos kelmadi"]) }

        let dir = FileManager.default.temporaryDirectory.appendingPathComponent("yangialifbo-update-\(UUID().uuidString)")
        try FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        let zip = dir.appendingPathComponent(asset)
        try data.write(to: zip)
        let unzip = Process()
        unzip.executableURL = URL(fileURLWithPath: "/usr/bin/ditto")
        unzip.arguments = ["-x", "-k", zip.path, dir.path]
        try unzip.run(); unzip.waitUntilExit()
        let newApp = dir.appendingPathComponent("YangiAlifbo.app")
        guard unzip.terminationStatus == 0, FileManager.default.fileExists(atPath: newApp.path) else {
            throw NSError(domain: "YangiAlifbo", code: 11, userInfo: [NSLocalizedDescriptionKey: "arxivni ochib bo‘lmadi"])
        }
        _ = try FileManager.default.replaceItemAt(Bundle.main.bundleURL, withItemAt: newApp)
        settings.whatsNew = "\(r.tag)\n\n\(r.notes)"
        Log.write("updated to \(r.tag), relaunching")

        let relaunch = Process()
        relaunch.executableURL = URL(fileURLWithPath: "/bin/sh")
        relaunch.arguments = ["-c", "sleep 1; /usr/bin/open \"$0\"", Bundle.main.bundlePath]
        try relaunch.run()
        await MainActor.run { NSApp.terminate(nil) }
    }
}
