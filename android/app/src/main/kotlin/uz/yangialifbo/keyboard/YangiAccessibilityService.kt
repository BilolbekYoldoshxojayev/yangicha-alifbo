package uz.yangialifbo.keyboard

import android.accessibilityservice.AccessibilityService
import android.os.Bundle
import android.text.SpannableStringBuilder
import android.view.accessibility.AccessibilityEvent
import android.view.accessibility.AccessibilityNodeInfo

/**
 * Converts words typed with any keyboard (Samsung, Gboard…). When text is added to a field and
 * it ends a word, the word is replaced by its conversion in the new alphabet. Only plain text
 * fields are touched: never passwords, e-mail or URL fields. Nothing is stored or sent anywhere.
 */
class YangiAccessibilityService : AccessibilityService() {
    private lateinit var settings: Settings

    override fun onServiceConnected() {
        super.onServiceConnected()
        settings = Settings(this)
        EngineHolder.start(settings)
    }

    override fun onAccessibilityEvent(event: AccessibilityEvent) {
        if (event.eventType != AccessibilityEvent.TYPE_VIEW_TEXT_CHANGED || event.isPassword) return
        if (!settings.convert) return
        val engine = EngineHolder.get() ?: return
        val snapshot = event.text.firstOrNull()?.toString() ?: return

        val node = event.source ?: return
        if (node.isPassword || !node.isEditable || node.isShowingHintText) return
        if (node.inputType != 0 && !FieldRules.isPlainText(node.inputType)) return

        // The event is old by now: read the field again.
        if (!node.refresh()) return
        val current = node.text ?: return
        val plan = TextReplacer.plan(
            snapshot, event.fromIndex, event.addedCount,
            current.toString(), node.textSelectionStart, node.textSelectionEnd,
        ) { word ->
            try {
                engine.convert(word, settings.mode, settings.slang, settings.neural)
            } catch (t: Throwable) {
                null
            }
        } ?: return

        val text = SpannableStringBuilder(current).replace(plan.start, plan.end, plan.replacement)
        val args = Bundle()
        args.putCharSequence(AccessibilityNodeInfo.ACTION_ARGUMENT_SET_TEXT_CHARSEQUENCE, text)
        if (!node.performAction(AccessibilityNodeInfo.ACTION_SET_TEXT, args)) return
        if (plan.selStart >= 0 && plan.selEnd >= 0) {
            val sel = Bundle()
            sel.putInt(AccessibilityNodeInfo.ACTION_ARGUMENT_SELECTION_START_INT, plan.selStart)
            sel.putInt(AccessibilityNodeInfo.ACTION_ARGUMENT_SELECTION_END_INT, plan.selEnd)
            node.performAction(AccessibilityNodeInfo.ACTION_SET_SELECTION, sel)
        }
    }

    override fun onInterrupt() = Unit
}
