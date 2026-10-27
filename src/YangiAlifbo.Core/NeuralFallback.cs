// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — NeuralFallback.cs
// Tier 4 of Strict mode: an ensemble of small character CNNs
// (tools/neural/train.py) that recognises Uzbek words the lexicon and
// morphology do not know. It is asked only after every other tier said
// "unknown", only about words written with Uzbek Latin letters whose
// letter trigrams all occur in Uzbek words, and it accepts only when its
// least confident model scores above a threshold no foreign word reached
// in validation. It never rejects a word the other tiers accepted.
// ──────────────────────────────────────────────────────────────────────

using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.Intrinsics;
using System.Text.RegularExpressions;

namespace YangiAlifbo.Core;

public sealed partial class NeuralFallback
{
    private const string ResourceName = "uzbek_neural.bin.br";

    private readonly int _maxLen, _emb, _ch, _hid;
    private readonly int[] _kernels;
    private readonly int[] _charId = new int[128];
    private readonly HashSet<string> _trigrams;
    private readonly Net[] _nets;

    /// <summary>Scores above this are accepted (highest foreign score seen in validation).</summary>
    public double Threshold { get; }

    /// <summary>Number of models in the ensemble.</summary>
    public int Models => _nets.Length;

    private sealed record Net(float[] Emb, float[][] ConvW, float[][] ConvB, float[] Fc1W, float[] Fc1B, float[] Fc2W, float Fc2B);

    private NeuralFallback(byte[] data)
    {
        var r = new Reader(data);
        if (r.Int() != 0x4E4E4159) throw new InvalidDataException("not a YangiAlifbo model");   // "YANN"
        if (r.Int() != 4) throw new InvalidDataException("unknown model version");
        int vocab = r.Int(); _maxLen = r.Int(); _emb = r.Int(); _ch = r.Int();
        int nk = r.Int(); _hid = r.Int(); int models = r.Int();
        _kernels = new int[nk];
        for (int i = 0; i < nk; i++) _kernels[i] = r.Int();
        Threshold = r.Double();
        string alphabet = r.CString();
        Array.Fill(_charId, -1);
        for (int i = 0; i < alphabet.Length; i++) _charId[alphabet[i]] = i + 3;
        _trigrams = new HashSet<string>(r.Ascii(r.Int()).Split('\n'), StringComparer.Ordinal);

        _nets = new Net[models];
        for (int m = 0; m < models; m++)
        {
            float[] emb = r.Int8Rows(vocab, _emb);
            var convW = new float[nk][]; var convB = new float[nk][];
            for (int i = 0; i < nk; i++) { convW[i] = WindowMajor(r.Int8Rows(_ch, _emb * _kernels[i]), _kernels[i]); convB[i] = r.Floats(_ch); }
            float[] fc1W = r.Int8Rows(_hid, _ch * nk), fc1B = r.Floats(_hid), fc2W = r.Floats(_hid);
            _nets[m] = new Net(emb, convW, convB, fc1W, fc1B, fc2W, r.Floats(1)[0]);
        }
        if (!r.AtEnd) throw new InvalidDataException("model size mismatch");
    }

    /// <summary>
    /// PyTorch stores a filter as [channel][dim][offset]; reorder it to [channel][offset][dim]
    /// so it lines up with ks consecutive embedding rows, which are contiguous in memory.
    /// </summary>
    private float[] WindowMajor(float[] w, int ks)
    {
        var o = new float[w.Length];
        for (int c = 0; c < _ch; c++)
            for (int d = 0; d < _emb; d++)
                for (int j = 0; j < ks; j++)
                    o[(c * ks + j) * _emb + d] = w[(c * _emb + d) * ks + j];
        return o;
    }

    /// <summary>Loads a model file written by train.py (uncompressed).</summary>
    public static NeuralFallback FromBytes(byte[] data) => new(data);

    /// <summary>The model embedded in the exe, or null when it is missing or unreadable.</summary>
    public static NeuralFallback? LoadEmbedded()
    {
        try
        {
            using var s = typeof(NeuralFallback).Assembly.GetManifestResourceStream(ResourceName);
            if (s == null) return null;
            using var br = new BrotliStream(s, CompressionMode.Decompress);
            using var ms = new MemoryStream();
            br.CopyTo(ms);
            return new NeuralFallback(ms.ToArray());
        }
        catch { return null; }
    }

    [GeneratedRegex("^[a-z']+(-[a-z']+)?$")]
    private static partial Regex EligibleShape();

    /// <summary>
    /// Words the model may be asked about: 4–32 letters of the Uzbek Latin alphabet
    /// (no w, no c outside ch), an optional hyphen, and a sh/ch/o'/g' to convert.
    /// Same rule as eligible() in tools/neural/build_data.py.
    /// </summary>
    public static bool IsEligible(string normalized)
    {
        if (normalized.Length is < 4 or > 32 || !EligibleShape().IsMatch(normalized)) return false;
        if (normalized.Contains('w')) return false;
        for (int i = 0; i < normalized.Length; i++)
            if (normalized[i] == 'c' && (i + 1 == normalized.Length || normalized[i + 1] != 'h')) return false;
        if (normalized[0] == '\'' || normalized.Contains("''")) return false;
        return UzbekPhonetics.HasConvertiblePatterns(normalized);
    }

