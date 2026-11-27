package uz.yangialifbo.keyboard

import android.animation.ValueAnimator
import android.content.Context
import android.content.res.ColorStateList
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.ColorFilter
import android.graphics.LinearGradient
import android.graphics.Paint
import android.graphics.PixelFormat
import android.graphics.RadialGradient
import android.graphics.Rect
import android.graphics.RectF
import android.graphics.Shader
import android.graphics.Typeface
import android.graphics.drawable.Drawable
import android.graphics.drawable.GradientDrawable
import android.graphics.drawable.RippleDrawable
import android.util.AttributeSet
import android.view.Gravity
import android.view.View
import android.view.animation.LinearInterpolator
import android.widget.Button
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.TextView
import androidx.core.content.ContextCompat
import kotlin.math.cos
import kotlin.math.max
import kotlin.math.sin

/*
 * The "glass" look: soft coloured light behind, panes of translucent white on top, each with a
 * thin bright edge that fades towards the opposite corner. Everything is drawn with plain
 * canvas calls (no blur API), so it behaves the same on every Android version.
 */

private fun Context.color(id: Int) = ContextCompat.getColor(this, id)

private fun Context.dp(v: Float) = v * resources.displayMetrics.density

/** A translucent rounded pane with a gradient edge, or the accent-filled variant for primary buttons. */
class GlassDrawable(context: Context, radiusDp: Float, private val kind: Kind) : Drawable() {
    enum class Kind { CARD, FIELD, BUTTON, PRIMARY }

    private val radius = context.dp(radiusDp)
    private val stroke = context.dp(1f)

    // A shader is multiplied by the paint's alpha, so the accent fill needs an opaque paint colour.
    private val fill = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        color = if (kind == Kind.PRIMARY) Color.BLACK else context.color(if (kind == Kind.FIELD) R.color.glass_field else R.color.glass_fill)
    }
    private val accent = context.color(R.color.glass_accent)
    private val accent2 = context.color(R.color.glass_accent2)
    private val borderHi = context.color(R.color.glass_border_hi)
    private val borderLo = context.color(R.color.glass_border_lo)

    private val border = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        style = Paint.Style.STROKE
        strokeWidth = stroke
    }
    private val edge = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        style = Paint.Style.STROKE
        strokeWidth = stroke
        color = context.color(R.color.glass_edge)
    }
    private val rect = RectF()

    override fun onBoundsChange(bounds: Rect) {
        rect.set(bounds)
        rect.inset(stroke, stroke)
        if (kind == Kind.PRIMARY) {
            fill.shader = LinearGradient(rect.left, rect.top, rect.right, rect.bottom, accent, accent2, Shader.TileMode.CLAMP)
        }
        val hi = if (kind == Kind.PRIMARY) Color.argb(120, 255, 255, 255) else borderHi
        val lo = if (kind == Kind.PRIMARY) Color.argb(20, 255, 255, 255) else borderLo
        border.shader = LinearGradient(rect.left, rect.top, rect.right, rect.bottom, hi, lo, Shader.TileMode.CLAMP)
    }

    override fun draw(canvas: Canvas) {
        canvas.drawRoundRect(rect, radius, radius, fill)
        if (kind != Kind.PRIMARY) canvas.drawRoundRect(rect, radius, radius, edge)
        canvas.drawRoundRect(rect, radius, radius, border)
    }

    override fun setAlpha(alpha: Int) = Unit
    override fun setColorFilter(colorFilter: ColorFilter?) = Unit

    @Deprecated("Deprecated in Java")
    override fun getOpacity() = PixelFormat.TRANSLUCENT
}

/** A pane that groups content. */
class GlassCard(context: Context, attrs: AttributeSet? = null) : LinearLayout(context, attrs) {
    init {
        orientation = VERTICAL
        val pad = context.dp(20f).toInt()
        setPadding(pad, pad, pad, pad)
        background = GlassDrawable(context, 28f, GlassDrawable.Kind.CARD)
    }
}

/** A small pane with one short label, e.g. the sh → ş chips. */
class GlassChip(context: Context, attrs: AttributeSet? = null) : TextView(context, attrs) {
    init {
        background = GlassDrawable(context, 16f, GlassDrawable.Kind.FIELD)
        setTextColor(context.color(R.color.glass_text))
        textSize = 16f
        typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        gravity = Gravity.CENTER
        val h = context.dp(8f).toInt()
        val v = context.dp(12f).toInt()
        setPadding(h, v, h, v)
    }
}

