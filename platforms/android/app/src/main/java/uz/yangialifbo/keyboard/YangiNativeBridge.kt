package uz.yangialifbo.keyboard

/**
 * JNI Bridge connecting the Android InputMethodService to yangi-core via yangi-ffi.
 * Features a JVM fallback mechanism for headless unit tests.
 */
object YangiNativeBridge {

    private var nativeLoaded = false

    init {
        try {
            System.loadLibrary("yangi_ffi")
            nativeLoaded = true
        } catch (e: UnsatisfiedLinkError) {
            nativeLoaded = false
        }
    }

    val isNativeLoaded: Boolean get() = nativeLoaded

    @JvmStatic
    external fun convertWord(word: String, mode: String, slang: Boolean, useMl: Boolean): String

    @JvmStatic
    external fun convertText(text: String, mode: String, slang: Boolean, useMl: Boolean): String

    @JvmStatic
    external fun shouldConvert(word: String, mode: String, useMl: Boolean): Boolean

    @JvmStatic
    external fun isUzbek(word: String, mode: String, useMl: Boolean): Boolean

    @JvmStatic
    external fun isForeignOrReject(word: String): Boolean

    @JvmStatic
    external fun phoneticsConvert(word: String): String

    @JvmStatic
    external fun chatNormalize(word: String): String

    // High-level safe wrappers with JVM fallback
    fun transliterateWord(word: String, mode: String = "strict", slang: Boolean = false, useMl: Boolean = false): String {
        return if (nativeLoaded) {
            try {
                convertWord(word, mode, slang, useMl)
            } catch (e: Throwable) {
                fallbackConvertWord(word)
            }
        } else {
            fallbackConvertWord(word)
        }
    }

    fun canConvert(word: String, mode: String = "strict", useMl: Boolean = false): Boolean {
        return if (nativeLoaded) {
            try {
                shouldConvert(word, mode, useMl)
            } catch (e: Throwable) {
                fallbackShouldConvert(word)
            }
        } else {
            fallbackShouldConvert(word)
        }
    }

    // JVM Fallback rule-engine (used in unit tests when native .so is absent)
    private val REJECT_WORDS = setOf(
        "school", "chat", "share", "google", "she", "he", "the", "short", "shirt", "shift",
        "show", "shop", "cash", "fish", "dish", "push", "change", "check", "child", "choose",
        "each", "much", "such", "rich", "which", "match", "catch", "watch", "touch", "switch"
    )

    private fun fallbackShouldConvert(word: String): Boolean {
        val lower = word.lowercase()
        if (REJECT_WORDS.contains(lower)) return false
        if (lower.contains("is'hoq") || lower.contains("mus'haf") || lower.contains("as-hob")) return false
        return lower.contains("sh") || lower.contains("ch") || lower.contains("o'") || lower.contains("g'") ||
                lower.contains("o‘") || lower.contains("g‘") || lower.contains("o’") || lower.contains("g’")
    }

    private fun fallbackConvertWord(word: String): String {
        if (!fallbackShouldConvert(word)) return word
        return applyPhonetics(word)
    }

    internal fun applyPhonetics(word: String): String {
        var res = word
        // Casing and tutuq protection
        if (res.equals("Is'hoq", ignoreCase = true) || res.contains("mus'haf")) {
            return res
        }
        res = res.replace("sh", "ş")
            .replace("Sh", "Ş")
            .replace("SH", "Ş")
            .replace("ch", "ç")
            .replace("Ch", "Ç")
            .replace("CH", "Ç")
            .replace("o'", "ö")
            .replace("O'", "Ö")
            .replace("o‘", "ö")
            .replace("O‘", "Ö")
            .replace("o’", "ö")
            .replace("O’", "Ö")
            .replace("g'", "ğ")
            .replace("G'", "Ğ")
            .replace("g‘", "ğ")
            .replace("G‘", "Ğ")
            .replace("g’", "ğ")
            .replace("G’", "Ğ")
        return res
    }
}
