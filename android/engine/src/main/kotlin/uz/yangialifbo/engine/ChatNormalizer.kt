package uz.yangialifbo.engine

/** Port of ChatNormalizer.cs (informal chat spellings -> standard orthography). */
internal object ChatNormalizer {
    fun normalize(word: String): String {
        if (Dn.isNullOrWhiteSpace(word) || word.length < 2) return word

        val lower = Dn.toLower(word)
        var norm = lower

        // 0. Hyphenated numbers (5-ci -> 5-chi, 1-orin -> 1-o'rin)
        val numHyphen = norm.indexOf('-')
        if (numHyphen > 0) {
            var allDigits = true
            for (i in 0 until numHyphen) {
                if (!Dn.isDigit(norm[i])) { allDigits = false; break }
            }
            if (allDigits) {
                val prefix = norm.substring(0, numHyphen + 1)
                val suffix = norm.substring(numHyphen + 1)
                if (suffix == "ci" || suffix == "c") {
                    norm = prefix + "chi"
                } else {
                    val normSuffix = normalize(suffix)
                    if (normSuffix != suffix) norm = prefix + normSuffix
                }
            }
        }

        // 1. Russian keyboard 'w' substitutions
        if (norm == "wu") {
            norm = "shu"
        } else if (norm.startsWith("wunaqa") || norm.startsWith("wunaqangi") ||
            norm.startsWith("wuni") || norm.startsWith("wunga") ||
            norm.startsWith("wunday") || norm.startsWith("wunde") ||
            norm.startsWith("wuncha") || norm.startsWith("wunda") ||
            norm.startsWith("wuning") || norm.startsWith("wunisi")
        ) {
            norm = "sh" + norm.substring(1)
        } else if (norm.startsWith("owa") || norm.startsWith("uwa")) {
            norm = "o'sha" + norm.substring(3)
        } else if (norm == "awqam") {
            norm = "oqshom"
        } else {
            if (norm.contains("yaxw")) norm = norm.replace("yaxw", "yaxsh")
            if (norm.contains("tuw")) norm = norm.replace("tuw", "tush")
            if (norm.contains("bowq")) norm = norm.replace("bowq", "boshq")
            if (norm.contains("bowlan")) norm = norm.replace("bowlan", "boshlan")
            if (norm.contains("yow")) norm = norm.replace("yow", "yosh")
            if (norm.endsWith("iwi") && norm.length >= 5) norm = norm.substring(0, norm.length - 3) + "ishi"
            if (norm == "iwim" || norm == "iwing" || norm == "iwi") norm = norm.replace("w", "sh")
            if (norm == "quwiladi") norm = "qo'shiladi"
            if (norm.contains("tawavor")) norm = norm.replace("tawavor", "tashavor")
        }

        // 2. 'c' for 'ch'
        if (norm == "ucun") norm = "uchun"
        else if (norm == "qacon") norm = "qachon"
        else if (norm == "hec") norm = "hech"
        else if (norm == "qanca") norm = "qancha"
        else if (norm == "necu") norm = "nechun"

        // 3. Dialectal & spoken stems
        if (norm.startsWith("osha")) {
            norm = "o'sha" + norm.substring(4)
        } else if (norm.startsWith("bopti")) {
            norm = "bo'pti" + norm.substring(5)
        } else if (norm == "bomasam" || norm == "bomasa" || norm == "bomasin") {
            norm = "bo'lmasam"
        } else if (norm == "bosa") {
            norm = "bo'lsa"
        } else if (norm.startsWith("bovotti")) {
            norm = "bo'votti" + norm.substring(7)
        } else if (norm.startsWith("bovot")) {
            norm = "bo'votti" + norm.substring(5)
        } else if (norm == "lekn") {
            norm = "lekin"
        } else if (norm == "blan" || norm == "bln") {
            norm = "bilan"
        } else if (norm == "nma") {
            norm = "nima"
        } else if (norm == "nmaga") {
            norm = "nimaga"
        } else if (norm == "nmadur" || norm == "nmadir") {
            norm = "nimadir"
        } else if (norm == "qalesz") {
            norm = "qaleysiz"
        } else if (norm.startsWith("qattansz")) {
            norm = "qattansiz"
        } else if (norm == "yaxwmsz") {
            norm = "yaxshimisiz"
        } else if (norm == "qanaqangisi") {
            norm = "qanaqangisi"
        }

        // 4. Suffix contractions
        if (norm.endsWith("amz") && norm.length >= 5) {
            norm = norm.substring(0, norm.length - 3) + "amiz"
        } else if (norm.endsWith("asz") && norm.length >= 5) {
            norm = norm.substring(0, norm.length - 3) + "asiz"
        } else if (norm.endsWith("misz") && norm.length >= 6) {
            norm = norm.substring(0, norm.length - 4) + "misiz"
        } else if (norm.endsWith("dm") && norm.length >= 3 && norm != "adm") {
            norm = norm.substring(0, norm.length - 2) + "dim"
        } else if (norm.endsWith("dk") && norm.length >= 4 && norm != "idk") {
            norm = norm.substring(0, norm.length - 2) + "dik"
        }

        // 5. Unaccented 'o' expansion
        norm = expandUnaccentedRootsFast(norm)

        if (norm == lower) return word
        return matchCasing(word, norm)
    }

