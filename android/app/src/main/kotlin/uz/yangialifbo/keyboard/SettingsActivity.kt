package uz.yangialifbo.keyboard

import android.app.Activity
import android.app.AlertDialog
import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.provider.Settings.ACTION_INPUT_METHOD_SETTINGS
import android.provider.Settings.Secure
import android.text.InputType
import android.view.View
import android.view.inputmethod.InputMethodManager
import android.widget.EditText
import android.widget.TextView
import uz.yangialifbo.engine.Mode

/** Conversion settings, the two word lists, and the optional YangiAlifbo keyboard. */
class SettingsActivity : Activity() {
    private lateinit var settings: Settings
    private lateinit var whitelist: NavRow
    private lateinit var blacklist: NavRow
    private lateinit var keyboardEnable: NavRow

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_settings)
        settings = Settings(this)
        applyInsets(findViewById(R.id.root))

        findViewById<View>(R.id.back).setOnClickListener { finish() }

        findViewById<ToggleRow>(R.id.slang).bind(settings.slang) { settings.slang = it }
        findViewById<ToggleRow>(R.id.neural).bind(settings.neural) { settings.neural = it }
        findViewById<ToggleRow>(R.id.haptic).bind(settings.haptic) { settings.haptic = it }

        val modeHint = findViewById<TextView>(R.id.modeHint)
        fun showModeHint(mode: Mode) =
            modeHint.setText(if (mode == Mode.AGGRESSIVE) R.string.mode_aggressive_hint else R.string.mode_strict_hint)
        showModeHint(settings.mode)
        findViewById<GlassSegment>(R.id.mode).setOptions(
            listOf(getString(R.string.mode_strict), getString(R.string.mode_aggressive)),
            if (settings.mode == Mode.AGGRESSIVE) 1 else 0,
        ) { index ->
            settings.mode = if (index == 1) Mode.AGGRESSIVE else Mode.STRICT
            showModeHint(settings.mode)
        }

        whitelist = findViewById(R.id.whitelist)
        blacklist = findViewById(R.id.blacklist)
        whitelist.setOnClickListener {
            editList(R.string.whitelist_title, settings.whitelistText) { settings.whitelistText = it }
        }
        blacklist.setOnClickListener {
            editList(R.string.blacklist_title, settings.blacklistText) { settings.blacklistText = it }
        }

        keyboardEnable = findViewById(R.id.keyboardEnable)
        keyboardEnable.setOnClickListener { startActivity(Intent(ACTION_INPUT_METHOD_SETTINGS)) }
        findViewById<View>(R.id.keyboardSelect).setOnClickListener {
            (getSystemService(Context.INPUT_METHOD_SERVICE) as InputMethodManager).showInputMethodPicker()
        }

        val version = packageManager.getPackageInfo(packageName, 0).versionName.orEmpty()
        findViewById<TextView>(R.id.version).text = getString(R.string.version_fmt, version)

        if (savedInstanceState == null) animateIn(findViewById(R.id.content))
    }

    override fun onResume() {
        super.onResume()
        refresh()
    }

    override fun onWindowFocusChanged(hasFocus: Boolean) {
        super.onWindowFocusChanged(hasFocus)
        if (hasFocus) refresh()
    }

    private fun refresh() {
        whitelist.setSubtitle(countText(settings.whitelistText))
        blacklist.setSubtitle(countText(settings.blacklistText))

        val imm = getSystemService(Context.INPUT_METHOD_SERVICE) as InputMethodManager
        val enabled = imm.enabledInputMethodList.any { it.packageName == packageName }
        val selected = Secure.getString(contentResolver, Secure.DEFAULT_INPUT_METHOD).orEmpty().startsWith("$packageName/")
        keyboardEnable.setSubtitle(
            getString(
                when {
                    enabled && selected -> R.string.keyboard_status_selected
                    enabled -> R.string.keyboard_status_on
                    else -> R.string.keyboard_status_off
                },
            ),
        )
    }

    private fun countText(text: String): String {
        val n = Settings.parseList(text).size
        return if (n == 0) getString(R.string.list_empty) else getString(R.string.list_count, n)
    }

    /** One word per line; saved when the dialog is confirmed and given to the engine at once. */
    private fun editList(title: Int, current: String, save: (String) -> Unit) {
        val field = EditText(this).apply {
            setText(current)
            setHint(R.string.list_hint)
            inputType = InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_FLAG_MULTI_LINE or InputType.TYPE_TEXT_FLAG_NO_SUGGESTIONS
            minLines = 6
            gravity = android.view.Gravity.TOP
            importantForAutofill = View.IMPORTANT_FOR_AUTOFILL_NO
        }
        val pad = dp(20f)
        val box = android.widget.FrameLayout(this).apply {
            setPadding(pad, dp(8f), pad, 0)
            addView(field)
        }
        AlertDialog.Builder(this)
            .setTitle(title)
            .setView(box)
            .setPositiveButton(R.string.save) { _, _ ->
                save(field.text.toString())
                EngineHolder.applyLists(settings)
                refresh()
            }
            .setNegativeButton(R.string.cancel, null)
            .show()
    }
}
