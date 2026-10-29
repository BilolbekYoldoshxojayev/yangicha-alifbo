package uz.yangialifbo.engine

/** Strict converts only words confirmed Uzbek; aggressive converts every word with sh/ch/o'/g'. */
enum class Mode { STRICT, AGGRESSIVE }

/**
 * The Uzbek old-Latin -> new-alphabet conversion engine (Kotlin port of YangiAlifbo.Core).
 * Loads its dictionaries and neural model from classpath resources.
 */
class YangiEngine {
    private val detector: LanguageDetector

    /** Loads dictionaries + neural model from classpath resources. Throws on failure. Thread-safe afterwards. */
    constructor() {
        val englishReject = LanguageDetector.loadWordList("english_reject.br")
        val forms = CompactWordSet.fromBrotliResource("uzbek_forms.br")
        val foreign = CompactWordSet.fromBrotliResource("foreign_words.br")
        val neural = NeuralFallback.loadEmbedded()
        detector = LanguageDetector(englishReject, foreign, forms, neural)
    }

    /**
     * Returns the converted word, or null when the word stays as typed
     * (also null when the result equals the input).
     */
    fun convert(word: String, mode: Mode = Mode.STRICT, slang: Boolean = true, neural: Boolean = true): String? {
        val result = WordConverter.convert(detector, word, mode, slang, neural)
        return if (result == null || result == word) null else result
    }

    /** Replaces the user whitelist/blacklist (matched like the C# user lists). */
    fun setUserLists(whitelist: Set<String>, blacklist: Set<String>) {
        detector.setUserLists(whitelist, blacklist)
    }
}
