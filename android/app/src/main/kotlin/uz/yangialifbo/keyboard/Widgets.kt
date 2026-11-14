package uz.yangialifbo.keyboard

import android.animation.ValueAnimator
import android.content.Context
import android.content.res.ColorStateList
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.LinearGradient
import android.graphics.Paint
import android.graphics.Path
import android.graphics.RadialGradient
import android.graphics.RectF
import android.graphics.Shader
import android.graphics.drawable.GradientDrawable
import android.graphics.drawable.RippleDrawable
import android.util.AttributeSet
import android.view.Gravity
import android.view.View
import android.view.animation.LinearInterpolator
import android.view.animation.OvershootInterpolator
import android.widget.LinearLayout
import android.widget.Switch
import android.widget.TextView
import androidx.core.content.ContextCompat

private fun Context.color(id: Int) = ContextCompat.getColor(this, id)

private fun Context.dp(v: Float) = v * resources.displayMetrics.density

/** A tappable list row: title, optional second line, and a chevron. */
class NavRow(context: Context, attrs: AttributeSet? = null) : LinearLayout(context, attrs) {
    private val subtitleView: TextView

    init {
        orientation = HORIZONTAL
        gravity = Gravity.CENTER_VERTICAL
        minimumHeight = context.dp(64f).toInt()
        val pad = context.dp(4f).toInt()
        setPadding(pad, pad, pad, pad)
        isClickable = true
        isFocusable = true
        background = RippleDrawable(
            ColorStateList.valueOf(context.color(R.color.glass_ripple)), null,
            GradientDrawable().apply { cornerRadius = context.dp(16f); setColor(Color.BLACK) },
        )

        val a = context.obtainStyledAttributes(attrs, R.styleable.Row)
        val title = a.getString(R.styleable.Row_title).orEmpty()
        val subtitle = a.getString(R.styleable.Row_subtitle)
        a.recycle()

        val texts = LinearLayout(context).apply { orientation = VERTICAL }
        texts.addView(TextView(context).apply {
            text = title
            textSize = 16f
            setTextColor(context.color(R.color.glass_text))
        })
        subtitleView = TextView(context).apply {
            textSize = 13f
            setTextColor(context.color(R.color.glass_muted))
            this.text = subtitle
            visibility = if (subtitle.isNullOrEmpty()) GONE else VISIBLE
        }
        texts.addView(subtitleView)
        addView(texts, LayoutParams(0, LayoutParams.WRAP_CONTENT, 1f))

        addView(TextView(context).apply {
            text = "›"
            textSize = 26f
            setTextColor(context.color(R.color.glass_muted))
            setPadding(context.dp(8f).toInt(), 0, context.dp(8f).toInt(), 0)
        })
    }

    fun setSubtitle(text: CharSequence?) {
        subtitleView.text = text
        subtitleView.visibility = if (text.isNullOrEmpty()) GONE else VISIBLE
    }
}

/** A setting with a title, an optional explanation and a switch. */
class ToggleRow(context: Context, attrs: AttributeSet? = null) : LinearLayout(context, attrs) {
    val toggle: Switch

    init {
        orientation = HORIZONTAL
        gravity = Gravity.CENTER_VERTICAL
        minimumHeight = context.dp(64f).toInt()
        val pad = context.dp(4f).toInt()
        setPadding(pad, pad, pad, pad)

        val a = context.obtainStyledAttributes(attrs, R.styleable.Row)
        val title = a.getString(R.styleable.Row_title).orEmpty()
        val subtitle = a.getString(R.styleable.Row_subtitle)
        a.recycle()

        val texts = LinearLayout(context).apply { orientation = VERTICAL }
        texts.addView(TextView(context).apply {
            text = title
            textSize = 16f
            setTextColor(context.color(R.color.glass_text))
        })
        if (!subtitle.isNullOrEmpty()) {
            texts.addView(TextView(context).apply {
                text = subtitle
                textSize = 13f
                setTextColor(context.color(R.color.glass_muted))
            })
        }
        addView(texts, LayoutParams(0, LayoutParams.WRAP_CONTENT, 1f))

        toggle = Switch(context).apply {
            trackTintList = ContextCompat.getColorStateList(context, R.color.switch_track)
            thumbTintList = ContextCompat.getColorStateList(context, R.color.switch_thumb)
        }
        addView(toggle)
        setOnClickListener { toggle.toggle() }
    }