    private fun expandUnaccentedRootsFast(norm: String): String {
        if (norm.startsWith("bol") && CHAT_BOL_SUFFIXES.contains(norm.substring(3))) return "bo'l" + norm.substring(3)
        if (norm.startsWith("kor") && CHAT_KOR_SUFFIXES.contains(norm.substring(3))) return "ko'r" + norm.substring(3)
        if (norm.startsWith("oz") && CHAT_OZ_SUFFIXES.contains(norm.substring(2))) return "o'z" + norm.substring(2)

        if (norm == "togri" || norm.startsWith("togri")) return "to'g'ri" + norm.substring(5)
        if (norm == "zor" || norm.startsWith("zor")) return "zo'r" + norm.substring(3)
        if (norm == "hop" || norm == "xop") return "xo'p"
        if (norm == "yoq" || norm == "yoqmi" || norm == "yoqligi" || norm == "yoqku") return "yo'q" + norm.substring(3)

        if (norm.startsWith("kop") && CHAT_KOP_SUFFIXES.contains(norm.substring(3))) return "ko'p" + norm.substring(3)
        if (norm.startsWith("soz") && CHAT_SOZ_SUFFIXES.contains(norm.substring(3))) return "so'z" + norm.substring(3)
        if (norm.startsWith("sora") && CHAT_SORA_SUFFIXES.contains(norm.substring(4))) return "so'ra" + norm.substring(4)
        if (norm.startsWith("yol") && CHAT_YOL_SUFFIXES.contains(norm.substring(3))) return "yo'l" + norm.substring(3)
        if (norm.startsWith("ort") && CHAT_ORT_SUFFIXES.contains(norm.substring(3))) return "o'rt" + norm.substring(3)
        if (norm.startsWith("ochir") && CHAT_OCHIR_SUFFIXES.contains(norm.substring(5))) return "o'chir" + norm.substring(5)
        if (norm.startsWith("oyla") && CHAT_OYLA_SUFFIXES.contains(norm.substring(4))) return "o'yla" + norm.substring(4)
        if (norm.startsWith("jonat") && CHAT_JONAT_SUFFIXES.contains(norm.substring(5))) return "jo'nat" + norm.substring(5)
        if (norm.startsWith("qosh") && CHAT_QOSH_SUFFIXES.contains(norm.substring(4))) return "qo'sh" + norm.substring(4)
        if (norm.startsWith("orin") && CHAT_ORIN_SUFFIXES.contains(norm.substring(4))) return "o'rin" + norm.substring(4)

        if (norm.startsWith("orgat")) return "o'rgat" + norm.substring(5)
        if (norm.startsWith("organ")) return "o'rgan" + norm.substring(5)

        if (norm.startsWith("tol") && CHAT_TOL_SUFFIXES.contains(norm.substring(3))) return "to'l" + norm.substring(3)
        if (norm.startsWith("ot") && CHAT_OT_SUFFIXES.contains(norm.substring(2))) return "o't" + norm.substring(2)

        return norm
    }

    fun matchCasing(original: String, transformed: String): String {
        if (transformed.isEmpty()) return transformed

        var allUpper = true
        for (c in original) {
            if (Dn.isLetter(c) && !Dn.isUpper(c)) { allUpper = false; break }
        }
        if (allUpper) return Dn.toUpper(transformed)
        if (Dn.isUpper(original[0])) return Dn.upper(transformed[0]).toString() + transformed.substring(1)
        return Dn.toLower(transformed)
    }
}
