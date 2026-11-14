package uz.yangialifbo.engine

/**
 * .NET-compatible character helpers. The C# engine uses per-UTF-16-char invariant case mapping
 * (char.ToUpperInvariant, string.ToLowerInvariant, StringComparison.OrdinalIgnoreCase) and the
 * Unicode-category based char.IsWhiteSpace/IsDigit/IsLetter/IsUpper; java.lang.Character differs
 * in a few places, so everything goes through here.
 */
internal object Dn {
    // Characters where .NET invariant casing maps to itself but java.lang.Character does not
    // (dumped from char.ToUpperInvariant/ToLowerInvariant over all 65536 chars).
    fun upper(c: Char): Char = when {
        c < '\u0080' -> if (c in 'a'..'z') (c.code - 32).toChar() else c
        c == '\u0131' || c == '\u019B' || c == '\u0264' || c == '\u1C8A' || c == '\uA7CD' || c == '\uA7DB' -> c
        else -> Character.toUpperCase(c)
    }

    fun lower(c: Char): Char = when {
        c < '\u0080' -> if (c in 'A'..'Z') (c.code + 32).toChar() else c
        c == '\u0130' || c == '\u1C89' || c == '\uA7CB' || c == '\uA7CC' || c == '\uA7DA' || c == '\uA7DC' -> c
        else -> Character.toLowerCase(c)
    }

    fun toUpper(s: String): String {
        var i = 0
        while (i < s.length && upper(s[i]) == s[i]) i++
        if (i == s.length) return s
        val a = s.toCharArray()
        while (i < a.size) { a[i] = upper(a[i]); i++ }
        return String(a)
    }

    fun toLower(s: String): String {
        var i = 0
        while (i < s.length && lower(s[i]) == s[i]) i++
        if (i == s.length) return s
        val a = s.toCharArray()
        while (i < a.size) { a[i] = lower(a[i]); i++ }
        return String(a)
    }

    /** char.IsWhiteSpace */
    fun isWhiteSpace(c: Char): Boolean {
        if (c < '\u0100') return c == ' ' || c in '\t'..'\r' || c == '\u0085' || c == '\u00A0'
        return when (Character.getType(c)) {
            Character.SPACE_SEPARATOR.toInt(), Character.LINE_SEPARATOR.toInt(), Character.PARAGRAPH_SEPARATOR.toInt() -> true
            else -> false
        }
    }

    /** char.IsDigit (Unicode Nd) */
    fun isDigit(c: Char): Boolean = if (c < '\u0080') c in '0'..'9' else Character.getType(c) == Character.DECIMAL_DIGIT_NUMBER.toInt()

    /** char.IsLetter */
    fun isLetter(c: Char): Boolean = Character.isLetter(c)

    /** char.IsUpper (Unicode Lu only) */
    fun isUpper(c: Char): Boolean = if (c < '\u0080') c in 'A'..'Z' else Character.getType(c) == Character.UPPERCASE_LETTER.toInt()

    /** string.IsNullOrWhiteSpace */
    fun isNullOrWhiteSpace(s: String?): Boolean {
        if (s == null) return true
        for (c in s) if (!isWhiteSpace(c)) return false
        return true
    }

    /** string.Trim() */
    fun trim(s: String): String {
        var a = 0
        var b = s.length
        while (a < b && isWhiteSpace(s[a])) a++
        while (b > a && isWhiteSpace(s[b - 1])) b--
        return s.substring(a, b)
    }

    /** OrdinalIgnoreCase char equality */
    fun ceq(a: Char, b: Char): Boolean = a == b || upper(a) == upper(b)

    /** s.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) */
    fun endsWithIgnoreCase(s: String, suffix: String): Boolean {
        val off = s.length - suffix.length
        if (off < 0) return false
        for (i in suffix.indices) if (!ceq(s[off + i], suffix[i])) return false
        return true
    }
}

/** HashSet<string>(StringComparer.OrdinalIgnoreCase): keys are stored upper-cased. */
internal class FoldedSet(private val keys: HashSet<String>) {
    fun contains(s: String): Boolean = keys.contains(Dn.toUpper(s))
    val size: Int get() = keys.size
}

internal fun foldedSetOf(vararg items: String): FoldedSet {
    val h = HashSet<String>()
    for (i in items) h.add(Dn.toUpper(i))
    return FoldedSet(h)
}

internal fun foldedSetOf(items: Collection<String>): FoldedSet {
    val h = HashSet<String>()
    for (i in items) h.add(Dn.toUpper(i))
    return FoldedSet(h)
}
