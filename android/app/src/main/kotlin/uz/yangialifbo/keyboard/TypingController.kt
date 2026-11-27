package uz.yangialifbo.keyboard

import android.view.inputmethod.InputConnection

/**
 * What typing does to the text field, independent of the Android service so it can be tested
 * against a fake [InputConnection].
 *
 * The word being typed is the composing text (underlined). A character that is not part of a
 * word finishes it: the word is committed converted to the new alphabet, together with that
 * character. Backspace right after such a conversion brings the word back as typed.
 */
class TypingController(
    private val connection: () -> InputConnection?,
    convert: (String) -> String?,
    /** Sends a key press (Backspace) to the editor. */
    private val sendKey: (keyCode: Int) -> Unit,
    /** Sends a character as a key event, for fields without an editor. */
    private val sendChar: (Char) -> Unit,
) {
    /** The field accepts converted text and the user has conversion on. */
    var converting = false

    /** The field has no editor (terminal-like): characters go in as key events. */
    var keyEventsOnly = false

    private val buffer = WordBuffer(convert)

    /** An unexpected selection change was seen: check the composing text before the next edit. */
    private var verify = false

    /** The last automatic conversion: [typed] is what the user typed, [committed] what replaced it. */
    private class AutoReplace(val typed: String, val committed: String)

    private var lastAuto: AutoReplace? = null

    val word: String get() = buffer.word

    fun reset() {
        buffer.clear()
        lastAuto = null
        verify = false
    }

    /** The text field reported a selection that does not match the composing region. */
    fun selectionLooksMoved() {
        if (!buffer.isEmpty) verify = true
    }

    fun type(ch: Char) {
        val ic = connection() ?: return
        if (keyEventsOnly) {
            sendChar(ch)
            return
        }
        if (!converting) {
            ic.commitText(ch.toString(), 1)
            return
        }
        checkComposing(ic)
        if (buffer.accepts(ch)) {
            lastAuto = null
            buffer.add(ch)
            ic.setComposingText(buffer.word, 1)
            return
        }
        val taken = buffer.take()
        val committed = taken.converted + ch
        ic.commitText(committed, 1)
        lastAuto = if (taken.converted != taken.original) AutoReplace(taken.original + ch, committed) else null
    }

    /** Backspace. */
    fun delete() {
        val ic = connection() ?: return
        if (keyEventsOnly) {
            sendKey(KEYCODE_DEL)
            return
        }
        if (converting) {
            checkComposing(ic)
            if (buffer.backspace()) {
                if (buffer.isEmpty) ic.commitText("", 1) else ic.setComposingText(buffer.word, 1)
                return
            }
            if (restoreTypedWord(ic)) return
        }
        lastAuto = null
        if (!ic.getSelectedText(0).isNullOrEmpty()) ic.commitText("", 1) else sendKey(KEYCODE_DEL)
    }

    /** Finishes the word (converted) before Enter acts. */
    fun flush() {
        val ic = connection() ?: return
        if (converting) {
            checkComposing(ic)
            val taken = buffer.take()
            if (taken.original.isNotEmpty()) ic.commitText(taken.converted, 1)
        }
        lastAuto = null
    }

    /** Leaves the word in the field as typed. */
    fun finishAsTyped() {
        if (!buffer.isEmpty) {
            buffer.clear()
            connection()?.finishComposingText()
        }
        lastAuto = null
    }

    /** Backspace right after an automatic conversion brings the word back as typed. */
    private fun restoreTypedWord(ic: InputConnection): Boolean {
        val auto = lastAuto ?: return false
        lastAuto = null
        val before = ic.getTextBeforeCursor(auto.committed.length, 0)?.toString()
        if (before != auto.committed) return false
        ic.beginBatchEdit()
        ic.deleteSurroundingText(auto.committed.length, 0)
        ic.commitText(auto.typed, 1)
        ic.endBatchEdit()
        return true
    }

    /** After a suspicious selection update: drop the word if the cursor is no longer behind it. */
    private fun checkComposing(ic: InputConnection) {
        if (!verify) return
        verify = false
        if (buffer.isEmpty) return
        val before = ic.getTextBeforeCursor(buffer.word.length, 0)?.toString()
        if (before != buffer.word) {
            buffer.clear()
            ic.finishComposingText()
        }
    }

    private companion object {
        const val KEYCODE_DEL = 67 // KeyEvent.KEYCODE_DEL, a compile-time constant that tests need not load
    }
}
