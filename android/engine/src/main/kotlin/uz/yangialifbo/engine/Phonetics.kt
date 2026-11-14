package uz.yangialifbo.engine

/** Port of UzbekPhonetics.cs: sh -> ş, ch -> ç, o' -> ö, g' -> ğ with case and s'h / c'h protection. */
internal object Phonetics {
    private fun isApostrophe(c: Char): Boolean = when (c) {
        '\'', '\u2018', '\u2019', '\u02BB', '\u02BC', '`', '\u00B4', '\u201B', '\u2032' -> true
        else -> false
    }

    fun normalizeApostrophes(text: String): String {
        var i = 0
        while (i < text.length && (text[i] == '\'' || !isApostrophe(text[i]))) i++
        if (i == text.length) return text
        val a = text.toCharArray()
        while (i < a.size) { if (isApostrophe(a[i])) a[i] = '\''; i++ }
        return String(a)
    }

    fun hasConvertiblePatterns(word: String): Boolean {
        if (word.length < 2) return false
        val n = normalizeApostrophes(word)
        var prot: BooleanArray? = null
        for (i in 0 until n.length - 2) {
            val lo = Dn.lower(n[i])
            val next = Dn.lower(n[i + 2])
            if ((lo == 's' || lo == 'c') && n[i + 1] == '\'' && next == 'h') {
                if (prot == null) prot = BooleanArray(n.length)
                prot[i] = true; prot[i + 1] = true; prot[i + 2] = true
            }
        }
        for (i in 0 until n.length - 1) {
            if (prot != null && (prot[i] || prot[i + 1])) continue
            val lo = Dn.lower(n[i])
            val next = n[i + 1]
            val loNext = Dn.lower(next)
            if ((lo == 's' || lo == 'c') && loNext == 'h') return true
            if ((lo == 'o' || lo == 'g') && next == '\'') return true
        }
        return false
    }

    fun convert(word: String): String {
        if (word.isEmpty()) return word
        val n = normalizeApostrophes(word)
        val len = n.length
        var prot: BooleanArray? = null
        for (i in 0 until len - 2) {
            val lo = Dn.lower(n[i])
            val next = Dn.lower(n[i + 2])
            if ((lo == 's' || lo == 'c') && n[i + 1] == '\'' && next == 'h') {
                if (prot == null) prot = BooleanArray(len)
                prot[i] = true; prot[i + 1] = true; prot[i + 2] = true
            }
        }
        val sb = StringBuilder(len)
        var idx = 0
        while (idx < len) {
            if (prot != null && prot[idx]) { sb.append(n[idx]); idx++; continue }
            if (idx + 1 < len && (prot == null || !prot[idx + 1])) {
                val lo = Dn.lower(n[idx])
                val nextChar = n[idx + 1]
                val loNext = Dn.lower(nextChar)
                val up = Dn.isUpper(n[idx])
                var rep = '\u0000'
                if (lo == 's' && loNext == 'h') rep = if (up) 'Ş' else 'ş'
                else if (lo == 'c' && loNext == 'h') rep = if (up) 'Ç' else 'ç'
                else if (lo == 'o' && nextChar == '\'') rep = if (up) 'Ö' else 'ö'
                else if (lo == 'g' && nextChar == '\'') rep = if (up) 'Ğ' else 'ğ'
                if (rep != '\u0000') { sb.append(rep); idx += 2; continue }
            }
            sb.append(n[idx]); idx++
        }
        return sb.toString()
    }
}
