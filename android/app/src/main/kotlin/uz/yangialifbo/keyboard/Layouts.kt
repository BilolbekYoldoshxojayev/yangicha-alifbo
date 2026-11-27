package uz.yangialifbo.keyboard

enum class Kind { CHAR, SHIFT, DELETE, SYMBOLS, SPACE, ENTER, GLOBE, CONVERT, SPACER }

/**
 * One key. [alt] is typed by a long press (the new-alphabet letters on s, c, o, g and the
 * tutuq sign on the apostrophe), so a letter can be typed on purpose when the word buffer
 * would not produce it.
 */
class Key(val kind: Kind, val label: String = "", val weight: Float = 1f, val alt: String? = null)

class Row(val keys: List<Key>, val height: Float = 1f)

object Layouts {
    private fun chars(s: String, alts: Map<Char, String> = emptyMap()) =
        s.map { Key(Kind.CHAR, it.toString(), alt = alts[it]) }

    private fun digits() = Row(chars("1234567890"), height = 0.8f)

    private val letterAlts = mapOf('s' to "ş", 'c' to "ç", 'o' to "ö", 'g' to "ğ")

    /** Tutuq belgisi, the sign that keeps s'h, a'zo apart from digraphs. */
    const val TUTUQ = "ʼ"

    fun letters(showGlobe: Boolean): List<Row> = listOf(
        digits(),
        Row(chars("qwertyuiop", letterAlts)),
        Row(listOf(Key(Kind.SPACER, weight = 0.5f)) + chars("asdfghjkl", letterAlts) + Key(Kind.SPACER, weight = 0.5f)),
        Row(listOf(Key(Kind.SHIFT, "⇧", 1.5f)) + chars("zxcvbnm", letterAlts) + Key(Kind.DELETE, "⌫", 1.5f)),
        bottom(showGlobe, symbolsLabel = "?123"),
    )

    fun symbols(showGlobe: Boolean): List<Row> = listOf(
        digits(),
        Row(chars("@#\$_&-+()/")),
        Row(listOf(Key(Kind.SPACER, weight = 0.5f)) + chars("*\"':;!?%=") + Key(Kind.SPACER, weight = 0.5f)),
        Row(chars("~|<>\\^`").let { listOf(Key(Kind.SPACER, weight = 1.5f)) + it + Key(Kind.DELETE, "⌫", 1.5f) }),
        bottom(showGlobe, symbolsLabel = "ABC"),
    )

    private fun bottom(showGlobe: Boolean, symbolsLabel: String) = Row(
        listOf(
            Key(Kind.SYMBOLS, symbolsLabel, 1.4f),
            if (showGlobe) Key(Kind.GLOBE, "\uD83C\uDF10") else Key(Kind.CHAR, ",", alt = null),
            Key(Kind.CHAR, "'", alt = TUTUQ),
            Key(Kind.SPACE, "", 3.6f),
            Key(Kind.CHAR, "."),
            Key(Kind.CONVERT, "ş"),
            Key(Kind.ENTER, "⏎", 1.4f),
        ),
    )
}
