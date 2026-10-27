// Cross-platform parity: converts a fixed word list (built from the repo's word files)
// with the shared engine and prints the SHA-256 of the results. The Windows hash is
// kept in tests/data/parity.sha256; CI on Linux must reproduce it, and so must
// libyangialifbo.so (linux/tests/parity.py writes the same format).
//
//   dotnet run -c Release --project tools/parity -- <words-out.txt> <results-out.tsv>
using System.Security.Cryptography;
using System.Text;
using YangiAlifbo.Core;
using YangiAlifbo.Win32;

string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
string dict = Path.Combine(root, "src/YangiAlifbo.Core/Dictionaries");
var words = new List<string>();
var seen = new HashSet<string>(StringComparer.Ordinal);
void Add(string w) { if (w.Length > 0 && seen.Add(w)) words.Add(w); }
foreach (var file in new[] { Path.Combine(dict, "uzbek_words.txt"), Path.Combine(dict, "english_reject.txt"),
                             Path.Combine(dict, "chat_corpus_extracted.txt"), Path.Combine(root, "tools/dict/removed_foreign.txt"),
                             Path.Combine(root, "tools/dict/wiki_added.txt") })
{
    int i = 0;
    foreach (var line in File.ReadLines(file, Encoding.UTF8))
    {
        string w = line.Split('\t')[0].Trim();
        if (w.Length == 0 || w.StartsWith('#')) continue;
        Add(w);
        if (i % 10 == 0) Add(char.ToUpperInvariant(w[0]) + w[1..]);
        if (i % 37 == 0) Add(w.ToUpperInvariant());
        if (i % 7 == 0) Add(w + "lar");
        if (i % 11 == 0) Add(w.Replace("'", "’"));
        i++;
    }
}
File.WriteAllLines(args[0], words, new UTF8Encoding(false));

var d = new LanguageDetector();
var sb = new StringBuilder();
foreach (var w in words)
{
    sb.Append(w).Append('\t')
      .Append(WordConverter.Convert(d, w, TransliterationMode.Strict, slang: true)).Append('\t')
      .Append(WordConverter.Convert(d, w, TransliterationMode.Strict, slang: false)).Append('\t')
      .Append(WordConverter.Convert(d, w, TransliterationMode.Aggressive, slang: true)).Append('\n');
}
byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
File.WriteAllBytes(args[1], bytes);
Console.WriteLine($"{words.Count} words, sha256 {Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}");
d.Dispose();