    /// <summary>True when <paramref name="normalized"/> (lower case, ASCII apostrophes) is eligible and scores above the threshold.</summary>
    public bool Accepts(string normalized)
    {
        if (!IsEligible(normalized) || !HasUzbekTrigrams(normalized)) return false;
        Span<int> ids = stackalloc int[_maxLen];
        int len = Encode(normalized, ids);
        if (len < 0) return false;
        foreach (var net in _nets)
            if (Forward(net, ids, len) <= Threshold) return false;   // the least confident model decides
        return true;
    }

    /// <summary>True when every letter trigram of the word (with ^/$ edges) occurs in Uzbek training words.</summary>
    public bool HasUzbekTrigrams(string word)
    {
        string p = "^" + word + "$";
        for (int i = 0; i + 3 <= p.Length; i++)
            if (!_trigrams.Contains(p.Substring(i, 3))) return false;
        return true;
    }

    /// <summary>
    /// Ensemble score: 0 when the word fails the trigram check, otherwise the lowest
    /// probability any model gives that the word is Uzbek.
    /// </summary>
    public double Score(string word)
    {
        if (!HasUzbekTrigrams(word)) return 0;
        Span<int> ids = stackalloc int[_maxLen];
        int len = Encode(word, ids);
        if (len < 0) return 0;
        double min = 1;
        foreach (var net in _nets)
            min = Math.Min(min, Forward(net, ids, len));
        return min;
    }

    /// <summary>BOS, letters, EOS, then padding; returns the used length or -1 for a letter outside the alphabet.</summary>
    private int Encode(string word, Span<int> ids)
    {
        int len = Math.Min(word.Length, _maxLen - 2) + 2;
        ids.Clear();
        ids[0] = 1;
        for (int i = 0; i < len - 2; i++)
        {
            char c = word[i];
            int id = c < 128 ? _charId[c] : -1;
            if (id < 0) return -1;
            ids[i + 1] = id;
        }
        ids[len - 1] = 2;
        return len;
    }

    private double Forward(Net net, ReadOnlySpan<int> ids, int len)
    {
        // Embeddings, laid out [position][dim]; padding rows stay zero.
        Span<float> x = stackalloc float[_maxLen * _emb];
        x.Clear();
        for (int p = 0; p < len; p++)
            net.Emb.AsSpan(ids[p] * _emb, _emb).CopyTo(x.Slice(p * _emb));

        Span<float> pooled = stackalloc float[_ch * _kernels.Length];
        for (int k = 0; k < _kernels.Length; k++)
        {
            int ks = _kernels[k], outLen = _maxLen - ks + 1, span = ks * _emb;
            // Windows starting at or after `len` see only padding and output just the bias.
            int live = Math.Min(outLen, len);
            bool padOnly = outLen > len;
            float[] w = net.ConvW[k], b = net.ConvB[k];
            for (int o = 0; o < _ch; o++)
            {
                ReadOnlySpan<float> filter = w.AsSpan(o * span, span);
                float best = padOnly ? b[o] : float.NegativeInfinity;
                for (int t = 0; t < live; t++)
                {
                    float sum = b[o] + Dot(filter, x.Slice(t * _emb, span));
                    if (sum > best) best = sum;
                }
                pooled[k * _ch + o] = Math.Max(best, 0f);   // max-pool of ReLU = ReLU of max
            }
        }

        double logit = net.Fc2B;
        int n = pooled.Length;
        for (int h = 0; h < _hid; h++)
        {
            float sum = net.Fc1B[h] + Dot(net.Fc1W.AsSpan(h * n, n), pooled);
            if (sum > 0) logit += net.Fc2W[h] * sum;
        }
        return 1.0 / (1.0 + Math.Exp(-logit));
    }

    // Fixed 4-lane accumulation: the same summation order on every CPU (x64 and ARM64),
    // so Windows and Linux give bit-identical scores.
    private static float Dot(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        int i = 0;
        var acc = Vector128<float>.Zero;
        for (; i <= a.Length - 4; i += 4)
            acc += Vector128.Create(a.Slice(i, 4)) * Vector128.Create(b.Slice(i, 4));
        float sum = (acc.GetElement(0) + acc.GetElement(1)) + (acc.GetElement(2) + acc.GetElement(3));
        for (; i < a.Length; i++) sum += a[i] * b[i];
        return sum;
    }

    private ref struct Reader(byte[] data)
    {
        private readonly ReadOnlySpan<byte> _d = data;
        private int _p;
        public readonly bool AtEnd => _p == _d.Length;
        public int Int() { int v = BinaryPrimitives.ReadInt32LittleEndian(_d[_p..]); _p += 4; return v; }
        public double Double() { double v = BinaryPrimitives.ReadDoubleLittleEndian(_d[_p..]); _p += 8; return v; }
        public string Ascii(int n) { string s = System.Text.Encoding.ASCII.GetString(_d.Slice(_p, n)); _p += n; return s; }
        public string CString() { int e = _d[_p..].IndexOf((byte)0); string s = System.Text.Encoding.ASCII.GetString(_d.Slice(_p, e)); _p += e + 1; return s; }
        public float[] Floats(int n)
        {
            var a = new float[n];
            for (int i = 0; i < n; i++) a[i] = BinaryPrimitives.ReadSingleLittleEndian(_d[(_p + 4 * i)..]);
            _p += 4 * n;
            return a;
        }

        /// <summary>Reads a weight matrix stored as one float scale per row, then int8 values.</summary>
        public float[] Int8Rows(int rows, int cols)
        {
            float[] scale = Floats(rows);
            var a = new float[rows * cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    a[r * cols + c] = (sbyte)_d[_p + r * cols + c] * scale[r];
            _p += rows * cols;
            return a;
        }
    }
}
