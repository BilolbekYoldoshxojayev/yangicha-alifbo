// Builds the embedded dictionaries in src/Core/Dictionaries and proves what they change.
//
//   dotnet run -c Release --project tools/compact -- wiki <wiki.tsv> <foreign_raw.txt> <out.tsv>
//       Uzbek Wikipedia words (tools/dict/wiki_words.py) the engine does not know yet and no
//       foreign list contains: candidates for uzbek_words.txt, for review.
//
//   dotnet run -c Release --project tools/compact -- build <foreign_raw.txt> <baseline_words.txt> [<extra_inputs.txt> ...]
//       1. uzbek_forms.br: every form of uzbek_words.txt in old spelling, minus forms the
//          morphology derives from other entries. A form is dropped only if every input
//          accepted with the full list is still accepted (checked, add-back until stable).
//       2. foreign_words.br: words of foreign_raw.txt (tools/dict/foreign_words.py) with
//          sh/ch/o'/g' that the engine does not accept as Uzbek and the chat corpus lacks.
//       3. english_reject.br: english_reject.txt, compressed.
//       Then compares every decision against the baseline lexicon (the previous
//       uzbek_words.txt) with the old reject list, and prints each kind of change.
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using YangiAlifbo.Core;

string dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Core/Dictionaries"));
HashSet<string> ReadFile(string path) => new(File.ReadLines(path, Encoding.UTF8)
    .Select(l => l.Split('\t')[0].Trim().ToLowerInvariant()).Where(l => l.Length > 0 && !l.StartsWith('#')), StringComparer.OrdinalIgnoreCase);
HashSet<string> Read(string f) => ReadFile(Path.Combine(dir, f));
HashSet<string> OldForms(IEnumerable<string> words) =>
    new(words.Select(LanguageDetector.ToOldSpelling), StringComparer.OrdinalIgnoreCase);

void WriteBrotli(string name, IEnumerable<string> words, string header = "")
{
    string text = header + string.Join('\n', words.OrderBy(w => w, StringComparer.Ordinal)) + "\n";
    using (var fs = File.Create(Path.Combine(dir, name)))
    using (var br = new BrotliStream(fs, new BrotliCompressionOptions { Quality = 11 }))
        br.Write(Encoding.UTF8.GetBytes(text));
    Console.WriteLine($"{name}: {Encoding.UTF8.GetByteCount(text):N0} bytes text, {new FileInfo(Path.Combine(dir, name)).Length:N0} bytes brotli");
}

var full = Read("uzbek_words.txt");
var reject = Read("english_reject.txt");
string mode = args.FirstOrDefault() ?? "";

if (mode == "wiki")
{
    var foreignRaw = ReadFile(args[2]);
    var det = new LanguageDetector(reject, new HashWordSet(OldForms(full)), oldSpellingOnly: true);
    int n = 0;
    using var o = new StreamWriter(args[3], false, new UTF8Encoding(false)) { NewLine = "\n" };
    foreach (string line in File.ReadLines(args[1]))
    {
        string w = line.Split('\t')[0];
        if (w.Length < 3 || foreignRaw.Contains(w) || reject.Contains(w) || det.IsUzbek(w)) continue;
        if (w.Contains('w') || w.Contains('-') || System.Text.RegularExpressions.Regex.IsMatch(w, "c(?!h)|[^a-z']|^'|'$|''")) continue;
        o.WriteLine(line);
        n++;
    }
    Console.WriteLine($"wiki candidates: {n:N0}");
    return 0;
}

if (mode != "build") { Console.WriteLine("usage: wiki ... | build ..."); return 2; }

var foreignRawB = ReadFile(args[1]);
var baseline = ReadFile(args[2]);
// Chat words stay off the foreign list (they may be Uzbek chat spellings), except the
// English ones removed from the lexicon by hand (tools/dict/removed_foreign.txt).
var chat = Read("chat_corpus_extracted.txt");
chat.ExceptWith(ReadFile(Path.Combine(dir, "../../../tools/dict/removed_foreign.txt")));

