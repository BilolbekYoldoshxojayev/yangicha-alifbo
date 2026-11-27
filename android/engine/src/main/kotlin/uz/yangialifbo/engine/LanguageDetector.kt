package uz.yangialifbo.engine

import java.util.concurrent.ConcurrentHashMap

/**
 * Port of LanguageDetector.cs (strict mode only): user lists -> English/foreign reject ->
 * Uzbek lexicon (old spelling) -> morphology -> neural fallback. User-list files, file watching
 * and persistence are owned by the app; lists arrive through [setUserLists].
 */
internal class LanguageDetector(
    private val englishReject: FoldedSet,
    private val foreign: CompactWordSet?,
    private val uzbekLexicon: CompactWordSet,
    var neural: NeuralFallback?,
) {
    @Volatile private var userWhitelist = FoldedSet(HashSet())
    @Volatile private var userBlacklist = FoldedSet(HashSet())
    private val lookupCache = ConcurrentHashMap<String, Boolean>()
    private val neuralCache = ConcurrentHashMap<String, Boolean>()

    /** Replaces the user lists the way the C# loader reads user files: trimmed, '#'/';' comments skipped, normalized. */
    fun setUserLists(whitelist: Collection<String>, blacklist: Collection<String>) {
        userWhitelist = foldedSetOf(prepare(whitelist))
        userBlacklist = foldedSetOf(prepare(blacklist))
        lookupCache.clear()
    }

    private fun prepare(words: Collection<String>): List<String> {
        val out = ArrayList<String>(words.size)
        for (w in words) {
            val t = Dn.trim(w)
            if (t.isNotEmpty() && t[0] != '#' && t[0] != ';') out.add(normalizeForLookup(t))
        }
        return out
    }

    private fun isAllDigits(s: String, end: Int): Boolean {
        if (end == 0) return false
        for (i in 0 until end) if (!Dn.isDigit(s[i])) return false
        return true
    }

    /** Tier 1: user blacklist or English/foreign reject list. */
    fun isForeignOrReject(word: String): Boolean {
        if (Dn.isNullOrWhiteSpace(word)) return false
        val normalized = normalizeForLookup(word)

        if (userBlacklist.contains(normalized)) return true
        if (userWhitelist.contains(normalized)) return false

        val hyphenIdx = normalized.indexOf('-')
        if (hyphenIdx > 0) {
            if (isAllDigits(normalized, hyphenIdx)) {
                val suffix = normalized.substring(hyphenIdx + 1)
                if (ORDINAL_SUFFIXES.contains(suffix)) return false
                return isRejectWord(suffix)
            }
            val part1 = normalized.substring(0, hyphenIdx)
            val part2 = normalized.substring(hyphenIdx + 1)
            if (part2 == "chi") return isRejectWord(part1)
            if (isRejectWord(part1) || isRejectWord(part2)) return true
        }
        return isRejectWord(normalized)
    }

    /** Strict-mode check for a word exactly as typed: [isUzbek], then the neural model. */
    fun isUzbekOrLikely(word: String, neuralEnabled: Boolean): Boolean {
        if (isUzbek(word)) return true
        val model = neural
        if (!neuralEnabled || model == null || Dn.isNullOrWhiteSpace(word)) return false

        val normalized = normalizeForLookup(word)
        if (userBlacklist.contains(normalized) || isForeignOrReject(normalized)) return false
        if (!NeuralFallback.isEligible(normalized)) return false

        val cached = neuralCache[normalized]
        if (cached != null) return cached
        val result = model.accepts(normalized)
        if (neuralCache.size < 10000) neuralCache.putIfAbsent(normalized, result)
        return result
    }

    fun isUzbek(word: String): Boolean {
        if (Dn.isNullOrWhiteSpace(word) || word.length < 2) return false
        val normalized = normalizeForLookup(word)
        val cached = lookupCache[normalized]
        if (cached != null) return cached
        val result = computeIsUzbek(normalized)
        if (lookupCache.size < 10000) lookupCache.putIfAbsent(normalized, result)
        return result
    }

    private fun computeIsUzbek(normalized: String): Boolean {
        if (userBlacklist.contains(normalized)) return false
        if (userWhitelist.contains(normalized)) return true

        // Tier 1
        if (isRejectWord(normalized)) return false

        val hyphen = normalized.indexOf('-')
        if (hyphen > 0) {
            if (isAllDigits(normalized, hyphen)) {
                val suffix = normalized.substring(hyphen + 1)
                if (ORDINAL_SUFFIXES.contains(suffix)) return true
                if (computeIsUzbek(suffix)) return true
            } else {
                if (Dn.endsWithIgnoreCase(normalized, "-chi")) {
                    val stem = normalized.substring(0, normalized.length - 4)
                    if (stem.length >= 2 && computeIsUzbek(stem)) return true
                }
                val part1 = normalized.substring(0, hyphen)
                val part2 = normalized.substring(hyphen + 1)
                if (!isRejectWord(part1) && !isRejectWord(part2)) {
                    if (computeIsUzbek(part1) || computeIsUzbek(part2)) return true
                }
            }
        }

        // Tier 2
        if (inLexicon(normalized)) return true
        val converted = Phonetics.convert(normalized)
        if (converted != normalized && inLexicon(converted)) return true

        // Tier 3
        if (tryMorphologicalMatch(normalized)) return true
        if (converted != normalized && tryMorphologicalMatch(converted)) return true
        return false
    }

    private fun inLexicon(word: String): Boolean = uzbekLexicon.contains(toOldSpelling(word))

    private fun isRejectWord(word: String): Boolean = englishReject.contains(word) || (foreign?.contains(word) ?: false)

    private fun tryMorphologicalMatch(word: String): Boolean {
        for (suffix in UZBEK_SUFFIXES) {
            if (word.length > suffix.length + 1 && Dn.endsWithIgnoreCase(word, suffix)) {
                val stem = word.substring(0, word.length - suffix.length)
                if (checkStem(stem)) return true

                for (suffix2 in CHAINABLE_INNER_SUFFIXES) {
                    if (stem.length > suffix2.length + 1 && Dn.endsWithIgnoreCase(stem, suffix2)) {
                        val stem2 = stem.substring(0, stem.length - suffix2.length)
                        if (checkStem(stem2)) return true

                        for (suffix3 in CHAINABLE_INNER_SUFFIXES) {
                            if (stem2.length > suffix3.length + 1 && Dn.endsWithIgnoreCase(stem2, suffix3)) {
                                val stem3 = stem2.substring(0, stem2.length - suffix3.length)
                                if (checkStem(stem3)) return true
                            }
                        }
                    }
                }
            }
        }
        return false
    }

    /** stem or stem converted to new spelling is in the lexicon. */
    private fun lexOrConverted(s: String): Boolean {
        if (inLexicon(s)) return true
        val c = Phonetics.convert(s)
        return c != s && inLexicon(c)
    }

    /** Voice-derivation check shared by the n / il / t / tir / dir branches. */
    private fun baseStemMatch(baseStem: String): Boolean {
        if (inLexicon(baseStem) || inLexicon(baseStem + "moq")) return true
        val convBase = Phonetics.convert(baseStem)
        return inLexicon(convBase) || inLexicon(convBase + "moq")
    }

    private fun checkStem(stem: String): Boolean {
        if (stem.length < 2) return false

        // 1. Direct match
        if (lexOrConverted(stem)) return true

        // 2. Verb infinitive
        if (lexOrConverted(stem + "moq")) return true

        // 3. Consonant alternation g' -> q
        if (stem.endsWith('\'') && stem.length > 2) {
            if (Dn.endsWithIgnoreCase(stem, "g'")) {
                if (lexOrConverted(stem.substring(0, stem.length - 2) + "q")) return true
            }
        } else if ((stem.endsWith('ğ') || stem.endsWith('Ğ')) && stem.length > 2) {
            if (lexOrConverted(stem.substring(0, stem.length - 1) + "q")) return true
        }

        // 4. g -> k
        if (Dn.endsWithIgnoreCase(stem, "g") && stem.length > 2) {
            if (lexOrConverted(stem.substring(0, stem.length - 1) + "k")) return true
        }

        // 5. Voice derivations
        if (stem.endsWith('n') && stem.length > 2) {
            if (baseStemMatch(stem.substring(0, stem.length - 1))) return true
        }
        if (Dn.endsWithIgnoreCase(stem, "il") && stem.length > 3) {
            if (baseStemMatch(stem.substring(0, stem.length - 2))) return true
        }
        if (stem.endsWith('t') && stem.length > 2) {
            if (baseStemMatch(stem.substring(0, stem.length - 1))) return true
        }
        if (stem.length > 4 && (Dn.endsWithIgnoreCase(stem, "tir") || Dn.endsWithIgnoreCase(stem, "dir"))) {
            if (baseStemMatch(stem.substring(0, stem.length - 3))) return true
        }
        return false
    }

    companion object {
        /** Lower-cases and normalizes all apostrophe variants to U+0027. */
        fun normalizeForLookup(word: String): String = Dn.toLower(Phonetics.normalizeApostrophes(word))

        /** ş->sh, ç->ch, ö->o', ğ->g' (input is already lower-case). */
        fun toOldSpelling(word: String): String {
            var found = false
            for (c in word) if (c == 'ş' || c == 'ç' || c == 'ö' || c == 'ğ') { found = true; break }
            if (!found) return word
            val sb = StringBuilder(word.length + 4)
            for (c in word) {
                when (c) {
                    'ş' -> sb.append("sh")
                    'ç' -> sb.append("ch")
                    'ö' -> sb.append("o'")
                    'ğ' -> sb.append("g'")
                    else -> sb.append(c)
                }
            }
            return sb.toString()
        }

        /** Loads a word list ("# forms:" and '#' lines skipped, trimmed, one word per line, case-insensitive set). */
        fun loadWordList(name: String): FoldedSet {
            val text = CompactWordSet.readBrotliText(name)
            val set = HashSet<String>()
            var pos = 0
            val n = text.length
            while (pos <= n) {
                var end = pos
                while (end < n && text[end] != '\n' && text[end] != '\r') end++
                val trimmed = Dn.trim(text.substring(pos, end))
                if (trimmed.isNotEmpty() && trimmed[0] != '#') set.add(Dn.toUpper(trimmed))
                if (end >= n) break
                pos = if (text[end] == '\r' && end + 1 < n && text[end + 1] == '\n') end + 2 else end + 1
            }
            return FoldedSet(set)
        }
    }
}
