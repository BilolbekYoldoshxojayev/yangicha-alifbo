package uz.yangialifbo.engine

/** Port of WordConverter.cs: decides what one finished word becomes. */
internal object WordConverter {
    fun convert(detector: LanguageDetector, word: String, mode: Mode, slang: Boolean, neural: Boolean): String? {
        if (Dn.isNullOrWhiteSpace(word) || word.length < 2) return null

        // Tier 1: absolute protection for English and foreign words
        if (detector.isForeignOrReject(word)) return null

        val workWord = if (slang) ChatNormalizer.normalize(word) else word

        if (!Phonetics.hasConvertiblePatterns(workWord)) {
            // A slang rewrite without sh/ch/o'/g' is used only when the result is a known Uzbek word.
            if (workWord != word && detector.isUzbek(workWord)) return workWord
            return null
        }

        if (mode == Mode.AGGRESSIVE) return Phonetics.convert(workWord)

        // Strict: the neural fallback only judges words as typed, never slang rewrites.
        if (if (workWord == word) detector.isUzbekOrLikely(workWord, neural) else detector.isUzbek(workWord))
            return Phonetics.convert(workWord)

        return null
    }
}
