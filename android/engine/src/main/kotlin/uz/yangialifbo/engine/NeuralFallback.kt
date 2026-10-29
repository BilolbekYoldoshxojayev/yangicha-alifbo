package uz.yangialifbo.engine

import java.nio.ByteBuffer
import java.nio.ByteOrder

/**
 * Port of NeuralFallback.cs: an ensemble of small character CNNs that recognises Uzbek words the
 * lexicon and morphology do not know. Float arithmetic keeps the exact operation order of the C#
 * code (4-lane dot product, no fused multiply-add) so scores are bit-identical.
 */
class NeuralFallback private constructor(data: ByteArray) {
    private val maxLen: Int
    private val emb: Int
    private val ch: Int
    private val hid: Int
    private val kernels: IntArray
    private val charId = IntArray(128) { -1 }
    private val trigrams: HashSet<String>
    private val nets: Array<Net>

    /** Scores above this are accepted (highest foreign score seen in validation). */
    val threshold: Double

    val models: Int get() = nets.size

    private class Net(
        val emb: FloatArray, val convW: Array<FloatArray>, val convB: Array<FloatArray>,
        val fc1W: FloatArray, val fc1B: FloatArray, val fc2W: FloatArray, val fc2B: Float,
    )

    private class Reader(private val d: ByteArray) {
        private val bb: ByteBuffer = ByteBuffer.wrap(d).order(ByteOrder.LITTLE_ENDIAN)
        private var p = 0
        val atEnd: Boolean get() = p == d.size
        fun int(): Int { val v = bb.getInt(p); p += 4; return v }
        fun double(): Double { val v = bb.getDouble(p); p += 8; return v }
        fun ascii(n: Int): String { val s = String(d, p, n, Charsets.ISO_8859_1); p += n; return s }
        fun cString(): String {
            var e = p
            while (d[e] != 0.toByte()) e++
            val s = String(d, p, e - p, Charsets.ISO_8859_1)
            p = e + 1
            return s
        }
        fun floats(n: Int): FloatArray {
            val a = FloatArray(n)
            for (i in 0 until n) a[i] = bb.getFloat(p + 4 * i)
            p += 4 * n
            return a
        }

        /** A weight matrix stored as one float scale per row, then int8 values. */
        fun int8Rows(rows: Int, cols: Int): FloatArray {
            val scale = floats(rows)
            val a = FloatArray(rows * cols)
            for (r in 0 until rows) {
                val s = scale[r]
                val base = p + r * cols
                for (c in 0 until cols) a[r * cols + c] = d[base + c].toFloat() * s
            }
            p += rows * cols
            return a
        }
    }

    init {
        val r = Reader(data)
        if (r.int() != 0x4E4E4159) throw IllegalStateException("not a YangiAlifbo model")
        if (r.int() != 4) throw IllegalStateException("unknown model version")
        val vocab = r.int(); maxLen = r.int(); emb = r.int(); ch = r.int()
        val nk = r.int(); hid = r.int(); val nModels = r.int()
        kernels = IntArray(nk) { r.int() }
        threshold = r.double()
        val alphabet = r.cString()
        for (i in alphabet.indices) charId[alphabet[i].code] = i + 3
        val tri = r.ascii(r.int())
        trigrams = HashSet(tri.split('\n'))

        nets = Array(nModels) {
            val e = r.int8Rows(vocab, emb)
            val convW = arrayOfNulls<FloatArray>(nk)
            val convB = arrayOfNulls<FloatArray>(nk)
            for (i in 0 until nk) {
                convW[i] = windowMajor(r.int8Rows(ch, emb * kernels[i]), kernels[i])
                convB[i] = r.floats(ch)
            }
            val fc1W = r.int8Rows(hid, ch * nk)
            val fc1B = r.floats(hid)
            val fc2W = r.floats(hid)
            @Suppress("UNCHECKED_CAST")
            Net(e, convW as Array<FloatArray>, convB as Array<FloatArray>, fc1W, fc1B, fc2W, r.floats(1)[0])
        }
        if (!r.atEnd) throw IllegalStateException("model size mismatch")
    }

    /** PyTorch stores a filter as [channel][dim][offset]; reorder to [channel][offset][dim]. */
    private fun windowMajor(w: FloatArray, ks: Int): FloatArray {
        val o = FloatArray(w.size)
        for (c in 0 until ch)
            for (d in 0 until emb)
                for (j in 0 until ks)
                    o[(c * ks + j) * emb + d] = w[(c * emb + d) * ks + j]
        return o
    }

    /** True when [normalized] (lower case, ASCII apostrophes) is eligible and scores above the threshold. */
    fun accepts(normalized: String): Boolean {
        if (!isEligible(normalized) || !hasUzbekTrigrams(normalized)) return false
        val ids = IntArray(maxLen)
        val len = encode(normalized, ids)
        if (len < 0) return false
        for (net in nets) if (forward(net, ids, len) <= threshold) return false   // least confident model decides
        return true
    }

