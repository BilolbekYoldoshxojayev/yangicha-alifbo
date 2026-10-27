// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — CompactWordSet.cs
// Read-only word set over one string: words are stored back to back and an
// open-addressing table of offsets finds them. ~10× less memory than a
// HashSet<string> of the same words and no per-word allocations at load.
// ──────────────────────────────────────────────────────────────────────

using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;

namespace YangiAlifbo.Core;

/// <summary>Membership test used by <see cref="LanguageDetector"/>.</summary>
public interface IWordSet
{
    bool Contains(string word);
    int Count { get; }
}

/// <summary>Adapter for word lists built in code (tests, user lists).</summary>
public sealed class HashWordSet : IWordSet
{
    private readonly HashSet<string> _set;
    public HashWordSet(HashSet<string> set) => _set = set;
    public bool Contains(string word) => _set.Contains(word);
    public int Count => _set.Count;
}

/// <summary>
/// Case-insensitive (lower-case data) word set. Lines starting with '#' are comments;
/// "# forms: N" is exposed as <see cref="CoveredForms"/>.
/// </summary>
public sealed class CompactWordSet : IWordSet
{
    private readonly string _text;
    private readonly int[] _start;
    private readonly int[] _length;
    private readonly int[] _slots;   // word index + 1, 0 = empty
    private readonly int _mask;

    public int Count => _start.Length;
    public int CoveredForms { get; }

    public CompactWordSet(string text)
    {
        _text = text;
        var starts = new List<int>(1 << 20);
        var lengths = new List<int>(1 << 20);
        int pos = 0;
        while (pos < text.Length)
        {
            int end = text.IndexOf('\n', pos);
            if (end < 0) end = text.Length;
            int len = end - pos;
            if (len > 0 && text[pos + len - 1] == '\r') len--;
            if (len > 0)
            {
                if (text[pos] == '#')
                {
                    var line = text.AsSpan(pos, len);
                    if (line.StartsWith("# forms:") && int.TryParse(line[8..].Trim(), out int n)) CoveredForms = n;
                }
                else { starts.Add(pos); lengths.Add(len); }
            }
            pos = end + 1;
        }
        _start = starts.ToArray();
        _length = lengths.ToArray();

        int size = 1;
        while (size < _start.Length * 2) size <<= 1;
        _slots = new int[size];
        _mask = size - 1;
        for (int i = 0; i < _start.Length; i++)
        {
            int h = Hash(_text.AsSpan(_start[i], _length[i])) & _mask;
            while (_slots[h] != 0) h = (h + 1) & _mask;
            _slots[h] = i + 1;
        }
    }

    /// <summary>Loads a Brotli-compressed UTF-8 word list from an embedded resource.</summary>
    public static CompactWordSet FromBrotliResource(System.Reflection.Assembly asm, string name)
    {
        using var raw = asm.GetManifestResourceStream(name)
            ?? throw new FileNotFoundException("Embedded resource not found: " + name);
        using var br = new BrotliStream(raw, CompressionMode.Decompress);
        using var reader = new StreamReader(br, Encoding.UTF8);
        return new CompactWordSet(reader.ReadToEnd());
    }

    public bool Contains(string word) => Contains(word.AsSpan());

    public bool Contains(ReadOnlySpan<char> word)
    {
        int h = Hash(word) & _mask;
        int idx;
        while ((idx = _slots[h]) != 0)
        {
            idx--;
            if (_length[idx] == word.Length && EqualsLower(_text.AsSpan(_start[idx], _length[idx]), word))
                return true;
            h = (h + 1) & _mask;
        }
        return false;
    }

    // FNV-1a over lower-cased chars.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Hash(ReadOnlySpan<char> s)
    {
        uint h = 2166136261;
        foreach (char c in s) { h ^= Lower(c); h *= 16777619; }
        return (int)(h ^ (h >> 15));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char Lower(char c) => c < 128 ? (c is >= 'A' and <= 'Z' ? (char)(c + 32) : c) : char.ToLowerInvariant(c);

    private static bool EqualsLower(ReadOnlySpan<char> stored, ReadOnlySpan<char> word)
    {
        for (int i = 0; i < stored.Length; i++)
            if (stored[i] != Lower(word[i])) return false;
        return true;
    }
}
