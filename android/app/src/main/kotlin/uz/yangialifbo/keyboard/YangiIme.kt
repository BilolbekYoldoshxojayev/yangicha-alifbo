package uz.yangialifbo.keyboard

import android.inputmethodservice.InputMethodService
import android.os.Build
import android.os.SystemClock
import android.text.InputType
import android.view.KeyEvent
import android.view.View
import android.view.inputmethod.EditorInfo
import android.view.inputmethod.InputMethodManager
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat

/**
 * The keyboard service. Typing itself is [TypingController]; this class connects it to the
 * Android input method framework, the keyboard view and the settings. Password, e-mail and URL
 * fields, and non-text fields, are never converted.
 */
class YangiIme : InputMethodService(), KeyboardView.Listener {
    private lateinit var settings: Settings
    private var keyboard: KeyboardView? = null

    private val typing = TypingController(
        connection = { currentInputConnection },
        convert = { word ->
            val engine = EngineHolder.get()
            if (engine == null) {
                null
            } else {
                try {
                    engine.convert(word, settings.mode, settings.slang, settings.neural)
                } catch (t: Throwable) {
                    null
                }
            }
        },
        sendKey = { code -> sendDownUpKeyEvents(code) },
        sendChar = { ch -> currentInputConnection?.sendKeyEvent(KeyEvent(SystemClock.uptimeMillis(), ch.toString(), 0, 0)) },
    )

    /** The field accepts converted text (a plain text field, not a password, e-mail or URL). */
    private var textField = false

    override fun onCreate() {
        super.onCreate()
        settings = Settings(this)
        EngineHolder.start(settings)
    }

    override fun onCreateInputView(): View {
        val view = KeyboardView(this)
        view.listener = this
        ViewCompat.setOnApplyWindowInsetsListener(view) { v, insets ->
            val bars = insets.getInsets(WindowInsetsCompat.Type.systemBars() or WindowInsetsCompat.Type.displayCutout())
            v.setPadding(bars.left, 0, bars.right, bars.bottom)
            insets
        }
        keyboard = view
        return view
    }

    override fun onEvaluateFullscreenMode(): Boolean = false

    override fun onStartInputView(info: EditorInfo, restarting: Boolean) {
        super.onStartInputView(info, restarting)
        typing.reset()

        val type = info.inputType
        textField = FieldRules.isPlainText(type)
        typing.keyEventsOnly = type == InputType.TYPE_NULL
        typing.converting = textField && settings.convert

        EngineHolder.start(settings)
        EngineHolder.applyLists(settings)
        keyboard?.let {
            it.haptic = settings.haptic
            it.convertOn = settings.convert
            it.showGlobe = Build.VERSION.SDK_INT < 28 || shouldOfferSwitchingToNextInputMethod()
            it.reset(capsWanted(info))
        }
    }

    override fun onFinishInputView(finishingInput: Boolean) {
        typing.reset()
        super.onFinishInputView(finishingInput)
    }

    override fun onUpdateSelection(
        oldSelStart: Int, oldSelEnd: Int, newSelStart: Int, newSelEnd: Int,
        candidatesStart: Int, candidatesEnd: Int,
    ) {
        super.onUpdateSelection(oldSelStart, oldSelEnd, newSelStart, newSelEnd, candidatesStart, candidatesEnd)
        // Updates arrive late, so one that disagrees with the composing region is not proof the
        // cursor moved; it only makes the next edit check the text in the field first.
        if (candidatesStart == -1 || newSelStart != candidatesEnd || newSelEnd != candidatesEnd) {
            typing.selectionLooksMoved()
        }
    }

    // ── Keys ───────────────────────────────────────────────────────────

    override fun onText(text: String) {
        for (ch in text) typing.type(ch)
        if (typing.word.isEmpty()) updateShift()
    }

    override fun onDelete() {
        typing.delete()
        if (typing.word.isEmpty()) updateShift()
    }

    override fun onEnter() {
        val ic = currentInputConnection ?: return
        typing.flush()
        val info = currentInputEditorInfo
        val action = info.imeOptions and EditorInfo.IME_MASK_ACTION
        when {
            info.actionLabel != null && info.actionId != 0 -> ic.performEditorAction(info.actionId)
            info.imeOptions and EditorInfo.IME_FLAG_NO_ENTER_ACTION != 0 -> sendDownUpKeyEvents(KeyEvent.KEYCODE_ENTER)
            action == EditorInfo.IME_ACTION_NONE || action == EditorInfo.IME_ACTION_UNSPECIFIED ->
                sendDownUpKeyEvents(KeyEvent.KEYCODE_ENTER)
            else -> ic.performEditorAction(action)
        }
        updateShift()
    }

    override fun onGlobe() {
        typing.finishAsTyped()
        if (Build.VERSION.SDK_INT >= 28) {
            switchToNextInputMethod(false)
        } else {
            (getSystemService(INPUT_METHOD_SERVICE) as InputMethodManager).showInputMethodPicker()
        }
    }

    override fun onToggleConvert() {
        typing.finishAsTyped()
        settings.convert = !settings.convert
        typing.converting = textField && settings.convert
        keyboard?.convertOn = settings.convert
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private fun capsWanted(info: EditorInfo): Boolean =
        currentInputConnection?.getCursorCapsMode(info.inputType)?.let { it != 0 } ?: false

    private fun updateShift() {
        val info = currentInputEditorInfo ?: return
        keyboard?.setAutoShift(capsWanted(info))
    }
}
