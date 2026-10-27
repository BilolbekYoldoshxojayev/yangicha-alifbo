// Tracks the word being typed, like the Windows keyboard hook. Keys always reach the
// app; when a word ends and converts differently, the caller deletes it and retypes it.
// Same word/delimiter rules as linux/engine/wordbuffer.py.

import Foundation

public let apostrophes: Set<Character> = ["'", "\u{2018}", "\u{2019}", "\u{02BB}", "\u{02BC}", "`", "\u{00B4}", "\u{201B}", "\u{2032}"]

public enum KeyKind: Equatable {
    case character(Character)
    case backspace
    case end        // Return, Enter, Tab
    case other      // arrows, Escape, Home, F-keys …
}

public enum KeyAction: Equatable {
    case pass
    /// Before the key reaches the app: delete `count` characters, type `text`.
    case retype(count: Int, text: String)
}

public struct WordBuffer {
    public private(set) var word = ""
    public init() {}

    public mutating func reset() { word = "" }

    static func isWordChar(_ c: Character, after word: String) -> Bool {
        c.isLetter || c.isNumber || apostrophes.contains(c) || (c == "-" && !word.isEmpty)
    }

    /// convert returns the word in the new alphabet, or nil to keep it.
    public mutating func key(_ kind: KeyKind, shortcut: Bool, convert: (String) -> String?) -> KeyAction {
        if shortcut { reset(); return .pass }
        switch kind {
        case .backspace:
            if !word.isEmpty { word.removeLast() }
            return .pass
        case .character(let c) where WordBuffer.isWordChar(c, after: word):
            word.append(c)
            return .pass
        case .character, .end:
            defer { reset() }
            guard !word.isEmpty, let converted = convert(word), converted != word else { return .pass }
            return .retype(count: word.count, text: converted)
        case .other:
            reset()
            return .pass
        }
    }
}

/// Version strings like "v2.1.0" compared numerically.
public func isNewer(_ latest: String, than current: String) -> Bool {
    func parts(_ v: String) -> [Int] {
        v.trimmingCharacters(in: CharacterSet(charactersIn: "vV")).split(separator: "-")[0].split(separator: ".").map { Int($0) ?? 0 }
    }
    let a = parts(latest), b = parts(current)
    for i in 0..<max(a.count, b.count) {
        let x = i < a.count ? a[i] : 0, y = i < b.count ? b[i] : 0
        if x != y { return x > y }
    }
    return false
}

