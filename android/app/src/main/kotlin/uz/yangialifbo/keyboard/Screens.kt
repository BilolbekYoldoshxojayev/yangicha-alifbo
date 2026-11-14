package uz.yangialifbo.keyboard

import android.app.Activity
import android.content.Context
import android.provider.Settings.Secure
import android.view.View
import android.view.ViewGroup
import android.view.animation.DecelerateInterpolator
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat

/** Pads the scrolling content by the system bars and the on-screen keyboard (edge-to-edge). */
fun applyInsets(root: View) {
    ViewCompat.setOnApplyWindowInsetsListener(root) { v, insets ->
        val bars = insets.getInsets(
            WindowInsetsCompat.Type.systemBars() or WindowInsetsCompat.Type.displayCutout() or WindowInsetsCompat.Type.ime(),
        )
        v.setPadding(bars.left, bars.top, bars.right, bars.bottom)
        WindowInsetsCompat.CONSUMED
    }
}

/** The panes rise into place one after another. */
fun animateIn(content: ViewGroup) {
    val rise = 24 * content.resources.displayMetrics.density
    for (i in 0 until content.childCount) {
        val child = content.getChildAt(i)
        child.alpha = 0f
        child.translationY = rise
        child.animate().alpha(1f).translationY(0f)
            .setStartDelay(60L * i).setDuration(420L)
            .setInterpolator(DecelerateInterpolator(2f)).start()
    }
}

/** Whether the accessibility service (conversion in any keyboard) is switched on in system settings. */
fun Context.isAccessibilityServiceOn(): Boolean {
    val services = Secure.getString(contentResolver, Secure.ENABLED_ACCESSIBILITY_SERVICES).orEmpty()
    return services.split(':').any { it.startsWith("$packageName/") }
}

fun Activity.dp(v: Float) = (v * resources.displayMetrics.density).toInt()
