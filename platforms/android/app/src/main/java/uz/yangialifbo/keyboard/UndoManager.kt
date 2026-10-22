package uz.yangialifbo.keyboard

/**
 * Manages single-tap backspace undo for transliterated words.
 * Requirement: "one backspace right after a conversion undoes it".
 */
class UndoManager {

    data class ConversionRecord(
        val originalWord: String,
        val convertedWord: String,
        val delimiter: String
    )

    private var lastRecord: ConversionRecord? = null

    /**
     * Records a successful transliteration.
     */
    fun record(original: String, converted: String, delimiter: String) {
        lastRecord = ConversionRecord(original, converted, delimiter)
    }

    /**
     * Clears the undo state (e.g. after any subsequent non-backspace keypress or cursor move).
     */
    fun clear() {
        lastRecord = null
    }

    /**
     * Checks if the backspace press can revert the last conversion.
     * @param textBeforeCursor The text currently preceding the cursor in the InputConnection.
     * @return The original word to restore, or null if undo is not applicable.
     */
    fun checkUndo(textBeforeCursor: CharSequence?): ConversionRecord? {
        val record = lastRecord ?: return null
        if (textBeforeCursor == null) return null

        val expectedSuffix = record.convertedWord + record.delimiter
        if (textBeforeCursor.endsWith(expectedSuffix)) {
            val result = record
            lastRecord = null // Consumed
            return result
        }
        return null
    }
}
