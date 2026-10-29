package uz.yangialifbo.keyboard

import android.content.Context
import android.graphics.Canvas
import android.graphics.Paint
import android.graphics.RectF
import android.graphics.Typeface
import android.os.Handler
import android.os.Looper
import android.util.SparseArray
import android.view.HapticFeedbackConstants
import android.view.MotionEvent
import android.view.View
import androidx.core.content.ContextCompat
import java.util.Locale

/**
 * The keyboard: a QWERTY layout drawn on one canvas. Letters fire when the finger lifts
 * (so a long press can type the alternate letter instead); Backspace fires on touch and repeats.
 * Shift and the symbols page are handled here; everything else goes to [Listener].
 */
class KeyboardView(context: Context) : View(context) {
    interface Listener {
        fun onText(text: String)
        fun onDelete()
        fun onEnter()
        fun onGlobe()
        fun onToggleConvert()
    }

    var listener: Listener? = null
    var haptic = true

    var showGlobe = false
        set(v) {
            if (field != v) {
                field = v
                rebuild()
            }
        }

    var convertOn = true
        set(v) {
            field = v
            invalidate()
        }

    private enum class Shift { OFF, ONCE, CAPS }

    private var shift = Shift.OFF
    private var symbols = false
    private var lastShiftTap = 0L

    private var rows: List<Row> = Layouts.letters(false)
    private val rects = ArrayList<Pair<Key, RectF>>()

    private val density = resources.displayMetrics.density
    private val scaledDensity = density * resources.configuration.fontScale
    private fun dp(v: Float) = v * density

    private val keyHeight = dp(50f)
    private val gap = dp(3f)
    private val radius = dp(7f)
    private val verticalPad = dp(5f)

    private val cBackground = ContextCompat.getColor(context, R.color.kb_background)
    private val cKey = ContextCompat.getColor(context, R.color.kb_key)
    private val cSpecial = ContextCompat.getColor(context, R.color.kb_key_special)
    private val cPressed = ContextCompat.getColor(context, R.color.kb_key_pressed)
    private val cText = ContextCompat.getColor(context, R.color.kb_text)
    private val cHint = ContextCompat.getColor(context, R.color.kb_hint)
    private val cAccent = ContextCompat.getColor(context, R.color.kb_accent)
    private val cAccentText = ContextCompat.getColor(context, R.color.kb_accent_text)

