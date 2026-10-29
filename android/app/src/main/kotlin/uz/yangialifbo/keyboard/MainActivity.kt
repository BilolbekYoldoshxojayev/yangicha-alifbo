package uz.yangialifbo.keyboard

import android.app.Activity
import android.app.AlertDialog
import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.provider.Settings.ACTION_ACCESSIBILITY_SETTINGS
import android.provider.Settings.ACTION_APPLICATION_DETAILS_SETTINGS
import android.view.View
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.TextView

/**
 * Home: one status and one action. While conversion is not switched on it shows a single
 * button and the three steps; once it works it shows the on/off switch and a place to try it.
 */
class MainActivity : Activity() {
    private lateinit var settings: Settings

    private lateinit var orb: StatusOrb
    private lateinit var stateTitle: TextView
    private lateinit var stateText: TextView
    private lateinit var enable: View
    private lateinit var convertRow: ToggleRow
    private lateinit var steps: View

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)
        settings = Settings(this)
        applyInsets(findViewById(R.id.root))

        orb = findViewById(R.id.orb)
        stateTitle = findViewById(R.id.stateTitle)
        stateText = findViewById(R.id.stateText)
        enable = findViewById(R.id.enable)
        convertRow = findViewById(R.id.convertRow)
        steps = findViewById(R.id.steps)

        findViewById<View>(R.id.settings).setOnClickListener { startActivity(Intent(this, SettingsActivity::class.java)) }
        enable.setOnClickListener { startActivity(Intent(ACTION_ACCESSIBILITY_SETTINGS)) }
        findViewById<View>(R.id.help).setOnClickListener { showHelp() }

        convertRow.bind(settings.convert) {
            settings.convert = it
            refresh()
        }

        val field = findViewById<EditText>(R.id.tryField)
        val chips = findViewById<LinearLayout>(R.id.chips)
        for (i in 0 until chips.childCount) {
            val chip = chips.getChildAt(i) as TextView
            // The word and a space, as typed: the same path the keyboards take. A text view only
            // reports changes to accessibility while it has focus.
            chip.setOnClickListener {
                field.requestFocus()
                field.append(chip.text.toString() + " ")
                field.setSelection(field.text.length)
            }
        }

        if (savedInstanceState == null) animateIn(findViewById(R.id.content))
    }

    override fun onResume() {
        super.onResume()
        refresh()
    }

    // The system settings screen returns focus without always resuming us.
    override fun onWindowFocusChanged(hasFocus: Boolean) {
        super.onWindowFocusChanged(hasFocus)
        if (hasFocus) refresh()
    }

    private fun refresh() {
        val serviceOn = isAccessibilityServiceOn()
        val running = serviceOn && settings.convert
        orb.state = when {
            !serviceOn -> StatusOrb.State.OFF
            running -> StatusOrb.State.ON
            else -> StatusOrb.State.PAUSED
        }
        when (orb.state) {
            StatusOrb.State.OFF -> {
                stateTitle.setText(R.string.state_off_title)
                stateText.setText(R.string.state_off_text)
            }
            StatusOrb.State.ON -> {
                stateTitle.setText(R.string.state_on_title)
                stateText.setText(R.string.state_on_text)
            }
            StatusOrb.State.PAUSED -> {
                stateTitle.setText(R.string.state_paused_title)
                stateText.setText(R.string.state_paused_text)
            }
        }
        enable.visibility = if (serviceOn) View.GONE else View.VISIBLE
        steps.visibility = if (serviceOn) View.GONE else View.VISIBLE
        convertRow.visibility = if (serviceOn) View.VISIBLE else View.GONE
        convertRow.bind(settings.convert) {
            settings.convert = it
            refresh()
        }
    }

    private fun showHelp() {
        AlertDialog.Builder(this)
            .setTitle(R.string.help_title)
            .setMessage(R.string.help_text)
            .setPositiveButton(R.string.help_open) { _, _ ->
                startActivity(Intent(ACTION_APPLICATION_DETAILS_SETTINGS, Uri.fromParts("package", packageName, null)))
            }
            .setNegativeButton(R.string.close, null)
            .show()
    }
}
