package uz.yangialifbo.keyboard

/**
 * Decides what to change in a text field after another keyboard (Samsung, Gboard…) typed into
 * it, for [YangiAccessibilityService]. The accessibility event says where text was added; if
 * that ended a word, the word is replaced by its conversion.
 *
 * The event is a snapshot and arrives late: the field may have grown since. So the word is found
 * in the snapshot, checked against the field's current text, and replaced there.
 */
object TextReplacer {
    /** Replace [start, end) of the current text with [replacement]; selection to [selStart], [selEnd]. */
    class Plan(val start: Int, val end: Int, val replacement: String, val selStart: Int, val selEnd: Int)

    /** More than this at once is a paste, not typing or autocorrect of one word. */
    private const val MAX_ADDED = 24

    /**
     * @param snapshot the field text when the event happened
     * @param from index of the first changed character, [added] how many characters were added there
     * @param current the field text now, with the selection [selStart]..[selEnd] (-1 if unknown)
     */
    fun plan(
        snapshot: String,
        from: Int,
        added: Int,
        current: String,
        selStart: Int,
        selEnd: Int,
        convert: (String) -> String?,
    ): Plan? {
        if (added !in 1..MAX_ADDED || from < 0) return null
        val d = from + added - 1 // the last added character must end a word
        if (d !in snapshot.indices || WordBuffer.isWordChar(snapshot[d], true)) return null

        var start = d
        while (start > 0 && WordBuffer.isWordChar(snapshot[start - 1], true)) start--
        while (start < d && snapshot[start] == '-') start++
        if (start >= d) return null

        val word = snapshot.substring(start, d)
        val converted = convert(word)?.takeIf { it.isNotEmpty() && it != word } ?: return null

        // The field must still hold this word and its delimiter in the same place.
        if (current.length <= d || !current.regionMatches(start, snapshot, start, d - start + 1)) return null

        val delta = converted.length - word.length
        fun move(pos: Int) = when {
            pos < 0 -> pos
            pos >= d -> pos + delta
            pos > start -> start + converted.length // inside the word
            else -> pos
        }
        return Plan(start, d, converted, move(selStart), move(selEnd))
    }
}
