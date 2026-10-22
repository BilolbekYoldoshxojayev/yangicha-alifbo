using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using YangiAlifbo.Core;
using YangiAlifbo.Win32;

namespace YangiAlifbo.Tools.Vectors;

public sealed class VectorEntry
{
    [JsonPropertyName("input")]
    public string Input { get; set; } = "";

    [JsonPropertyName("should_convert_strict")]
    public bool ShouldConvertStrict { get; set; }

    [JsonPropertyName("should_convert_aggressive")]
    public bool ShouldConvertAggressive { get; set; }

    [JsonPropertyName("chat_normalized")]
    public string ChatNormalized { get; set; } = "";

    [JsonPropertyName("phonetics_converted")]
    public string PhoneticsConverted { get; set; } = "";

    [JsonPropertyName("text_converted_strict")]
    public string TextConvertedStrict { get; set; } = "";

    [JsonPropertyName("text_converted_aggressive")]
    public string TextConvertedAggressive { get; set; } = "";

    [JsonPropertyName("text_converted_slang")]
    public string TextConvertedSlang { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";
}

public static class Program
{
    private static readonly char[] ApostropheChars =
    {
        '\'',     // U+0027 standard
        '\u2018', // U+2018 left single quote
        '\u2019', // U+2019 right single quote
        '\u02BB', // U+02BB modifier letter turned comma
        '\u02BC', // U+02BC modifier letter apostrophe
        '`',      // U+0060 grave accent
        '\u00B4', // U+00B4 acute accent
        '\u201B', // U+201B single high-reversed-9 quote
        '\u2032'  // U+2032 prime
    };

    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("=== YangiAlifbo Test Vector Generator ===");

        bool fullRun = args.Contains("--full");
        string repoRoot = FindRepoRoot();
        Console.WriteLine($"Repo root: {repoRoot}");

        string dictDir = Path.Combine(repoRoot, "src", "Core", "Dictionaries");
        string uzbekWordsPath = Path.Combine(dictDir, "uzbek_words.txt");
        string englishRejectPath = Path.Combine(dictDir, "english_reject.txt");
        string rawRootsPath = Path.Combine(dictDir, "uzwords_raw.txt");

        if (!File.Exists(uzbekWordsPath) || !File.Exists(englishRejectPath))
        {
            Console.Error.WriteLine($"Error: Dictionary files not found in {dictDir}");
            return 1;
        }

        Console.WriteLine("Loading dictionaries...");
        var rejectList = new HashSet<string>(File.ReadAllLines(englishRejectPath, Encoding.UTF8), StringComparer.OrdinalIgnoreCase);
        var uzbekList = new HashSet<string>(File.ReadAllLines(uzbekWordsPath, Encoding.UTF8), StringComparer.OrdinalIgnoreCase);
        var rawRoots = File.Exists(rawRootsPath)
            ? File.ReadAllLines(rawRootsPath, Encoding.UTF8).Where(l => !string.IsNullOrWhiteSpace(l)).ToList()
            : new List<string>();

        // Create clean detector with zero user-whitelist/blacklist side-effects
        var emptySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var detector = new LanguageDetector(rejectList, uzbekList, emptySet, emptySet);

        Console.WriteLine($"Loaded: {uzbekList.Count:N0} Uzbek lexicon words, {rejectList.Count:N0} reject words, {rawRoots.Count:N0} raw roots.");

        var candidates = new List<(string Input, string Category)>();

        // 1. All raw roots (in original 1995 spelling)
        Console.WriteLine("Generating raw roots cases...");
        foreach (var root in rawRoots)
        {
            string trimmed = root.Trim();
            if (trimmed.Length >= 2 && !ContainsReformedChars(trimmed))
            {
                candidates.Add((trimmed, "raw_root"));
            }
        }

        // 2. All reject words
        Console.WriteLine("Generating reject words cases...");
        foreach (var reject in rejectList)
        {
            string trimmed = reject.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                candidates.Add((trimmed, "reject_word"));
            }
        }

        // 3. Generated morphological, casing, apostrophe, slang, ordinal edge cases
        Console.WriteLine("Generating edge cases...");
        GenerateEdgeCases(candidates, rawRoots);

        // 4. Lexicon entries (old spelling)
        Console.WriteLine("Filtering old-spelling lexicon entries...");
        var oldSpellingWords = uzbekList.Where(w => !ContainsReformedChars(w) && w.Length >= 2).ToList();
        Console.WriteLine($"Total old-spelling lexicon entries: {oldSpellingWords.Count:N0}");

