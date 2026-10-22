package uz.yangialifbo.keyboard

import android.content.SharedPreferences
import android.inputmethodservice.InputMethodService
import android.text.InputType
import android.view.View
import android.view.inputmethod.EditorInfo
import androidx.preference.PreferenceManager

/**
 * YangiAlifbo Android InputMethodService.
 * Real-time word boundary transliteration to 2024 Reformed Latin.
 * Strictly enforces zero-network privacy and secure field bypass.
 */
class YangiKeyboardService : InputMethodService(), LatinKeyboardView.OnKeyboardActionListener {

    private lateinit var prefs: SharedPreferences
    private lateinit var undoManager: UndoManager
    private var isSecureField: Boolean = false

    private var autoConvertEnabled: Boolean = true
    private var conversionMode: String = "strict"
    private var slangEnabled: Boolean = false
    private var mlEnabled: Boolean = false

    private val delimiterRegex = Regex("[\\s.,!?;:()\\[\\]{}\"/\\\\<>«»]")

    override fun onCreate() {
        super.onCreate()
        prefs = PreferenceManager.getDefaultSharedPreferences(this)
        undoManager = UndoManager()
    }

    override fun onCreateInputView(): View {
        val keyboardView = LatinKeyboardView(this)
        keyboardView.setOnKeyboardActionListener(this)
        return keyboardView
    }

    override fun onStartInputView(info: EditorInfo?, restarting: Boolean) {
        super.onStartInputView(info, restarting)
        undoManager.clear()
        loadPreferences()
        isSecureField = checkIsSecureField(info)
    }

    private fun loadPreferences() {
        autoConvertEnabled = prefs.getBoolean("pref_enabled", true)
        conversionMode = prefs.getString("pref_mode", "strict") ?: "strict"
        slangEnabled = prefs.getBoolean("pref_slang", false)
        mlEnabled = prefs.getBoolean("pref_ml", false)
    }

    /**
     * Hard Rule 7 & Security Invariant: Check if field is secure or password.
     */
    fun checkIsSecureField(info: EditorInfo?): Boolean {
        if (info == null) return false
        val inputType = info.inputType
        val textClass = inputType and InputType.TYPE_MASK_CLASS

        // Non-text inputs (numbers, phone, date, time) must not be transliterated
        if (textClass != InputType.TYPE_CLASS_TEXT) {
            return true
        }

        val variation = inputType and InputType.TYPE_MASK_VARIATION
        return when (variation) {
            InputType.TYPE_TEXT_VARIATION_PASSWORD,
            InputType.TYPE_TEXT_VARIATION_VISIBLE_PASSWORD,
            InputType.TYPE_TEXT_VARIATION_WEB_PASSWORD -> true
            InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS,
            InputType.TYPE_TEXT_VARIATION_URI -> true
            else -> false
        }
    }

    // Key event handling from LatinKeyboardView
    override fun onKey(primaryCode: Int) {
        val ic = currentInputConnection ?: return

        when (primaryCode) {
            LatinKeyboardView.KEYCODE_DELETE -> handleBackspace()
            LatinKeyboardView.KEYCODE_ENTER -> handleDelimiter('\n')
            LatinKeyboardView.KEYCODE_SPACE -> handleDelimiter(' ')
            else -> {
                val char = primaryCode.toChar()
                if (isDelimiter(char)) {
                    handleDelimiter(char)
                } else {
                    ic.commitText(char.toString(), 1)
                    undoManager.clear()
                }
            }
        }
    }

    override fun onText(text: CharSequence) {
        currentInputConnection?.commitText(text, 1)
        undoManager.clear()
    }

    private fun isDelimiter(c: Char): Boolean {
        return delimiterRegex.matches(c.toString())
    }

    private fun handleDelimiter(delimiter: Char) {
        val ic = currentInputConnection ?: return

        if (!isSecureField && autoConvertEnabled) {
            val textBefore = ic.getTextBeforeCursor(64, 0)
            if (!textBefore.isNullOrEmpty()) {
                val lastWord = extractLastWord(textBefore)
                if (lastWord.length >= 2 && YangiNativeBridge.canConvert(lastWord, conversionMode, mlEnabled)) {
                    val converted = YangiNativeBridge.transliterateWord(lastWord, conversionMode, slangEnabled, mlEnabled)
                    if (converted != lastWord) {
                        // Replace word and append delimiter
                        ic.deleteSurroundingText(lastWord.length, 0)
                        ic.commitText(converted + delimiter, 1)
                        undoManager.record(lastWord, converted, delimiter.toString())
                        return
                    }
                }
            }
        }

        ic.commitText(delimiter.toString(), 1)
        undoManager.clear()
    }

    private fun handleBackspace() {
        val ic = currentInputConnection ?: return

        val textBefore = ic.getTextBeforeCursor(64, 0)
        val undoRecord = undoManager.checkUndo(textBefore)

        if (undoRecord != null) {
            // Revert transliterated word back to original
            val totalLen = undoRecord.convertedWord.length + undoRecord.delimiter.length
            ic.deleteSurroundingText(totalLen, 0)
            ic.commitText(undoRecord.originalWord + undoRecord.delimiter, 1)
        } else {
            // Normal backspace: delete 1 character
            ic.deleteSurroundingText(1, 0)
        }
    }

    internal fun extractLastWord(text: CharSequence): String {
        var end = text.length
        while (end > 0 && isDelimiter(text[end - 1])) {
            end--
        }
        var start = end
        while (start > 0 && !isDelimiter(text[start - 1])) {
            start--
        }
        return text.subSequence(start, end).toString()
    }
}
