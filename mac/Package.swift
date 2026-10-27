// swift-tools-version:5.7
// YangiAlifbo for macOS: menu-bar app (Sources/YangiAlifbo) over the shared engine
// (libyangialifbo.dylib, src/YangiAlifbo.Native). Key logic is in YangiAlifboKit for tests.
import PackageDescription

let package = Package(
    name: "YangiAlifbo",
    platforms: [.macOS(.v12)],
    targets: [
        .target(name: "YangiAlifboKit"),
        .executableTarget(name: "YangiAlifbo", dependencies: ["YangiAlifboKit"]),
        .testTarget(name: "YangiAlifboKitTests", dependencies: ["YangiAlifboKit"]),
    ]
)