// ── 1. Prune forms the morphology derives ───────────────────────────
var forms = OldForms(full);
var sw = Stopwatch.StartNew();
var mask = new MaskSet(forms);
var maskDet = new LanguageDetector(reject, mask, oldSpellingOnly: true);
var pruned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
// Only leaves are candidates: a form that starts another form may be its stem. In ordinal
// order every form starting with f directly follows f.
var ordered = forms.OrderBy(f => f, StringComparer.Ordinal).ToArray();
for (int j = 0; j < ordered.Length; j++)
{
    string f = ordered[j];
    if (j + 1 < ordered.Length && ordered[j + 1].StartsWith(f, StringComparison.Ordinal)) continue;
    mask.Hidden = f;
    if (maskDet.IsUzbek(f)) pruned.Add(f);
}
mask.Hidden = null;
Console.WriteLine($"forms {forms.Count:N0}; derivable without their own entry {pruned.Count:N0} ({sw.Elapsed.TotalSeconds:F0}s)");

var fullDet = new LanguageDetector(reject, new HashWordSet(forms), oldSpellingOnly: true);

// Inputs: every entry (both spellings), reject words, case variants, entries with extra
// endings or cut short (so the morphology is exercised), and any extra word lists.
string[] extra = { "lar", "ni", "da", "dan", "ga", "imiz", "ning", "lik", "chi", "dagi", "si", "mi", "ku", "u", "-chi", "lardan" };
var inputs = new List<string>(full.Count * 3);
inputs.AddRange(full); inputs.AddRange(reject);
int k = 0;
foreach (var w in full)
{
    if (k % 40 == 0) inputs.Add(char.ToUpperInvariant(w[0]) + w[1..]);
    if (k % 90 == 0) inputs.Add(w.ToUpperInvariant());
    if (k % 2 == 0) inputs.Add(w + extra[k / 2 % extra.Length]);
    if (k % 50 == 0 && w.Length > 3) inputs.Add(w[..^1]);
    k++;
}
foreach (var n in new[] { "5-chi", "1-o'rin", "kel-chi", "o'z-o'zidan", "check-in", "Is'hoq", "mus'haf", "toshkentda" }) inputs.Add(n);
foreach (var f in args.Skip(3)) inputs.AddRange(ReadFile(f));
var accepted = inputs.Where(w => fullDet.IsUzbek(w)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
Console.WriteLine($"inputs {inputs.Count:N0}, accepted with the full list {accepted.Count:N0}");

int lastLost = -1;
for (int round = 1; ; round++)
{
    var kept = new HashSet<string>(forms.Where(f => !pruned.Contains(f)), StringComparer.OrdinalIgnoreCase);
    var det = new LanguageDetector(reject, new HashWordSet(kept), oldSpellingOnly: true);
    var lost = accepted.Where(w => !det.IsUzbek(w)).ToList();
    Console.WriteLine($"round {round}: stored {kept.Count:N0}, accepted inputs lost {lost.Count:N0}" +
                      (lost.Count is > 0 and < 20 ? " (" + string.Join(", ", lost) + ")" : ""));
    if (lost.Count == 0) break;
    if (lost.Count == lastLost)   // no progress: give back everything with the same first three letters
        foreach (var w in lost)
        {
            string o = LanguageDetector.ToOldSpelling(LanguageDetector.NormalizeForLookup(w));
            string head = o[..Math.Min(3, o.Length)];
            pruned.RemoveWhere(f => f.StartsWith(head, StringComparison.Ordinal));
        }
    lastLost = lost.Count;
    // Give back the input itself and every pruned form it could stem from: prefixes, and
    // forms that differ only in their last two letters (o'rtoq / o'rtog'i, boshlamoq / boshla).
    var byKey = pruned.GroupBy(f => f.Length > 4 ? f[..^2] : f, StringComparer.Ordinal)
                      .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
    foreach (var w in lost)
    {
        string o = LanguageDetector.ToOldSpelling(LanguageDetector.NormalizeForLookup(w));
        for (int len = 1; len <= o.Length; len++)
            if (byKey.TryGetValue(o[..len], out var back)) foreach (var f in back) pruned.Remove(f);
    }
    if (round == 20) { Console.WriteLine("not stable after 20 rounds; keeping every form"); pruned.Clear(); }
}
var stored = forms.Where(f => !pruned.Contains(f)).ToList();
WriteBrotli("uzbek_forms.br", stored, $"# forms: {full.Count}\n");

// ── 2. Foreign words ─────────────────────────────────────────────────
var foreign = foreignRawB.Where(w => UzbekPhonetics.HasConvertiblePatterns(w) && !reject.Contains(w) && !chat.Contains(w)
                                     && !fullDet.IsUzbek(w)).ToList();
WriteBrotli("foreign_words.br", foreign);
WriteBrotli("english_reject.br", reject);

// ── 3. Shipped data vs the baseline ───────────────────────────────────
CompactWordSet Load(string name)
{
    using var fs = File.OpenRead(Path.Combine(dir, name));
    using var br = new BrotliStream(fs, CompressionMode.Decompress);
    using var rd = new StreamReader(br, Encoding.UTF8);
    return new CompactWordSet(rd.ReadToEnd());
}
var newDet = new LanguageDetector(reject, Load("uzbek_forms.br"), oldSpellingOnly: true, foreign: Load("foreign_words.br"));
var oldDet = new LanguageDetector(reject, new HashSet<string>(baseline, StringComparer.OrdinalIgnoreCase));
var foreignSet = new HashSet<string>(foreign, StringComparer.OrdinalIgnoreCase);
var added = new HashSet<string>(OldForms(full.Except(baseline, StringComparer.OrdinalIgnoreCase)), StringComparer.OrdinalIgnoreCase);
var removed = OldForms(baseline.Except(full, StringComparer.OrdinalIgnoreCase));

inputs.AddRange(foreign.Where((_, j) => j % 10 == 0));
inputs.AddRange(baseline.Except(full, StringComparer.OrdinalIgnoreCase));
var kinds = new Dictionary<string, List<string>>();
void Note(string kind, string w) { if (!kinds.TryGetValue(kind, out var l)) kinds[kind] = l = new(); l.Add(w); }
sw.Restart();
foreach (var w in inputs.Distinct(StringComparer.Ordinal))
{
    string o = LanguageDetector.ToOldSpelling(LanguageDetector.NormalizeForLookup(w));
    bool s0 = oldDet.ShouldConvert(w, ConversionMode.Strict), s1 = newDet.ShouldConvert(w, ConversionMode.Strict);
    bool a0 = oldDet.ShouldConvert(w, ConversionMode.Aggressive), a1 = newDet.ShouldConvert(w, ConversionMode.Aggressive);
    if (s0 == s1 && a0 == a1) continue;
    bool isForeign = foreignSet.Contains(LanguageDetector.NormalizeForLookup(w)) || w.Split('-').Any(p => foreignSet.Contains(p.ToLowerInvariant()));
    string kind = s1 && !s0 ? (added.Any(a => o.StartsWith(a, StringComparison.Ordinal)) ? "strict: now converted (new word or its form)" : "strict: now converted (OTHER)")
        : s0 && !s1 ? (removed.Any(r => o.StartsWith(r, StringComparison.Ordinal)) ? "strict: no longer converted (removed entry)" : isForeign ? "strict: no longer converted (foreign list)" : "strict: no longer converted (OTHER)")
        : a0 && !a1 ? (isForeign ? "aggressive: foreign word now left alone" : "aggressive: now left alone (OTHER)")
        : "aggressive: now converted (OTHER)";
    Note(kind, w);
}
Console.WriteLine($"decision changes vs baseline ({sw.Elapsed.TotalSeconds:F0}s):");
foreach (var (kind, list) in kinds.OrderBy(p => p.Key))
    Console.WriteLine($"  {kind}: {list.Count:N0}  e.g. {string.Join(", ", list.Take(12))}");
File.WriteAllLines(Path.Combine(Path.GetTempPath(), "yangialifbo-dict-changes.tsv"),
    kinds.SelectMany(p => p.Value.Select(w => p.Key + "\t" + w)));
return kinds.Keys.Any(x => x.Contains("OTHER")) ? 1 : 0;

/// <summary>A word set with one word hidden, to ask "is it accepted without its own entry?"</summary>
sealed class MaskSet(HashSet<string> set) : IWordSet
{
    public string? Hidden;
    public bool Contains(string word) => set.Contains(word) && !string.Equals(word, Hidden, StringComparison.OrdinalIgnoreCase);
    public int Count => set.Count;
}