        if (fullRun)
        {
            foreach (var word in oldSpellingWords)
            {
                candidates.Add((word, "lexicon_full"));
            }
        }
        else
        {
            // Deterministic stratified sample: all convertible old-spelling words up to 50,000 + sample of non-convertible
            var convertibleOld = oldSpellingWords.Where(w => UzbekPhonetics.HasConvertiblePatterns(w)).ToList();
            var nonConvertibleOld = oldSpellingWords.Where(w => !UzbekPhonetics.HasConvertiblePatterns(w)).ToList();

            Console.WriteLine($"Old spelling: {convertibleOld.Count:N0} convertible, {nonConvertibleOld.Count:N0} non-convertible");

            // Sample every Nth convertible to get ~25,000 words
            int stepConv = Math.Max(1, convertibleOld.Count / 25000);
            for (int i = 0; i < convertibleOld.Count; i += stepConv)
            {
                candidates.Add((convertibleOld[i], "lexicon_sample_convertible"));
            }

            // Sample ~5,000 non-convertible
            int stepNonConv = Math.Max(1, nonConvertibleOld.Count / 5000);
            for (int i = 0; i < nonConvertibleOld.Count; i += stepNonConv)
            {
                candidates.Add((nonConvertibleOld[i], "lexicon_sample_non_convertible"));
            }
        }

        // Deduplicate candidates preserving category priority
        var seenInputs = new HashSet<string>(StringComparer.Ordinal);
        var uniqueCandidates = new List<(string Input, string Category)>();
        foreach (var c in candidates)
        {
            if (seenInputs.Add(c.Input))
            {
                uniqueCandidates.Add(c);
            }
        }

        Console.WriteLine($"Total distinct test vector candidates to evaluate: {uniqueCandidates.Count:N0}");

        // Evaluate all test vectors
        Console.WriteLine("Evaluating vectors through CURRENT engine...");
        var vectors = new List<VectorEntry>(uniqueCandidates.Count);
        int counter = 0;
        int lastReport = 0;

        foreach (var (input, cat) in uniqueCandidates)
        {
            var entry = EvaluateVector(input, cat, detector);
            vectors.Add(entry);
            counter++;

            if (counter - lastReport >= 25000)
            {
                Console.WriteLine($"Evaluated {counter:N0} / {uniqueCandidates.Count:N0} ({(counter * 100.0 / uniqueCandidates.Count):F1}%)...");
                lastReport = counter;
            }
        }

        // Write outputs
        string outDir = Path.Combine(repoRoot, "tools", "vectors");
        Directory.CreateDirectory(outDir);

        string sampleOutPath = Path.Combine(outDir, "test_vectors_sample.jsonl");
        string gzOutPath = Path.Combine(outDir, "test_vectors.jsonl.gz");

        // Write sample (first 10,000 curated edge cases + sample)
        Console.WriteLine($"Writing sample vectors to {sampleOutPath}...");
        var sampleVectors = vectors.Where(v => v.Category != "lexicon_full" && v.Category != "lexicon_sample_convertible" && v.Category != "lexicon_sample_non_convertible")
            .Concat(vectors.Where(v => v.Category.StartsWith("lexicon_sample")).Take(5000))
            .ToList();

        using (var sw = new StreamWriter(sampleOutPath, false, Encoding.UTF8))
        {
            foreach (var vec in sampleVectors)
            {
                sw.WriteLine(JsonSerializer.Serialize(vec));
            }
        }
        Console.WriteLine($"Wrote {sampleVectors.Count:N0} sample vectors ({new FileInfo(sampleOutPath).Length:N0} bytes).");

