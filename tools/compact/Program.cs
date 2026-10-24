// Builds src/Core/Dictionaries/uzbek_forms.br (every form of uzbek_words.txt, stored once
// in old spelling, Brotli-compressed) and proves the compact engine answers exactly like
// the full HashSet engine.
//   dotnet run -c Release --project tools/compact            build + verify
//   dotnet run -c Release --project tools/compact -- verify  verify only
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using YangiAlifbo.Core;

string dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Core/Dictionaries"));
HashSet<string> Read(string f) => new(File.ReadLines(Path.Combine(dir, f), Encoding.UTF8)
    .Select(l => l.Trim().ToLowerInvariant()).Where(l => l.Length > 0 && !l.StartsWith('#')), StringComparer.OrdinalIgnoreCase);

var full = Read("uzbek_words.txt");
var reject = Read("english_reject.txt");
string brPath = Path.Combine(dir, "uzbek_forms.br");

if (args.FirstOrDefault() != "verify")
{
    var forms = full.Select(LanguageDetector.ToOldSpelling).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(w => w, StringComparer.Ordinal);
    string text = $"# forms: {full.Count}\n" + string.Join('\n', forms) + "\n";
    byte[] utf8 = Encoding.UTF8.GetBytes(text);
    using (var fs = File.Create(brPath))
    using (var br = new BrotliStream(fs, new BrotliCompressionOptions { Quality = 11 }))
        br.Write(utf8);
    Console.WriteLine($"full entries {full.Count:N0}, stored forms {text.Count(c => c == '\n') - 1:N0}, text {utf8.Length:N0} bytes, brotli {new FileInfo(brPath).Length:N0} bytes");
}

var swLoad = Stopwatch.StartNew();
CompactWordSet compact;
using (var fs = File.OpenRead(brPath))
using (var br = new BrotliStream(fs, CompressionMode.Decompress))
using (var rd = new StreamReader(br, Encoding.UTF8))
    compact = new CompactWordSet(rd.ReadToEnd());
Console.WriteLine($"compact load: {swLoad.ElapsedMilliseconds} ms ({compact.Count:N0} forms)");

var fullDet = new LanguageDetector(reject, full);
var compactDet = new LanguageDetector(reject, compact, oldSpellingOnly: true);

// Inputs: every full entry (both spellings), the reject list, case variants, and words
// NOT in the lexicon (entries plus extra endings) so the morphology is compared too.
string[] extra = { "lar", "ni", "da", "dan", "ga", "imiz", "ning", "lik", "chi", "dagi", "si", "mi", "ku", "u", "-chi", "lardan" };
var inputs = new List<string>(full.Count * 3);
inputs.AddRange(full); inputs.AddRange(reject);
int i = 0;
foreach (var w in full)
{
    if (i % 40 == 0) inputs.Add(char.ToUpperInvariant(w[0]) + w[1..]);
    if (i % 90 == 0) inputs.Add(w.ToUpperInvariant());
    if (i % 2 == 0) inputs.Add(w + extra[i / 2 % extra.Length]);
    if (i % 50 == 0 && w.Length > 3) inputs.Add(w[..^1]);
    i++;
}
foreach (var n in new[] { "5-chi", "1-o'rin", "kel-chi", "o'z-o'zidan", "check-in", "Is'hoq", "mus'haf", "toshkentda" }) inputs.Add(n);

var sw = Stopwatch.StartNew();
int count = 0, sStrict = 0, sAggr = 0, sUz = 0, sRej = 0;
var examples = new List<string>();
foreach (var w in inputs)
{
    count++;
    bool a1 = fullDet.ShouldConvert(w, ConversionMode.Strict), b1 = compactDet.ShouldConvert(w, ConversionMode.Strict);
    if (a1 != b1) { sStrict++; if (examples.Count < 15) examples.Add($"{w}: full={a1} compact={b1}"); }
    if (fullDet.ShouldConvert(w, ConversionMode.Aggressive) != compactDet.ShouldConvert(w, ConversionMode.Aggressive)) sAggr++;
    bool u1 = fullDet.IsUzbek(w, ConversionMode.Strict), u2 = compactDet.IsUzbek(w, ConversionMode.Strict);
    if (u1 != u2) { sUz++; if (examples.Count < 15) examples.Add($"IsUzbek {w}: full={u1} compact={u2}"); }
    if (fullDet.IsForeignOrReject(w) != compactDet.IsForeignOrReject(w)) sRej++;
}
Console.WriteLine($"compared {count:N0} inputs in {sw.Elapsed.TotalSeconds:F0}s");
Console.WriteLine($"mismatches: ShouldConvert strict {sStrict}, aggressive {sAggr}, IsUzbek {sUz}, IsForeignOrReject {sRej}");
examples.ForEach(Console.WriteLine);

// Lookup speed on the same inputs (fresh detectors, no cache warm-up advantage).
var f2 = new LanguageDetector(reject, full); var c2 = new LanguageDetector(reject, compact, oldSpellingOnly: true);
var sample = inputs.Where((_, k) => k % 7 == 0).ToList();
var t = Stopwatch.StartNew(); foreach (var w in sample) f2.ShouldConvert(w, ConversionMode.Strict); double tf = t.Elapsed.TotalMicroseconds / sample.Count;
t.Restart(); foreach (var w in sample) c2.ShouldConvert(w, ConversionMode.Strict); double tc = t.Elapsed.TotalMicroseconds / sample.Count;
Console.WriteLine($"ShouldConvert mean: full {tf:F2} µs, compact {tc:F2} µs ({sample.Count:N0} words)");
return sStrict + sAggr + sUz + sRej == 0 ? 0 : 1;
