import XCTest
@testable import YangiAlifboKit

final class WordBufferTests: XCTestCase {
    let table = ["shahar": "şahar", "o'rtoq": "örtoq", "5-chi": "5-çi"]

    /// Types `text` and returns the actions taken at word ends.
    func run(_ text: String, extra: [KeyKind] = []) -> [KeyAction] {
        var b = WordBuffer()
        var out: [KeyAction] = []
        for c in text { let a = b.key(.character(c), shortcut: false) { self.table[$0] }; if a != .pass { out.append(a) } }
        for k in extra { let a = b.key(k, shortcut: false) { self.table[$0] }; if a != .pass { out.append(a) } }
        return out
    }

    func testConvertsOnSpace() { XCTAssertEqual(run("shahar "), [.retype(count: 6, text: "şahar")]) }
    func testPunctuationEndsWord() { XCTAssertEqual(run("o'rtoq,"), [.retype(count: 6, text: "örtoq")]) }
    func testUnknownWordUntouched() { XCTAssertEqual(run("fresh "), []) }
    func testOrdinal() { XCTAssertEqual(run("5-chi "), [.retype(count: 5, text: "5-çi")]) }
    func testEnterEndsWord() { XCTAssertEqual(run("shahar", extra: [.end]), [.retype(count: 6, text: "şahar")]) }
    func testBackspaceEdits() { XCTAssertEqual(run("shaharr", extra: [.backspace, .character(" ")]), [.retype(count: 6, text: "şahar")]) }
    func testArrowForgetsWord() { XCTAssertEqual(run("shahar", extra: [.other, .character(" ")]), []) }

    func testShortcutForgetsWord() {
        var b = WordBuffer()
        for c in "shahar" { _ = b.key(.character(c), shortcut: false) { _ in nil } }
        XCTAssertEqual(b.key(.character("s"), shortcut: true) { _ in nil }, .pass)
        XCTAssertEqual(b.word, "")
    }

    func testVersions() {
        XCTAssertTrue(isNewer("v2.1.0", than: "2.0.0"))
        XCTAssertFalse(isNewer("v2.1.0", than: "2.1.0"))
        XCTAssertTrue(isNewer("v10.0.0", than: "9.9.9"))
    }
}
