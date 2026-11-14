package uz.yangialifbo.keyboard

import android.content.Context
import android.content.SharedPreferences
import uz.yangialifbo.engine.Mode

/** Keyboard settings and the user word lists. The app owns persistence; the engine only gets the lists. */
class Settings(context: Context) {
    private val prefs: SharedPreferences = context.getSharedPreferences("settings", Context.MODE_PRIVATE)

    var convert: Boolean
        get() = prefs.getBoolean("convert", true)
        set(v) = prefs.edit().putBoolean("convert", v).apply()

    var slang: Boolean
        get() = prefs.getBoolean("slang", true)
        set(v) = prefs.edit().putBoolean("slang", v).apply()

    var neural: Boolean
        get() = prefs.getBoolean("neural", true)
        set(v) = prefs.edit().putBoolean("neural", v).apply()

    var haptic: Boolean
        get() = prefs.getBoolean("haptic", true)
        set(v) = prefs.edit().putBoolean("haptic", v).apply()

    var mode: Mode
        get() = if (prefs.getString("mode", "strict") == "aggressive") Mode.AGGRESSIVE else Mode.STRICT
        set(v) = prefs.edit().putString("mode", if (v == Mode.AGGRESSIVE) "aggressive" else "strict").apply()

    /** Words that always convert, one per line. */
    var whitelistText: String
        get() = prefs.getString("whitelist", "") ?: ""
        set(v) = prefs.edit().putString("whitelist", v).apply()

    /** Words that never convert, one per line. */
    var blacklistText: String
        get() = prefs.getString("blacklist", "") ?: ""
        set(v) = prefs.edit().putString("blacklist", v).apply()

    companion object {
        fun parseList(text: String): Set<String> =
            text.lineSequence().map { it.trim() }.filter { it.isNotEmpty() && !it.startsWith("#") }.toSet()
    }
}