class GlassEditText(context: Context, attrs: AttributeSet? = null) : EditText(context, attrs) {
    init {
        background = GlassDrawable(context, 18f, GlassDrawable.Kind.FIELD)
        setTextColor(context.color(R.color.glass_text))
        setHintTextColor(context.color(R.color.glass_muted))
        textSize = 16f
        val p = context.dp(16f).toInt()
        setPadding(p, p, p, p)
    }
}

/** A pill button: glass, or filled with the accent gradient when `app:primary="true"`. */
class GlassButton(context: Context, attrs: AttributeSet? = null) : Button(context, attrs) {
    init {
        var primary = false
        if (attrs != null) {
            val a = context.obtainStyledAttributes(attrs, R.styleable.GlassButton)
            try {
                primary = a.getBoolean(R.styleable.GlassButton_primary, false)
            } finally {
                a.recycle()
            }
        }
        isAllCaps = false
        stateListAnimator = null
        gravity = Gravity.CENTER
        textSize = 15f
        typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        minHeight = context.dp(54f).toInt()
        setTextColor(context.color(if (primary) R.color.glass_on_accent else R.color.glass_text))

        val radius = context.dp(27f)
        val shape = GlassDrawable(context, 27f, if (primary) GlassDrawable.Kind.PRIMARY else GlassDrawable.Kind.BUTTON)
        val mask = GradientDrawable().apply {
            cornerRadius = radius
            setColor(Color.BLACK)
        }
        background = RippleDrawable(ColorStateList.valueOf(context.color(R.color.glass_ripple)), shape, mask)
    }
}

/** The slowly drifting coloured light the glass panes sit on. */
class AuroraBackground(context: Context, attrs: AttributeSet? = null) : View(context, attrs) {
    private class Blob(val color: Int, val x: Float, val y: Float, val radius: Float, val speed: Float, val phase: Float)

    private val blobs = listOf(
        Blob(context.color(R.color.aurora_1), 0.15f, 0.10f, 0.62f, 1f, 0.0f),
        Blob(context.color(R.color.aurora_2), 0.90f, 0.30f, 0.55f, 1f, 0.35f),
        Blob(context.color(R.color.aurora_3), 0.10f, 0.62f, 0.50f, 2f, 0.7f),
        Blob(context.color(R.color.aurora_4), 0.85f, 0.92f, 0.55f, 1f, 0.15f),
    )

    private val basePaint = Paint()
    private val blobPaints = blobs.map { Paint(Paint.ANTI_ALIAS_FLAG) }
    private var t = 0f

    private val animator = ValueAnimator.ofFloat(0f, 1f).apply {
        duration = 48_000L
        repeatCount = ValueAnimator.INFINITE
        interpolator = LinearInterpolator()
        addUpdateListener {
            t = it.animatedFraction
            invalidate()
        }
    }

    override fun onSizeChanged(w: Int, h: Int, oldw: Int, oldh: Int) {
        basePaint.shader = LinearGradient(
            0f, 0f, 0f, h.toFloat(),
            context.color(R.color.aurora_top), context.color(R.color.aurora_bottom), Shader.TileMode.CLAMP,
        )
        val big = max(w, h).toFloat()
        // Centred on the origin: each frame only moves the canvas, nothing is reallocated.
        blobs.forEachIndexed { i, b ->
            val r = b.radius * big
            blobPaints[i].shader = RadialGradient(
                0f, 0f, r, b.color, b.color and 0x00FFFFFF, Shader.TileMode.CLAMP,
            )
        }
    }

    override fun onDraw(canvas: Canvas) {
        canvas.drawPaint(basePaint)
        val w = width.toFloat()
        val h = height.toFloat()
        val big = max(w, h)
        blobs.forEachIndexed { i, b ->
            val a = (2 * Math.PI * (t * b.speed + b.phase)).toFloat()
            val cx = (b.x + 0.07f * sin(a)) * w
            val cy = (b.y + 0.05f * cos(a * 1.3f)) * h
            canvas.save()
            canvas.translate(cx, cy)
            canvas.drawCircle(0f, 0f, b.radius * big, blobPaints[i])
            canvas.restore()
        }
    }

    override fun onAttachedToWindow() {
        super.onAttachedToWindow()
        animator.start()
    }

    override fun onDetachedFromWindow() {
        animator.cancel()
        super.onDetachedFromWindow()
    }

    override fun onWindowVisibilityChanged(visibility: Int) {
        super.onWindowVisibilityChanged(visibility)
        if (!animator.isStarted) return
        if (visibility == VISIBLE) animator.resume() else animator.pause()
    }
}
