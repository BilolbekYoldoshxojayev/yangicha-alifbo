package uz.yangialifbo.keyboard

import android.app.Activity
import android.content.Context
import android.os.Bundle
import android.provider.Settings.ACTION_INPUT_METHOD_SETTINGS
import android.provider.Settings.Secure
import android.content.Intent
import android.view.inputmethod.InputMethodManager
import android.widget.Button
import android.widget.EditText
import android.widget.RadioButton
import android.widget.Switch
import android.widget.TextView
import android.widget.Toast
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat
import uz.yangialifbo.engine.Mode

/** Setup steps (enable, select), the keyboard settings and a field to try it in. */
class MainActivity : Activity() {
    private lateinit var settings: Settings
    private lateinit var status: TextView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)
        settings = Settings(this)

        val root = findViewById<android.view.View>(R.id.root)
        ViewCompat.setOnApplyWindowInsetsListener(root) { v, insets ->
            val bars = insets.getInsets(
                WindowInsetsCompat.Type.systemBars() or WindowInsetsCompat.Type.displayCutout() or WindowInsetsCompat.Type.ime(),
            )
            v.setPadding(bars.left, bars.top, bars.right, bars.bottom)
            WindowInsetsCompat.CONSUMED
        }

        status = findViewById(R.id.status)
        findViewById<Button>(R.id.enable).setOnClickListener { startActivity(Intent(ACTION_INPUT_METHOD_SETTINGS)) }
        findViewById<Button>(R.id.select).setOnClickListener {
            (getSystemService(Context.INPUT_METHOD_SERVICE) as InputMethodManager).showInputMethodPicker()
        }

        bindSwitch(R.id.convert, settings::convert)
        bindSwitch(R.id.slang, settings::slang)
        bindSwitch(R.id.neural, settings::neural)
        bindSwitch(R.id.haptic, settings::haptic)

        val strict = findViewById<RadioButton>(R.id.modeStrict)
        val aggressive = findViewById<RadioButton>(R.id.modeAggressive)
        (if (settings.mode == Mode.AGGRESSIVE) aggressive else strict).isChecked = true
        strict.setOnClickListener { settings.mode = Mode.STRICT }
        aggressive.setOnClickListener { settings.mode = Mode.AGGRESSIVE }

        val whitelist = findViewById<EditText>(R.id.whitelist)
        val blacklist = findViewById<EditText>(R.id.blacklist)
        whitelist.setText(settings.whitelistText)
        blacklist.setText(settings.blacklistText)
        findViewById<Button>(R.id.save).setOnClickListener {
            settings.whitelistText = whitelist.text.toString()
            settings.blacklistText = blacklist.text.toString()
            EngineHolder.applyLists(settings)
            Toast.makeText(this, R.string.saved, Toast.LENGTH_SHORT).show()
        }

        val version = packageManager.getPackageInfo(packageName, 0).versionName.orEmpty()
        findViewById<TextView>(R.id.version).text = getString(R.string.version_fmt, version)
    }

    override fun onResume() {
        super.onResume()
        refreshStatus()
    }

    // The keyboard picker is a dialog: closing it returns focus without a resume.
    override fun onWindowFocusChanged(hasFocus: Boolean) {
        super.onWindowFocusChanged(hasFocus)
        if (hasFocus) refreshStatus()
    }

    private fun refreshStatus() {
        val imm = getSystemService(Context.INPUT_METHOD_SERVICE) as InputMethodManager
        val enabled = imm.enabledInputMethodList.any { it.packageName == packageName }
        val current = Secure.getString(contentResolver, Secure.DEFAULT_INPUT_METHOD).orEmpty()
        val selected = current.startsWith("$packageName/")
        status.text = listOf(
            getString(if (enabled) R.string.status_enabled else R.string.status_disabled),
            getString(if (selected) R.string.status_selected else R.string.status_not_selected),
        ).joinToString("\n")
    }

    private fun bindSwitch(id: Int, prop: kotlin.reflect.KMutableProperty0<Boolean>) {
        val s = findViewById<Switch>(id)
        s.isChecked = prop.get()
        s.setOnCheckedChangeListener { _, checked -> prop.set(checked) }
    }
}
