package uz.yangialifbo.keyboard

/**
 * The word being typed. Same rules as the Linux input method (linux/engine/wordbuffer.py):
 * letters, digits, apostrophes and (inside a word) a hyphen belong to the word; any other
 * character ends it, and the word is then committed converted to the new alphabet.
 * Only a finished word is converted, never a half-typed one.
 */
class WordBuffer(private val convert: (String) -> String?) {
    var word: String = ""
        private set

    val isEmpty: Boolean get() = word.isEmpty()

    fun clear() {
        word = ""
    }

    fun accepts(ch: Char): Boolean = isWordChar(ch, word.isNotEmpty())

    fun add(ch: Char) {
        word += ch
    }

    /** Removes the last character; false when the buffer was empty. */
    fun backspace(): Boolean {
        if (word.isEmpty()) return false
        word = word.dropLast(1)
        return true
    }

    /** What the word becomes (the word itself when the engine keeps it). */
    fun converted(): String = if (word.isEmpty()) "" else convert(word)?.takeIf { it.isNotEmpty() } ?: word

    /** Empties the buffer and returns the word as typed and in the new alphabet. */
    fun take(): Taken {
        val original = word
        val result = converted()
        word = ""
        return Taken(original, result)
    }

    data class Taken(val original: String, val converted: String)

    companion object {
        const val APOSTROPHES = "'‘’ʻʼ`´‛′"

        /** [inWord]: a word has already started (a hyphen only continues a word). */
        fun isWordChar(ch: Char, inWord: Boolean): Boolean =
            ch.isLetter() || ch.isDigit() || ch in APOSTROPHES || (ch == '-' && inWord)
    }
}