    private val fill = Paint(Paint.ANTI_ALIAS_FLAG)
    private val text = Paint(Paint.ANTI_ALIAS_FLAG).apply { textAlign = Paint.Align.CENTER }
    private val hint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        textAlign = Paint.Align.RIGHT
        textSize = 11f * scaledDensity
    }

    private class Touch(val key: Key, val rect: RectF) {
        var longFired = false
        var repeating = false
    }

    private val touches = SparseArray<Touch>()
    private val handler = Handler(Looper.getMainLooper())

    private fun rebuild() {
        rows = if (symbols) Layouts.symbols(showGlobe) else Layouts.letters(showGlobe)
        layoutKeys()
        invalidate()
    }

    /** Back to the letters page with Shift off (new input field). */
    fun reset(autoShift: Boolean) {
        symbols = false
        shift = if (autoShift) Shift.ONCE else Shift.OFF
        touches.clear()
        handler.removeCallbacksAndMessages(null)
        rebuild()
    }

    /** Sentence start: Shift for one letter, unless Caps Lock is on. */
    fun setAutoShift(on: Boolean) {
        if (shift == Shift.CAPS) return
        val want = if (on) Shift.ONCE else Shift.OFF
        if (shift != want) {
            shift = want
            invalidate()
        }
    }

    override fun onMeasure(widthMeasureSpec: Int, heightMeasureSpec: Int) {
        val w = MeasureSpec.getSize(widthMeasureSpec)
        val h = (rows.sumOf { it.height.toDouble() } * keyHeight + verticalPad * 2).toInt() + paddingBottom
        setMeasuredDimension(w, h)
    }

    override fun onSizeChanged(w: Int, h: Int, oldw: Int, oldh: Int) {
        layoutKeys()
    }

    override fun setPadding(left: Int, top: Int, right: Int, bottom: Int) {
        super.setPadding(left, top, right, bottom)
        layoutKeys()
    }

    private fun layoutKeys() {
        rects.clear()
        val left = paddingLeft.toFloat()
        val width = (this.width - paddingLeft - paddingRight).toFloat()
        if (width <= 0f) return
        var y = verticalPad
        for (row in rows) {
            val total = row.keys.sumOf { it.weight.toDouble() }.toFloat()
            val unit = width / total
            val h = row.height * keyHeight
            var x = left
            for (key in row.keys) {
                val w = key.weight * unit
                if (key.kind != Kind.SPACER) {
                    rects.add(key to RectF(x + gap / 2, y + gap / 2, x + w - gap / 2, y + h - gap / 2))
                }
                x += w
            }
            y += h
        }
    }

    // ── Drawing ────────────────────────────────────────────────────────

    override fun onDraw(canvas: Canvas) {
        canvas.drawColor(cBackground)
        for ((key, r) in rects) {
            val pressed = pressedRect(r)
            val accent = isAccent(key)
            fill.color = when {
                pressed -> cPressed
                accent -> cAccent
                key.kind == Kind.CHAR || key.kind == Kind.SPACE -> cKey
                else -> cSpecial
            }
            canvas.drawRoundRect(r, radius, radius, fill)

            text.color = if (accent && !pressed) cAccentText else cText
            text.typeface = Typeface.DEFAULT
            val label = labelOf(key)
            text.textSize = (if (key.kind == Kind.CHAR) 22f else 17f) * scaledDensity
            val baseline = r.centerY() - (text.descent() + text.ascent()) / 2
            canvas.drawText(label, r.centerX(), baseline, text)

            val alt = key.alt
            if (alt != null && key.kind == Kind.CHAR) {
                hint.color = cHint
                canvas.drawText(cased(alt), r.right - dp(5f), r.top + dp(14f), hint)
            }
            if (key.kind == Kind.SHIFT && shift == Shift.CAPS) {
                fill.color = cAccentText
                canvas.drawRect(r.centerX() - dp(8f), r.bottom - dp(9f), r.centerX() + dp(8f), r.bottom - dp(7f), fill)
            }
        }
    }

    private fun pressedRect(r: RectF): Boolean {
        for (i in 0 until touches.size()) if (touches.valueAt(i).rect === r) return true
        return false
    }

    private fun isAccent(key: Key) = when (key.kind) {
        Kind.ENTER -> true
        Kind.SHIFT -> shift != Shift.OFF
        Kind.CONVERT -> convertOn
        else -> false
    }

    private fun labelOf(key: Key) = when (key.kind) {
        Kind.CHAR -> cased(key.label)
        Kind.SYMBOLS -> if (symbols) "ABC" else "?123"
        else -> key.label
    }

    private fun cased(s: String) = if (symbols || shift == Shift.OFF) s else s.uppercase(Locale.ROOT)

    // ── Touch ──────────────────────────────────────────────────────────

    override fun onTouchEvent(e: MotionEvent): Boolean {
        when (e.actionMasked) {
            MotionEvent.ACTION_DOWN, MotionEvent.ACTION_POINTER_DOWN -> {
                val i = e.actionIndex
                down(e.getPointerId(i), e.getX(i), e.getY(i))
            }
            MotionEvent.ACTION_UP, MotionEvent.ACTION_POINTER_UP -> up(e.getPointerId(e.actionIndex))
            MotionEvent.ACTION_CANCEL -> {
                touches.clear()
                handler.removeCallbacksAndMessages(null)
                invalidate()
            }
        }
        return true
    }

    private fun keyAt(x: Float, y: Float): Pair<Key, RectF>? {
        var best: Pair<Key, RectF>? = null
        var bestDist = Float.MAX_VALUE
        for (p in rects) {
            val r = p.second
            // Thumbs land in the gaps: take the nearest key within half a gap.
            val dx = maxOf(r.left - x, 0f, x - r.right)
            val dy = maxOf(r.top - y, 0f, y - r.bottom)
            val d = dx * dx + dy * dy
            if (d < bestDist) {
                bestDist = d
                best = p
            }
        }
        return best?.takeIf { bestDist <= gap * gap }
    }

    private fun down(id: Int, x: Float, y: Float) {
        val (key, rect) = keyAt(x, y) ?: return
        val t = Touch(key, rect)
        touches.put(id, t)
        feedback()
        if (key.kind == Kind.DELETE) {
            listener?.onDelete()
            t.repeating = true
            handler.postDelayed(repeat(id), REPEAT_DELAY)
        } else if (key.kind == Kind.CHAR && key.alt != null) {
            handler.postDelayed({
                val live = touches.get(id)
                if (live === t && !t.longFired) {
                    t.longFired = true
                    feedback()
                    typeAlt(key)
                }
            }, LONG_PRESS)
        }
        invalidate()
    }

    private fun repeat(id: Int): Runnable = object : Runnable {
        override fun run() {
            if (touches.get(id)?.repeating != true) return
            listener?.onDelete()
            handler.postDelayed(this, REPEAT_INTERVAL)
        }
    }

    private fun up(id: Int) {
        val t = touches.get(id) ?: return
        touches.remove(id)
        handler.removeCallbacksAndMessages(null)
        // A repeat/long-press runnable of another finger was removed too: re-arm what is still held.
        for (i in 0 until touches.size()) {
            val other = touches.valueAt(i)
            if (other.repeating) handler.postDelayed(repeat(touches.keyAt(i)), REPEAT_INTERVAL)
        }
        if (!t.longFired && !t.repeating) fire(t.key)
        invalidate()
    }

    private fun fire(key: Key) {
        val l = listener ?: return
        when (key.kind) {
            Kind.CHAR -> {
                l.onText(cased(key.label))
                if (shift == Shift.ONCE) shift = Shift.OFF
                invalidate()
            }
            Kind.SPACE -> {
                l.onText(" ")
                if (shift == Shift.ONCE) shift = Shift.OFF
                invalidate()
            }
            Kind.SHIFT -> {
                val now = System.currentTimeMillis()
                shift = when {
                    shift == Shift.CAPS -> Shift.OFF
                    shift == Shift.ONCE && now - lastShiftTap < DOUBLE_TAP -> Shift.CAPS
                    shift == Shift.ONCE -> Shift.OFF
                    else -> Shift.ONCE
                }
                lastShiftTap = now
                invalidate()
            }
            Kind.SYMBOLS -> {
                symbols = !symbols
                rebuild()
            }
            Kind.ENTER -> l.onEnter()
            Kind.GLOBE -> l.onGlobe()
            Kind.CONVERT -> l.onToggleConvert()
            Kind.DELETE, Kind.SPACER -> Unit
        }
    }

    private fun typeAlt(key: Key) {
        val alt = key.alt ?: return
        listener?.onText(cased(alt))
        if (shift == Shift.ONCE) shift = Shift.OFF
        invalidate()
    }

    private fun feedback() {
        if (haptic) performHapticFeedback(HapticFeedbackConstants.KEYBOARD_TAP)
    }

    private companion object {
        const val LONG_PRESS = 350L
        const val REPEAT_DELAY = 400L
        const val REPEAT_INTERVAL = 50L
        const val DOUBLE_TAP = 400L
    }
}