    /** True when every letter trigram of the word (with ^/$ edges) occurs in Uzbek training words. */
    fun hasUzbekTrigrams(word: String): Boolean {
        val p = "^$word$"
        var i = 0
        while (i + 3 <= p.length) {
            if (!trigrams.contains(p.substring(i, i + 3))) return false
            i++
        }
        return true
    }

    /** 0 when the word fails the trigram check, otherwise the lowest probability any model gives. */
    fun score(word: String): Double {
        if (!hasUzbekTrigrams(word)) return 0.0
        val ids = IntArray(maxLen)
        val len = encode(word, ids)
        if (len < 0) return 0.0
        var min = 1.0
        for (net in nets) min = Math.min(min, forward(net, ids, len))
        return min
    }

    /** BOS, letters, EOS, then padding; returns the used length or -1 for a letter outside the alphabet. */
    private fun encode(word: String, ids: IntArray): Int {
        val len = Math.min(word.length, maxLen - 2) + 2
        ids.fill(0)
        ids[0] = 1
        for (i in 0 until len - 2) {
            val c = word[i]
            val id = if (c.code < 128) charId[c.code] else -1
            if (id < 0) return -1
            ids[i + 1] = id
        }
        ids[len - 1] = 2
        return len
    }

    private fun forward(net: Net, ids: IntArray, len: Int): Double {
        val x = FloatArray(maxLen * emb)
        for (p in 0 until len) System.arraycopy(net.emb, ids[p] * emb, x, p * emb, emb)

        val pooled = FloatArray(ch * kernels.size)
        for (k in kernels.indices) {
            val ks = kernels[k]
            val outLen = maxLen - ks + 1
            val span = ks * emb
            val live = Math.min(outLen, len)
            val padOnly = outLen > len
            val w = net.convW[k]
            val b = net.convB[k]
            for (o in 0 until ch) {
                var best = if (padOnly) b[o] else Float.NEGATIVE_INFINITY
                for (t in 0 until live) {
                    val sum = b[o] + dot(w, o * span, x, t * emb, span)
                    if (sum > best) best = sum
                }
                pooled[k * ch + o] = Math.max(best, 0f)   // max-pool of ReLU = ReLU of max
            }
        }

        var logit = net.fc2B.toDouble()
        val n = pooled.size
        for (h in 0 until hid) {
            val sum = net.fc1B[h] + dot(net.fc1W, h * n, pooled, 0, n)
            if (sum > 0) logit += (net.fc2W[h] * sum).toDouble()
        }
        return 1.0 / (1.0 + Math.exp(-logit))
    }

    // Fixed 4-lane accumulation: the same summation order as the C# Vector128 code.
    private fun dot(a: FloatArray, ao: Int, b: FloatArray, bo: Int, n: Int): Float {
        var i = 0
        var a0 = 0f; var a1 = 0f; var a2 = 0f; var a3 = 0f
        while (i <= n - 4) {
            a0 += a[ao + i] * b[bo + i]
            a1 += a[ao + i + 1] * b[bo + i + 1]
            a2 += a[ao + i + 2] * b[bo + i + 2]
            a3 += a[ao + i + 3] * b[bo + i + 3]
            i += 4
        }
        var sum = (a0 + a1) + (a2 + a3)
        while (i < n) { sum += a[ao + i] * b[bo + i]; i++ }
        return sum
    }

    companion object {
        private const val RESOURCE_NAME = "uzbek_neural.bin.br"

        /** Loads a model file written by train.py (uncompressed). */
        fun fromBytes(data: ByteArray): NeuralFallback = NeuralFallback(data)

        /** The model from the classpath resource; throws when it is missing or unreadable. */
        fun loadEmbedded(): NeuralFallback = NeuralFallback(CompactWordSet.readBrotliBytes(RESOURCE_NAME))

        /**
         * Words the model may be asked about: 4-32 letters of the Uzbek Latin alphabet
         * (no w, no c outside ch), an optional hyphen, and a sh/ch/o'/g' to convert.
         */
        fun isEligible(normalized: String): Boolean {
            val len = normalized.length
            if (len < 4 || len > 32 || !eligibleShape(normalized)) return false
            if (normalized.indexOf('w') >= 0) return false
            for (i in 0 until len)
                if (normalized[i] == 'c' && (i + 1 == len || normalized[i + 1] != 'h')) return false
            if (normalized[0] == '\'' || normalized.contains("''")) return false
            return Phonetics.hasConvertiblePatterns(normalized)
        }

        /** Regex ^[a-z']+(-[a-z']+)?$ ; like .NET, '$' also matches before one final '\n'. */
        private fun eligibleShape(s: String): Boolean {
            var end = s.length
            if (end > 0 && s[end - 1] == '\n') end--
            var i = 0
            var run = 0
            while (i < end && (s[i] in 'a'..'z' || s[i] == '\'')) { i++; run++ }
            if (run == 0) return false
            if (i == end) return true
            if (s[i] != '-') return false
            i++
            run = 0
            while (i < end && (s[i] in 'a'..'z' || s[i] == '\'')) { i++; run++ }
            return run > 0 && i == end
        }
    }
}