    /** Sets the state, then reports later changes (not this one). */
    fun bind(initial: Boolean, onChange: (Boolean) -> Unit) {
        toggle.setOnCheckedChangeListener(null)
        toggle.isChecked = initial
        toggle.setOnCheckedChangeListener { _, checked -> onChange(checked) }
    }
}

/** Two or more exclusive choices in one glass capsule. */
class GlassSegment(context: Context, attrs: AttributeSet? = null) : LinearLayout(context, attrs) {
    private val labels = ArrayList<TextView>()
    private var onSelect: ((Int) -> Unit)? = null

    init {
        orientation = HORIZONTAL
        val pad = context.dp(4f).toInt()
        setPadding(pad, pad, pad, pad)
        background = GlassDrawable(context, 22f, GlassDrawable.Kind.FIELD)
    }

    fun setOptions(options: List<String>, selected: Int, onSelect: (Int) -> Unit) {
        this.onSelect = onSelect
        removeAllViews()
        labels.clear()
        options.forEachIndexed { i, name ->
            val v = TextView(context).apply {
                text = name
                textSize = 15f
                gravity = Gravity.CENTER
                typeface = android.graphics.Typeface.DEFAULT_BOLD
                minHeight = context.dp(44f).toInt()
                setOnClickListener { select(i, notify = true) }
            }
            labels.add(v)
            addView(v, LayoutParams(0, LayoutParams.WRAP_CONTENT, 1f))
        }
        select(selected, notify = false)
    }

    private fun select(index: Int, notify: Boolean) {
        labels.forEachIndexed { i, v ->
            val on = i == index
            v.background = if (on) GlassDrawable(context, 18f, GlassDrawable.Kind.PRIMARY) else null
            v.setTextColor(context.color(if (on) R.color.glass_on_accent else R.color.glass_muted))
        }
        if (notify) onSelect?.invoke(index)
    }
}

/**
 * The big round status mark on the home screen: a power sign (not set up), a pause sign
 * (stopped by the user) or a tick (working), with a soft glow that breathes while working.
 */
class StatusOrb(context: Context, attrs: AttributeSet? = null) : View(context, attrs) {
    enum class State { OFF, PAUSED, ON }

    var state = State.OFF
        set(value) {
            if (field == value) return
            field = value
            rebuild()
            if (isAttachedToWindow) pop()
            updatePulse()
            invalidate()
        }