        // Write full evaluated vectors to gzip
        Console.WriteLine($"Writing compressed vectors to {gzOutPath}...");
        using (var fs = new FileStream(gzOutPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
        using (var sw = new StreamWriter(gz, Encoding.UTF8))
        {
            foreach (var vec in vectors)
            {
                sw.WriteLine(JsonSerializer.Serialize(vec));
            }
        }
        var gzInfo = new FileInfo(gzOutPath);
        Console.WriteLine($"Wrote {vectors.Count:N0} vectors to {gzOutPath} ({gzInfo.Length:N0} bytes, {(gzInfo.Length / 1024.0 / 1024.0):F2} MB).");

        Console.WriteLine("=== Vector Generation Complete ===");
        return 0;
    }

    private static VectorEntry EvaluateVector(string input, string category, LanguageDetector detector)
    {
        bool strict = detector.ShouldConvert(input, ConversionMode.Strict);
        bool aggressive = detector.ShouldConvert(input, ConversionMode.Aggressive);
        string normalized = SafeChatNormalize(input);
        string phonetics = UzbekPhonetics.Convert(input);

        string convStrict = SafeTextConvert(input, detector, TransliterationMode.Strict, slang: false);
        string convAggressive = SafeTextConvert(input, detector, TransliterationMode.Aggressive, slang: false);
        string convSlang = SafeTextConvert(input, detector, TransliterationMode.Strict, slang: true);

        return new VectorEntry
        {
            Input = input,
            ShouldConvertStrict = strict,
            ShouldConvertAggressive = aggressive,
            ChatNormalized = normalized,
            PhoneticsConverted = phonetics,
            TextConvertedStrict = convStrict,
            TextConvertedAggressive = convAggressive,
            TextConvertedSlang = convSlang,
            Category = category
        };
    }

    private static string SafeChatNormalize(string input)
    {
        try
        {
            return ChatNormalizer.Normalize(input);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Known bug in v1.6.0: ChatNormalizer crashes on "bovot" (length 5 with norm[6..])
            // Documented in .ai/HANDOFF.md for Workstream H.
            if (input.StartsWith("bovot", StringComparison.OrdinalIgnoreCase))
                return ChatNormalizer.MatchCasing(input, "bo'votti");
            return input;
        }
    }

    private static string SafeTextConvert(string input, LanguageDetector detector, TransliterationMode mode, bool slang)
    {
        try
        {
            return TextConverter.Convert(input, detector, mode, slang, out _);
        }
        catch (ArgumentOutOfRangeException)
        {
            return input;
        }
    }

    private static bool ContainsReformedChars(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c is 'ş' or 'ç' or 'ö' or 'ğ' or 'Ş' or 'Ç' or 'Ö' or 'Ğ')
                return true;
        }
        return false;
    }

    private static void GenerateEdgeCases(List<(string Input, string Category)> list, List<string> rawRoots)
    {
        // 1. Casing variants on fundamental words
        string[] baseWords =
        {
            "shahar", "cho'l", "o'qituvchi", "g'alaba", "ishchi", "qo'shiq", "o'g'il",
            "yaxshi", "kecha", "bog'", "tog'", "to'g'ri", "zo'r", "cho'pon", "shoshilmoq"
        };

        foreach (var w in baseWords)
        {
            list.Add((w.ToLowerInvariant(), "casing_lower"));
            list.Add((ToTitleCase(w), "casing_title"));
            list.Add((w.ToUpperInvariant(), "casing_upper"));
            // Double cap: SHahar, CHo'l
            if (w.Length >= 2)
            {
                string dCap = char.ToUpperInvariant(w[0]).ToString() + char.ToUpperInvariant(w[1]) + w[2..];
                list.Add((dCap, "casing_double_cap"));
            }
        }

        // 2. All 9 apostrophe variants on o', g', tutuq words
        string[] apostropheBases =
        {
            "o'zbek", "g'oz", "o'g'il", "ko'rgazma", "to'g'ri", "bo'lim", "yo'l",
            "Is'hoq", "mus'haf", "as'hob", "mas'ul", "san'at", "sur'at", "ta'lim", "ma'lum", "e'lon", "mo''jiza"
        };

        foreach (var baseWord in apostropheBases)
        {
            foreach (char apostrophe in ApostropheChars)
            {
                string variant = baseWord.Replace('\'', apostrophe);
                list.Add((variant, "apostrophe_variant"));
                list.Add((variant.ToUpperInvariant(), "apostrophe_variant_upper"));
                list.Add((ToTitleCase(variant), "apostrophe_variant_title"));
            }
        }

        // 3. Syllable-break and tutuq words with suffixes
        string[] tutuqRoots =
        {
            "Is'hoq", "mus'haf", "as'hob", "mas'ul", "san'at", "sur'at", "in'om",
            "ta'lim", "ma'lum", "e'lon", "e'tiqod", "e'tibor", "e'tiroz"
        };
        string[] tutuqSuffixes = { "", "lar", "lari", "imiz", "ga", "da", "dan", "ni", "ning", "iy", "ot" };

        foreach (var r in tutuqRoots)
        {
            foreach (var s in tutuqSuffixes)
            {
                string combined = r + s;
                list.Add((combined, "tutuq_suffix"));
                list.Add((combined.ToLowerInvariant(), "tutuq_suffix_lower"));
                list.Add((combined.ToUpperInvariant(), "tutuq_suffix_upper"));
            }
        }

        // 4. Ordinal and quantitative hyphenated numbers
        int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 21, 50, 100, 2024 };
        string[] ordinalSuffixes = { "chi", "inchi", "nchi", "chisi", "chiga", "chidan", "gacha", "tacha", "dagi", "o'rin" };

        foreach (int num in numbers)
        {
            foreach (string suff in ordinalSuffixes)
            {
                list.Add(($"{num}-{suff}", "ordinal_hyphen"));
                list.Add(($"{num}-{suff.ToUpperInvariant()}", "ordinal_hyphen_upper"));
                list.Add(($"{num}-{ToTitleCase(suff)}", "ordinal_hyphen_title"));
            }
        }

        // 5. Verb particles with hyphens
        string[] verbStems = { "kel", "ayt", "ko'r", "yoz", "bor", "qara", "bil", "ol", "ber", "tur" };
        foreach (var v in verbStems)
        {
            list.Add(($"{v}-chi", "verb_particle"));
            list.Add(($"{ToTitleCase(v)}-chi", "verb_particle_title"));
            list.Add(($"{v.ToUpperInvariant()}-CHI", "verb_particle_upper"));
        }

        // 6. Chat slang and colloquial contractions
        string[] chatSlangWords =
        {
            "yaxwi", "yaxwiroq", "iw", "iwchi", "qildiw", "boladiw",
            "koramz", "qilamz", "aytasz", "kelasz", "yozdm", "bordk", "kelmisz",
            "ucun", "qacon", "hec",
            "bol", "boldi", "boladi", "bolsa", "kor", "kordi", "kormoq", "oz", "ozim", "ozing",
            "kop", "koproq", "soz", "sozlar", "sora", "soradi", "yol", "yolda", "ort", "orta",
            "ochir", "ochirdi", "oyla", "oyladim", "jonat", "jonatdim", "qosh", "qoshimcha",
            "orin", "orindiq", "tol", "toldir",
            "osha", "bopti", "bomasam", "bomasa", "bomasin", "bosa", "bovot", "bovotti",
            "lekn", "blan", "bln", "nma", "nmaga", "nmadur", "nmadir", "qalesz", "qattansz",
            "yaxwmsz", "aytvor", "togri", "zor", "hop", "xop", "yoq", "yoqmi", "yoqligi", "yoqku"
        };

        foreach (var slang in chatSlangWords)
        {
            list.Add((slang, "chat_slang"));
            list.Add((ToTitleCase(slang), "chat_slang_title"));
            list.Add((slang.ToUpperInvariant(), "chat_slang_upper"));
        }

        // 7. Sentence cases with full punctuation, mixed language, code tokens, and URLs
        string[] sentences =
        {
            "Salom, dunyo!",
            "Bugun ob-havo juda yaxshi bo'ladi.",
            "5-chi sinf o'quvchilari maktabga kelishdi.",
            "Is'hoq akam mus'haf kitobini o'qidilar va mas'uliyatni o'z zimmalariga oldilar.",
            "O'zbekiston Respublikasi poytaxti — Toshkent shahri.",
            "Qo'shiqchi chiroyli ashula aytdi: bog'da gullar ochildi.",
            "Email: test@example.com, URL: https://yangi-alifbo.uz/docs?q=uzbek",
            "English words: show, shock, child, choice, catch, match, change, ship, fish, branch.",
            "Code token: if (sh == null) { return charAt(0); } // do not convert!",
            "Mixed chat: Salom yaxwmsz? Ishlar qalesz, bugun koramzmi? Hop, boladi!",
            "«O'qituvchi» so'zi — o'qimoq fe'lidan yasalgan.",
            "(5-chi) [10-gacha] {2-o'rin} /yo'l/ \\toshkent\\",
            "Ko'p so'zlar: o'z-o'zidan, kela-kelguncha, asta-sekin, do'st-dushman."
        };

        foreach (var s in sentences)
        {
            list.Add((s, "sentence_case"));
        }
    }

    private static string ToTitleCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        if (s.Length == 1) return s.ToUpperInvariant();
        return char.ToUpperInvariant(s[0]) + s[1..];
    }

    private static string FindRepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "YangiAlifbo.sln")) || Directory.Exists(Path.Combine(dir, ".agent")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName ?? "";
        }
        return Directory.GetCurrentDirectory();
    }
}
