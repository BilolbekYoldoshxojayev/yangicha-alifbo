package uz.yangialifbo.engine

import java.io.ByteArrayOutputStream
import java.io.InputStream

/**
 * Read-only word set over one string: words are stored back to back and an open-addressing
 * table of offsets finds them (port of CompactWordSet.cs). Lines starting with '#' are comments;
 * "# forms: N" is exposed as [coveredForms]. Lookup is case-insensitive over lower-case data.
 */
internal class CompactWordSet(private val text: String) {
    private val start: IntArray
    private val length: IntArray
    private val slots: IntArray   // word index + 1, 0 = empty
    private val mask: Int
    val count: Int
    var coveredForms: Int = 0
        private set

    init {
        var lines = 1
        for (ch in text) if (ch == '\n') lines++
        val starts = IntArray(lines)
        val lengths = IntArray(lines)
        var n = 0
        var pos = 0
        while (pos < text.length) {
            var end = text.indexOf('\n', pos)
            if (end < 0) end = text.length
            var len = end - pos
            if (len > 0 && text[pos + len - 1] == '\r') len--
            if (len > 0) {
                if (text[pos] == '#') {
                    if (text.startsWith("# forms:", pos) && len >= 8) {
                        val v = Dn.trim(text.substring(pos + 8, pos + len)).toIntOrNull()
                        if (v != null) coveredForms = v
                    }
                } else {
                    starts[n] = pos; lengths[n] = len; n++
                }
            }
            pos = end + 1
        }
        count = n
        start = if (n == starts.size) starts else starts.copyOf(n)
        length = if (n == lengths.size) lengths else lengths.copyOf(n)

        var size = 1
        while (size < n * 2) size = size shl 1
        slots = IntArray(size)
        mask = size - 1
        for (i in 0 until n) {
            var h = hash(text, start[i], length[i]) and mask
            while (slots[h] != 0) h = (h + 1) and mask
            slots[h] = i + 1
        }
    }

    fun contains(word: String): Boolean {
        var h = hash(word, 0, word.length) and mask
        while (true) {
            var idx = slots[h]
            if (idx == 0) return false
            idx--
            if (length[idx] == word.length && equalsLower(start[idx], word)) return true
            h = (h + 1) and mask
        }
    }

    private fun equalsLower(off: Int, word: String): Boolean {
        for (i in word.indices) if (text[off + i] != lower(word[i])) return false
        return true
    }

    companion object {
        /** Reads a Brotli-compressed UTF-8 word list from the classpath. */
        fun fromBrotliResource(name: String): CompactWordSet = CompactWordSet(readBrotliText(name))

        fun readBrotliBytes(name: String): ByteArray {
            val raw = CompactWordSet::class.java.getResourceAsStream("/$name")
                ?: throw java.io.FileNotFoundException("Resource not found: $name")
            raw.use { return decodeBrotli(it) }
        }

        fun readBrotliText(name: String): String {
            val bytes = readBrotliBytes(name)
            var off = 0
            // StreamReader(UTF8) drops a UTF-8 byte order mark.
            if (bytes.size >= 3 && bytes[0] == 0xEF.toByte() && bytes[1] == 0xBB.toByte() && bytes[2] == 0xBF.toByte()) off = 3
            return String(bytes, off, bytes.size - off, Charsets.UTF_8)
        }

        private fun decodeBrotli(input: InputStream): ByteArray {
            val br = org.brotli.dec.BrotliInputStream(input)
            val out = ByteArrayOutputStream(1 shl 24)
            val buf = ByteArray(1 shl 16)
            while (true) {
                val n = br.read(buf)
                if (n < 0) break
                out.write(buf, 0, n)
            }
            return out.toByteArray()
        }

        // FNV-1a over lower-cased chars.
        private fun hash(s: String, off: Int, len: Int): Int {
            var h = -2128831035 // 2166136261u
            for (i in off until off + len) { h = (h xor lower(s[i]).code) * 16777619 }
            return h xor (h ushr 15)
        }

        private fun lower(c: Char): Char = Dn.lower(c)
    }
}