    private val glow = Paint(Paint.ANTI_ALIAS_FLAG)
    private val disc = Paint(Paint.ANTI_ALIAS_FLAG)
    private val ring = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        style = Paint.Style.STROKE
        strokeWidth = context.dp(1.5f)
        color = Color.argb(90, 255, 255, 255)
    }
    private val glyph = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        style = Paint.Style.STROKE
        strokeCap = Paint.Cap.ROUND
        strokeJoin = Paint.Join.ROUND
        color = Color.WHITE
    }
    private val path = Path()
    private val arc = RectF()
    private var breathe = 0f

    private val pulse = ValueAnimator.ofFloat(0f, 1f).apply {
        duration = 2600L
        repeatCount = ValueAnimator.INFINITE
        repeatMode = ValueAnimator.REVERSE
        interpolator = LinearInterpolator()
        addUpdateListener {
            breathe = it.animatedValue as Float
            invalidate()
        }
    }

    private fun colors() = when (state) {
        State.ON -> intArrayOf(0xFF3DDC97.toInt(), 0xFF14B8A6.toInt())
        State.PAUSED -> intArrayOf(0xFFFFB454.toInt(), 0xFFFF8A5B.toInt())
        State.OFF -> intArrayOf(context.color(R.color.glass_accent), context.color(R.color.glass_accent2))
    }

    private fun rebuild() {
        val r = discRadius()
        val cx = width / 2f
        val cy = height / 2f
        if (r <= 0f) return
        val c = colors()
        disc.shader = LinearGradient(cx, cy - r, cx, cy + r, c[0], c[1], Shader.TileMode.CLAMP)
        val g = c[0]
        glow.shader = RadialGradient(
            cx, cy, r * 1.75f, Color.argb(120, Color.red(g), Color.green(g), Color.blue(g)), Color.argb(0, Color.red(g), Color.green(g), Color.blue(g)),
            Shader.TileMode.CLAMP,
        )
    }

    private fun discRadius() = minOf(width, height) / 2f * 0.56f

    override fun onSizeChanged(w: Int, h: Int, oldw: Int, oldh: Int) = rebuild()

    override fun onDraw(canvas: Canvas) {
        val cx = width / 2f
        val cy = height / 2f
        val r = discRadius()
        val scale = if (state == State.ON) 1f + 0.12f * breathe else 1f
        canvas.save()
        canvas.scale(scale, scale, cx, cy)
        canvas.drawCircle(cx, cy, r * 1.75f, glow)
        canvas.restore()

        canvas.drawCircle(cx, cy, r, disc)
        canvas.drawCircle(cx, cy, r - ring.strokeWidth / 2, ring)

        glyph.strokeWidth = r * 0.17f
        path.reset()
        when (state) {
            State.ON -> {
                path.moveTo(cx - r * 0.34f, cy + r * 0.02f)
                path.lineTo(cx - r * 0.1f, cy + r * 0.27f)
                path.lineTo(cx + r * 0.36f, cy - r * 0.24f)
                canvas.drawPath(path, glyph)
            }
            State.PAUSED -> {
                canvas.drawLine(cx - r * 0.2f, cy - r * 0.3f, cx - r * 0.2f, cy + r * 0.3f, glyph)
                canvas.drawLine(cx + r * 0.2f, cy - r * 0.3f, cx + r * 0.2f, cy + r * 0.3f, glyph)
            }
            State.OFF -> {
                val pr = r * 0.34f
                arc.set(cx - pr, cy - pr + r * 0.04f, cx + pr, cy + pr + r * 0.04f)
                canvas.drawArc(arc, -50f, 280f, false, glyph)
                canvas.drawLine(cx, cy - r * 0.38f, cx, cy - r * 0.04f, glyph)
            }
        }
    }

    private fun pop() {
        scaleX = 0.8f
        scaleY = 0.8f
        animate().scaleX(1f).scaleY(1f).setDuration(420L).setInterpolator(OvershootInterpolator(2.2f)).start()
    }

    private fun updatePulse() {
        if (!isAttachedToWindow) return
        if (state == State.ON) {
            if (!pulse.isStarted) pulse.start()
        } else {
            pulse.cancel()
            breathe = 0f
        }
    }

    override fun onAttachedToWindow() {
        super.onAttachedToWindow()
        updatePulse()
    }

    override fun onDetachedFromWindow() {
        pulse.cancel()
        super.onDetachedFromWindow()
    }
}

/** A round glass button with an icon. */
class GlassIconButton(context: Context, attrs: AttributeSet? = null) : android.widget.ImageButton(context, attrs) {
    init {
        val glass = GlassDrawable(context, 24f, GlassDrawable.Kind.BUTTON)
        val mask = GradientDrawable().apply {
            shape = GradientDrawable.OVAL
            setColor(Color.BLACK)
        }
        background = RippleDrawable(ColorStateList.valueOf(context.color(R.color.glass_ripple)), glass, mask)
        scaleType = ScaleType.CENTER
        stateListAnimator = null
    }
}
