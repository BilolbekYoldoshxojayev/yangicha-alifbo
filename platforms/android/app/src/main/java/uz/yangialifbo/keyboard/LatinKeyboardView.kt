package uz.yangialifbo.keyboard

import android.annotation.SuppressLint
import android.content.Context
import android.graphics.*
import android.os.Handler
import android.os.Looper
import android.util.AttributeSet
import android.view.MotionEvent
import android.view.View

/**
 * Lightweight, accessible Latin keyboard view rendered with YangiAlifbo design tokens.
 * Features long-press diacritic shortcuts for reformed Latin characters (ş, ç, ö, ğ).
 */
class LatinKeyboardView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
) : View(context, attrs, defStyleAttr) {

    interface OnKeyboardActionListener {
        fun onKey(primaryCode: Int)
        fun onText(text: CharSequence)
    }

    companion object {
        const val KEYCODE_DELETE = -5
        const val KEYCODE_ENTER = 10
        const val KEYCODE_SPACE = 32
        const val KEYCODE_SHIFT = -1
        const val KEYCODE_MODE_CHANGE = -2
    }

    private var listener: OnKeyboardActionListener? = null

    private var isShifted = false
    private var isSymbols = false

    // Color tokens
    private val colorDeep = Color.parseColor("#0E1846")
    private val colorLapis = Color.parseColor("#13205C")
    private val colorKeyPressed = Color.parseColor("#1E328A")
    private val colorTurquoise = Color.parseColor("#2FB3A6")
    private val colorActionPressed = Color.parseColor("#269186")
    private val colorSaffron = Color.parseColor("#F0B23A")
    private val colorPorcelain = Color.parseColor("#EEF1EC")

    private val bgPaint = Paint().apply { color = colorDeep }
    private val keyPaint = Paint(Paint.ANTI_ALIAS_FLAG)
    private val textPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = colorPorcelain
        textAlign = Paint.Align.CENTER
        typeface = Typeface.DEFAULT_BOLD
    }
    private val hintPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = colorSaffron
        textAlign = Paint.Align.RIGHT
        typeface = Typeface.DEFAULT_BOLD
    }

    data class KeyDef(
        val code: Int,
        val label: String,
        val diacriticHint: String? = null,
        val isAction: Boolean = false,
        var bounds: RectF = RectF()
    )

    private val keys = mutableListOf<KeyDef>()
    private var pressedKey: KeyDef? = null
    private val handler = Handler(Looper.getMainLooper())
    private var longPressTriggered = false

    private val longPressRunnable = Runnable {
        pressedKey?.let { key ->
            key.diacriticHint?.let { hint ->
                longPressTriggered = true
                val output = if (isShifted) hint.uppercase() else hint
                listener?.onText(output)
                invalidate()
            }
        }
    }

    fun setOnKeyboardActionListener(l: OnKeyboardActionListener) {
        this.listener = l
    }

    override fun onMeasure(widthMeasureSpec: Int, heightMeasureSpec: Int) {
        val width = MeasureSpec.getSize(widthMeasureSpec)
        val density = resources.displayMetrics.density
        val height = (240 * density).toInt() // Standard keyboard height ~240dp
        setMeasuredDimension(width, height)
    }

    override fun onSizeChanged(w: Int, h: Int, oldw: Int, oldh: Int) {
        super.onSizeChanged(w, h, oldw, oldh)
        layoutKeys(w, h)
    }

    private fun layoutKeys(w: Int, h: Int) {
        keys.clear()
        val rowHeight = h / 4f
        val padding = 4f

        // Row 1: Q W E R T Y U I O P
        val r1 = listOf("q", "w", "e", "r", "t", "y", "u", "i", "o", "p")
        val kw1 = w / r1.size.toFloat()
        for (i in r1.indices) {
            val label = r1[i]
            val hint = if (label == "o") "ö" else null
            keys.add(
                KeyDef(
                    label[0].code, label, hint,
                    bounds = RectF(i * kw1 + padding, padding, (i + 1) * kw1 - padding, rowHeight - padding)
                )
            )
        }

        // Row 2: A S D F G H J K L
        val r2 = listOf("a", "s", "d", "f", "g", "h", "j", "k", "l")
        val sidePad2 = (w - (w * 0.9f)) / 2f
        val kw2 = (w * 0.9f) / r2.size.toFloat()
        for (i in r2.indices) {
            val label = r2[i]
            val hint = when (label) {
                "s" -> "ş"
                "g" -> "ğ"
                else -> null
            }
            keys.add(
                KeyDef(
                    label[0].code, label, hint,
                    bounds = RectF(sidePad2 + i * kw2 + padding, rowHeight + padding, sidePad2 + (i + 1) * kw2 - padding, rowHeight * 2 - padding)
                )
            )
        }

        // Row 3: Shift, Z X C V B N M, Delete
        val r3 = listOf("z", "x", "c", "v", "b", "n", "m")
        val shiftWidth = w * 0.14f
        val delWidth = w * 0.14f
        val kw3 = (w - shiftWidth - delWidth) / r3.size.toFloat()

        keys.add(
            KeyDef(
                KEYCODE_SHIFT, "⇧", isAction = true,
                bounds = RectF(padding, rowHeight * 2 + padding, shiftWidth - padding, rowHeight * 3 - padding)
            )
        )

        for (i in r3.indices) {
            val label = r3[i]
            val hint = if (label == "c") "ç" else null
            keys.add(
                KeyDef(
                    label[0].code, label, hint,
                    bounds = RectF(shiftWidth + i * kw3 + padding, rowHeight * 2 + padding, shiftWidth + (i + 1) * kw3 - padding, rowHeight * 3 - padding)
                )
            )
        }

        keys.add(
            KeyDef(
                KEYCODE_DELETE, "⌫", isAction = true,
                bounds = RectF(w - delWidth + padding, rowHeight * 2 + padding, w - padding, rowHeight * 3 - padding)
            )
        )

        // Row 4: ?123, ,, Space, ., Enter
        val symWidth = w * 0.15f
        val commaWidth = w * 0.12f
        val dotWidth = w * 0.12f
        val enterWidth = w * 0.18f
        val spaceWidth = w - symWidth - commaWidth - dotWidth - enterWidth

        var curX = 0f
        keys.add(KeyDef(KEYCODE_MODE_CHANGE, "?123", isAction = true, bounds = RectF(curX + padding, rowHeight * 3 + padding, curX + symWidth - padding, rowHeight * 4 - padding)))
        curX += symWidth

        keys.add(KeyDef(','.code, ",", bounds = RectF(curX + padding, rowHeight * 3 + padding, curX + commaWidth - padding, rowHeight * 4 - padding)))
        curX += commaWidth

        keys.add(KeyDef(KEYCODE_SPACE, "Oʻzbekcha", isAction = true, bounds = RectF(curX + padding, rowHeight * 3 + padding, curX + spaceWidth - padding, rowHeight * 4 - padding)))
        curX += spaceWidth

        keys.add(KeyDef('.'.code, ".", bounds = RectF(curX + padding, rowHeight * 3 + padding, curX + dotWidth - padding, rowHeight * 4 - padding)))
        curX += dotWidth

        keys.add(KeyDef(KEYCODE_ENTER, "↵", isAction = true, bounds = RectF(curX + padding, rowHeight * 3 + padding, curX + enterWidth - padding, rowHeight * 4 - padding)))
    }

    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)
        canvas.drawRect(0f, 0f, width.toFloat(), height.toFloat(), bgPaint)

        val density = resources.displayMetrics.density
        val cornerRadius = 6 * density
        textPaint.textSize = 18 * density
        hintPaint.textSize = 11 * density

        for (k in keys) {
            val isPressed = (k == pressedKey)
            keyPaint.color = when {
                k.isAction && isPressed -> colorActionPressed
                k.isAction -> colorTurquoise
                isPressed -> colorKeyPressed
                else -> colorLapis
            }

            canvas.drawRoundRect(k.bounds, cornerRadius, cornerRadius, keyPaint)

            // Draw label
            val label = if (isShifted && k.code > 0) k.label.uppercase() else k.label
            val textY = k.bounds.centerY() - ((textPaint.descent() + textPaint.ascent()) / 2f)
            canvas.drawText(label, k.bounds.centerX(), textY, textPaint)

            // Draw diacritic hint in Saffron (ş, ç, ö, ğ)
            k.diacriticHint?.let { hint ->
                val hintLabel = if (isShifted) hint.uppercase() else hint
                canvas.drawText(hintLabel, k.bounds.right - 6 * density, k.bounds.top + 14 * density, hintPaint)
            }
        }
    }

    @SuppressLint("ClickableViewAccessibility")
    override fun onTouchEvent(event: MotionEvent): Boolean {
        val x = event.x
        val y = event.y

        when (event.action) {
            MotionEvent.ACTION_DOWN -> {
                val key = findKey(x, y)
                if (key != null) {
                    pressedKey = key
                    longPressTriggered = false
                    if (key.diacriticHint != null) {
                        handler.postDelayed(longPressRunnable, 350)
                    }
                    invalidate()
                }
            }
            MotionEvent.ACTION_UP -> {
                handler.removeCallbacks(longPressRunnable)
                pressedKey?.let { key ->
                    if (!longPressTriggered) {
                        handleKeyClick(key)
                    }
                }
                pressedKey = null
                longPressTriggered = false
                invalidate()
            }
            MotionEvent.ACTION_CANCEL -> {
                handler.removeCallbacks(longPressRunnable)
                pressedKey = null
                longPressTriggered = false
                invalidate()
            }
        }
        return true
    }

    private fun handleKeyClick(key: KeyDef) {
        when (key.code) {
            KEYCODE_SHIFT -> {
                isShifted = !isShifted
                invalidate()
            }
            else -> {
                val code = if (isShifted && key.code > 0) {
                    val upper = key.label.uppercase()
                    upper[0].code
                } else {
                    key.code
                }
                listener?.onKey(code)
            }
        }
    }

    private fun findKey(x: Float, y: Float): KeyDef? {
        return keys.firstOrNull { it.bounds.contains(x, y) }
    }
}
