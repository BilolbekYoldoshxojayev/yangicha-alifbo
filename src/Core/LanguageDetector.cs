// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — LanguageDetector.cs
// 3-tier Uzbek/foreign word discrimination engine.
// Tier 1: English/tech reject list (HashSet, ~10K+ words)
// Tier 2: Uzbek lexicon exact match with dual 1995/2024 indexing (HashSet, ~110K+ words)
// Tier 3: Uzbek morphological affix heuristic (140+ affixes, consonant alternation, verb infinitives)
// Plus user-editable whitelist/blacklist overrides with FileSystemWatcher hot-reloading.
// ──────────────────────────────────────────────────────────────────────

using System.Reflection;
using System.Text;

namespace YangiAlifbo.Core;

/// <summary>
/// Mode of transliteration conversion.
/// </summary>
public enum ConversionMode
{
    Strict,
    Aggressive
}

/// <summary>
/// Interface contract for linguistic discrimination.
/// </summary>
public interface ILanguageDetector
{
    bool ShouldConvert(string word, ConversionMode mode = ConversionMode.Strict);
    bool IsUzbek(string word);
    void AddToWhitelist(string word);
    void AddToBlacklist(string word);
    void ReloadUserLists();
}

/// <summary>
/// Determines whether a given word is Uzbek (and should be transliterated)
/// or foreign (and should be left untouched). Uses a 3-tier pipeline of
/// increasingly sophisticated checks.
/// </summary>
public sealed class LanguageDetector : ILanguageDetector, IDisposable
{
    // ── Dictionary data ────────────────────────────────────────────────

    private readonly HashSet<string> _englishReject;
    private readonly HashSet<string> _uzbekLexicon;
    private HashSet<string> _userWhitelist;
    private HashSet<string> _userBlacklist;

    private readonly FileSystemWatcher? _watcher;
    private readonly System.Threading.Timer? _debounceTimer;
    private readonly object _fileLock = new();
    private bool _isDisposed;

    // ── 140+ Known Uzbek suffixes for morphological analysis ───────────

    /// <summary>
    /// Common Uzbek suffixes, ordered strictly by length descending for greedy stripping.
    /// Uzbek is agglutinative — multiple suffixes can chain.
    /// </summary>
    private static readonly string[] UzbekSuffixes = new[]
    {
        // 14-letter suffixes
        "larimiznikidan",
        // 13-letter suffixes
        "larimizdandir", "larimiznikida", "larimiznikiga",
        // 12-letter suffixes
        "larimizgacha",
        // 11-letter suffixes
        "adiganimiz", "adiganingiz", "averasizlar", "ayotgandilar", "ayotgandimiz", "ayotgandingiz", "larimizdagi", "larimizniki", "larimizning", "laringizdan",
        "larinikidan", "ydiganimiz", "ydiganingiz",
        // 10-letter suffixes
        "adiganlari", "averadilar", "ayotgandik", "ayotgandim", "ayotganding", "yotgandik", "yotgandim", "yotganding", "averdingiz", "averganlar", "aversangiz", "chilikdagi",
        "larimizdan", "laringizda", "laringizga", "laringizni", "laringning",
        "larinikida", "larinikiga", "miznikidan", "olmaydilar", "verasizlar", "ydiganlari",
        // 9-letter suffixes
        "adiganlar", "averaylik", "ayotgandi", "yotgandi", "averdilar", "aversalar", "ayotganlar", "chilikdan", "larimizda",
        "larimizga", "larimizni", "larimning", "laringdan", "miznikida",
        "miznikiga", "moqchilar", "moqchiman", "moqchimiz", "moqchisan",
        "moqchisiz", "sayliklar", "veradilar", "verdingiz", "verganlar",
        "versangiz", "ydiganlar",
        // 8-letter suffixes
        "adiganda", "adigandan", "adiganga", "adiganni", "averaman", "averamiz",
        "averasan", "averasiz", "averding", "avergach", "avergani", "aversang",
        "ayliklar", "chilikda", "chilikka", "chilikni", "ganingiz", "ingizdan",
        "ingizlar", "kaningiz", "larimdan", "laringda", "laringga", "laringiz",
        "laringni", "lariniki", "larining", "maganlar", "maydigan", "maydilar",
        "moqdalar", "moqdaman", "moqdamiz", "moqdasan", "moqdasiz",
        "oladilar", "oldingiz", "olganlar", "olmasdan", "olmaslik",
        "olmaymiz", "olsangiz", "qaningiz", "veraylik", "verdilar",
        "versalar", "yaptilar", "ydiganda", "ydigandan", "ydiganga", "ydiganni",
        // 7-letter suffixes
        "adigan", "averadi", "averdik", "averdim", "avergan", "averish",
        "avermoq", "aversak", "aversam", "ayotgan", "chasiga", "dagilar",
        "gandilar", "gandimiz", "ganimiz", "imizdan", "ingizda", "ingizga", "ingizni",
        "kandilar", "kandimiz", "kanimiz", "laridan", "larimda", "larimga", "larimiz",
        "larimni", "larning", "likdagi", "likning", "madilar", "maslikka", "maslikni",
        "mizniki", "nikidan", "nikilar", "oldilar", "olmagan",
        "olmaydi", "olsalar", "qandilar", "qandimiz", "qanimiz", "veraman", "veramiz",
        "verasan", "verasiz", "verding", "versang", "ydigan", "yotibdi",
        // 6-letter suffixes
        "adilar", "averdi", "aversa", "chilik", "dingiz",
        "gancha", "gandan", "ganday", "gandek", "gandik", "gandim", "ganding", "ganing",
        "ganlar", "ganlik", "guncha", "ibdilar", "imizda", "imizga",
        "imizni", "inglar", "kancha", "kandan", "kanday",
        "kandek", "kandik", "kandim", "kanding", "kaning", "kanlar", "kuncha", "lardan",
        "larida", "lariga", "laring", "larini", "likdan",
        "masdan", "maslik", "moqchi", "moqdam", "nikida", "nikiga",
        "nikini", "olaman", "olamiz", "olasan", "olasiz",
        "olding", "olsang", "qancha", "qandan", "qanday",
        "qandek", "qandik", "qandim", "qanding", "qaning", "qanlar", "quncha", "sangiz",
        "saydik", "saydim", "saylik", "sinlar", "sizlik", "tingiz", "veradi",
        "verdik", "verdim", "vergan", "verish", "vermoq",
        "versak", "versam", "yapman", "yapmiz", "yapsan",
        "yapsiz", "ydilar", "yotgan",
        // 5-letter suffixes
        "aylik", "dilar", "ganda", "ganim", "ganin",
        "ibman", "ibmiz", "ibsan", "ibsiz", "ingiz", "kanda", "kanim", "kanin", "larda",
        "larga", "larim", "larni", "likda", "likka",
        "likni", "magan", "maydi", "mayman", "maymiz", "maysan", "maysiz",
        "mikan", "moqda",
        "oladi", "oldik", "oldim", "olgan", "olish",
        "olmoq", "olsak", "olsam", "qanda", "qanim",
        "qanin", "salar", "samiz", "sasiz", "saydi", "tilar",
        "verdi", "versa", "yapti", "yotir",
        // 4-letter suffixes
        "aman", "amiz", "asan", "asiz", "aver",
        "ayin", "chak", "choq", "dagi", "ding",
        "dosh", "gach", "gani", "gina", "ibdi", "imiz",
        "kach", "kani", "kina", "lari", "madi", "madik", "madim", "mading",
        "mish", "niki", "ning", "oldi", "olsa",
        "qach", "qani", "qina", "sang", "ting",
        "yman", "ymiz", "ysan", "ysiz",
        // 3-letter suffixes
        "adi", "ani", "cha", "chi", "dan",
        "day", "dek", "dik", "dim", "dir",
        "gan", "gil", "gin", "ida", "iga",
        "ini", "ish", "kan", "lar", "lik",
        "mas", "may", "miz", "moq", "qan", "roq", "sak",
        "sam", "san", "sin", "siz", "tik",
        "tim", "ver", "ydi",
        // 2-letter suffixes
        "ay", "da", "di", "ga", "ib", "im",
        "in", "ka", "ku", "li", "ma", "mi", "ng", "ni",
        "ov", "qa", "sa", "sh", "si", "ti", "uv", "yu",
        // 1-letter suffixes
        "a", "b", "i", "k", "m", "q", "u", "v", "y"
    };

    /// <summary>
    /// AppData directory for user configuration files.
    /// </summary>
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YangiAlifbo");

    private static readonly string WhitelistPath = Path.Combine(AppDataDir, "user_whitelist.txt");
    private static readonly string BlacklistPath = Path.Combine(AppDataDir, "user_blacklist.txt");

    // ── Constructors ───────────────────────────────────────────────────

    public LanguageDetector()
    {
        _englishReject = LoadEmbeddedWordList("english_reject.txt");
        _uzbekLexicon = LoadEmbeddedWordList("uzbek_words.txt");

        EnsureUserFilesExist();
        _userWhitelist = LoadUserFile(WhitelistPath);
        _userBlacklist = LoadUserFile(BlacklistPath);

        _debounceTimer = new System.Threading.Timer(DebounceTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        _watcher = InitializeWatcher();
    }

    /// <summary>
    /// Constructor for testing — accepts pre-built dictionaries.
    /// </summary>
    public LanguageDetector(
        HashSet<string> englishReject,
        HashSet<string> uzbekLexicon,
        HashSet<string>? userWhitelist = null,
        HashSet<string>? userBlacklist = null)
    {
        _englishReject = englishReject;
        _uzbekLexicon = uzbekLexicon;
        _userWhitelist = userWhitelist ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _userBlacklist = userBlacklist ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _debounceTimer = null;
        _watcher = null;
    }

    // ── Public API ─────────────────────────────────────────────────────

    /// <summary>
    /// Determines whether a word should be converted based on phonetic patterns,
    /// conversion mode, and 3-tier discrimination.
    /// </summary>
    public bool ShouldConvert(string word, ConversionMode mode = ConversionMode.Strict)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 2)
            return false;

        if (!UzbekPhonetics.HasConvertiblePatterns(word))
            return false;

        string normalized = NormalizeForLookup(word);

        if (_userBlacklist.Contains(normalized))
            return false;

        if (_userWhitelist.Contains(normalized))
            return true;

        if (mode == ConversionMode.Aggressive)
            return true;

        return IsUzbek(word);
    }

    /// <summary>
    /// Determines whether a word is Uzbek and should be transliterated (Strict mode).
    /// </summary>
    /// <param name="word">The word as typed by the user.</param>
    /// <returns>True if the word is identified as Uzbek.</returns>
    public bool IsUzbek(string word)
    {
        return IsUzbek(word, ConversionMode.Strict);
    }

    /// <summary>
    /// Determines whether a word is Uzbek with explicit mode control.
    /// </summary>
    public bool IsUzbek(string word, ConversionMode mode)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 2)
            return false;

        string normalized = NormalizeForLookup(word);

        // ── User blacklist — highest priority reject ───────────────
        if (_userBlacklist.Contains(normalized))
            return false;

        // ── User whitelist — highest priority accept ───────────────
        if (_userWhitelist.Contains(normalized))
            return true;

        if (mode == ConversionMode.Aggressive)
            return true;

        // ── Tier 1: English/foreign reject (fast path) ─────────────
        if (_englishReject.Contains(normalized))
            return false;

        // ── Tier 2: Uzbek lexicon exact match (dual orthography) ───
        if (_uzbekLexicon.Contains(normalized))
            return true;

        // Also check the converted 2024 form (or vice versa)
        string converted = UzbekPhonetics.Convert(normalized);
        if (converted != normalized && _uzbekLexicon.Contains(converted))
            return true;

        // ── Tier 3: Morphological affix stripping ──────────────────
        if (TryMorphologicalMatch(normalized))
            return true;

        if (converted != normalized && TryMorphologicalMatch(converted))
            return true;

        // ── Default: unknown word → do not convert ─────────────────
        return false;
    }

    /// <summary>Adds a word to the user whitelist and persists it to disk.</summary>
    public void AddToWhitelist(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return;
        string norm = NormalizeForLookup(word);
        if (_userWhitelist.Contains(norm)) return;
        lock (_fileLock)
        {
            if (_userWhitelist.Contains(norm)) return;
            var updated = new HashSet<string>(_userWhitelist, StringComparer.OrdinalIgnoreCase) { norm };
            Interlocked.Exchange(ref _userWhitelist, updated);
            try
            {
                Directory.CreateDirectory(AppDataDir);
                File.AppendAllLines(WhitelistPath, new[] { norm }, Encoding.UTF8);
            }
            catch { }
        }
    }

    /// <summary>Adds a word to the user blacklist and persists it to disk.</summary>
    public void AddToBlacklist(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return;
        string norm = NormalizeForLookup(word);
        if (_userBlacklist.Contains(norm)) return;
        lock (_fileLock)
        {
            if (_userBlacklist.Contains(norm)) return;
            var updated = new HashSet<string>(_userBlacklist, StringComparer.OrdinalIgnoreCase) { norm };
            Interlocked.Exchange(ref _userBlacklist, updated);
            try
            {
                Directory.CreateDirectory(AppDataDir);
                File.AppendAllLines(BlacklistPath, new[] { norm }, Encoding.UTF8);
            }
            catch { }
        }
    }

    /// <summary>Reloads user whitelist and blacklist from disk.</summary>
    public void ReloadUserLists()
    {
        var newWhite = LoadUserFile(WhitelistPath, _userWhitelist);
        var newBlack = LoadUserFile(BlacklistPath, _userBlacklist);
        Interlocked.Exchange(ref _userWhitelist, newWhite);
        Interlocked.Exchange(ref _userBlacklist, newBlack);
    }

    /// <summary>Gets the path to the user whitelist file.</summary>
    public static string GetWhitelistPath() => WhitelistPath;

    /// <summary>Gets the path to the user blacklist file.</summary>
    public static string GetBlacklistPath() => BlacklistPath;

    /// <summary>Returns the number of words in the Uzbek lexicon.</summary>
    public int UzbekLexiconCount => _uzbekLexicon.Count;

    /// <summary>Returns the number of words in the English reject list.</summary>
    public int EnglishRejectCount => _englishReject.Count;

    // ── Tier 3: Morphological Analysis ─────────────────────────────────

    /// <summary>
    /// Attempts to match a word by stripping known Uzbek suffixes and
    /// checking if the remaining stem exists in the lexicon.
    /// Handles root consonant alternation (g' -> q, g -> k) and verb infinitive resolution (stem + moq).
    /// </summary>
        /// <summary>
    /// Suffixes that can legitimately appear inside chained suffix sequences (Levels 2 & 3).
    /// Limited to short nominal, possessive, plural, and participial morphemes to prevent
    /// combinatorial explosion while preserving 100% recognition of deep agglutinative chains.
    /// </summary>
    private static readonly string[] ChainableInnerSuffixes = new[]
    {
        // 7-letter
        "ayotgan", "adigan",
        // 6-letter
        "yotgan", "ydigan", "magan",
        // 5-letter
        "larim", "larga", "larda", "larni", "ingiz",
        // 4-letter
        "lari", "dagi", "dosh", "gina", "kina", "qina", "imiz",
        // 3-letter
        "lar", "lik", "chi", "dan", "dek", "day", "gan", "qan", "kan",
        "ish", "sak", "sam", "san", "siz", "miz", "ini", "ida", "iga",
        // 2-letter
        "da", "di", "ga", "ka", "qa", "ni", "im", "in", "ma", "sa", "sh", "si", "ti", "ib", "li",
        // 1-letter
        "i", "k", "m", "q", "b"
    };

    /// <summary>
    /// Attempts to match a word by stripping known Uzbek suffixes and
    /// checking if the remaining stem exists in the lexicon.
    /// Handles root consonant alternation (g' -> q, g -> k) and verb infinitive resolution (stem + moq).
    /// </summary>
    private bool TryMorphologicalMatch(string word)
    {
        foreach (string suffix in UzbekSuffixes)
        {
            if (word.Length > suffix.Length + 1 && word.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                string stem = word[..^suffix.Length];
                if (CheckStem(stem))
                    return true;

                // Level 2 stripping for chained suffixes (using fast inner suffix set)
                foreach (string suffix2 in ChainableInnerSuffixes)
                {
                    if (stem.Length > suffix2.Length + 1 && stem.EndsWith(suffix2, StringComparison.OrdinalIgnoreCase))
                    {
                        string stem2 = stem[..^suffix2.Length];
                        if (CheckStem(stem2))
                            return true;

                        // Level 3 stripping for deeply chained suffixes (e.g. o'rtoq + lar + imiz + dan)
                        foreach (string suffix3 in ChainableInnerSuffixes)
                        {
                            if (stem2.Length > suffix3.Length + 1 && stem2.EndsWith(suffix3, StringComparison.OrdinalIgnoreCase))
                            {
                                string stem3 = stem2[..^suffix3.Length];
                                if (CheckStem(stem3))
                                    return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Verifies whether a candidate stem is valid in the Uzbek lexicon:
    /// 1. Exact match in 1995 or 2024 orthography.
    /// 2. Verb infinitive resolution (stem + "moq").
    /// 3. Root consonant alternation (stem ending in g' -> root ending in q; stem ending in g -> root ending in k).
    /// </summary>
    private bool CheckStem(string stem)
    {
        if (stem.Length < 2) return false;

        // 1. Direct match
        if (_uzbekLexicon.Contains(stem)) return true;

        string converted = UzbekPhonetics.Convert(stem);
        if (converted != stem && _uzbekLexicon.Contains(converted)) return true;

        // 2. Verb infinitive check (stems stripped of verbal endings)
        string verbInfinitive = stem + "moq";
        if (_uzbekLexicon.Contains(verbInfinitive)) return true;

        string convVerb = UzbekPhonetics.Convert(verbInfinitive);
        if (convVerb != verbInfinitive && _uzbekLexicon.Contains(convVerb)) return true;

        // 3. Consonant Alternation: g' -> q (e.g., o'rtog' -> o'rtoq, qishlog' -> qishloq)
        if (stem.EndsWith('\'') && stem.Length > 2)
        {
            if (stem.EndsWith("g'", StringComparison.OrdinalIgnoreCase))
            {
                string qStem = stem[..^2] + "q";
                if (_uzbekLexicon.Contains(qStem)) return true;
                string convQ = UzbekPhonetics.Convert(qStem);
                if (convQ != qStem && _uzbekLexicon.Contains(convQ)) return true;
            }
        }
        else if ((stem.EndsWith('ğ') || stem.EndsWith('Ğ')) && stem.Length > 2)
        {
            string qStem = stem[..^1] + "q";
            if (_uzbekLexicon.Contains(qStem)) return true;
            string convQ = UzbekPhonetics.Convert(qStem);
            if (convQ != qStem && _uzbekLexicon.Contains(convQ)) return true;
        }

        // 4. Consonant Alternation: g -> k (e.g., yurag -> yurak, eshig -> eshik)
        if (stem.EndsWith("g", StringComparison.OrdinalIgnoreCase) && stem.Length > 2)
        {
            string kStem = stem[..^1] + "k";
            if (_uzbekLexicon.Contains(kStem)) return true;
            string convK = UzbekPhonetics.Convert(kStem);
            if (convK != kStem && _uzbekLexicon.Contains(convK)) return true;
        }

        return false;
    }

    // ── Normalization ──────────────────────────────────────────────────

    /// <summary>
    /// Normalizes a word for dictionary lookup: lowercases and
    /// normalizes all apostrophe variants to the standard apostrophe.
    /// </summary>
    public static string NormalizeForLookup(string word)
    {
        return UzbekPhonetics.NormalizeApostrophes(word).ToLowerInvariant();
    }

    // ── Dictionary Loading ─────────────────────────────────────────────

    /// <summary>
    /// Loads a word list from an embedded resource. One word per line.
    /// </summary>
    private static HashSet<string> LoadEmbeddedWordList(string resourceName)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Warning: Embedded resource '{resourceName}' not found.");
            return result;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            string trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith('#'))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }

    /// <summary>
    /// Loads a user-editable word list from disk with retry loop and shared read.
    /// If an IO exception occurs, preserves the existing cached set rather than returning an empty set.
    /// </summary>
    private static HashSet<string> LoadUserFile(string path, HashSet<string>? existingCache = null)
    {
        if (!File.Exists(path))
            return existingCache ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool success = false;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, Encoding.UTF8);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith('#') && !trimmed.StartsWith(';'))
                    {
                        result.Add(NormalizeForLookup(trimmed));
                    }
                }
                success = true;
                break;
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
            catch
            {
                break;
            }
        }

        if (success)
            return result;

        return existingCache ?? result;
    }

    /// <summary>
    /// Creates the AppData directory and empty user files if they don't exist.
    /// </summary>
    private static void EnsureUserFilesExist()
    {
        try
        {
            Directory.CreateDirectory(AppDataDir);

            if (!File.Exists(WhitelistPath))
            {
                File.WriteAllText(WhitelistPath,
                    "# YangiAlifbo — User Whitelist\n" +
                    "# Qo'shimcha o'zbek so'zlarini qo'shing (har bir qatorda bitta so'z).\n" +
                    "# Add additional Uzbek words here (one per line).\n" +
                    "# Bu so'zlar har doim o'zbekcha deb tan olinadi.\n" +
                    "# These words will always be recognized as Uzbek.\n\n",
                    Encoding.UTF8);
            }

            if (!File.Exists(BlacklistPath))
            {
                File.WriteAllText(BlacklistPath,
                    "# YangiAlifbo — User Blacklist\n" +
                    "# O'zgartirilmasin degan so'zlarni qo'shing (har bir qatorda bitta so'z).\n" +
                    "# Add words that should NEVER be converted (one per line).\n" +
                    "# Bu so'zlar hech qachon o'zgartirilmaydi.\n" +
                    "# These words will never be transliterated.\n\n",
                    Encoding.UTF8);
            }
        }
        catch
        {
            // Non-fatal — app works without user files
        }
    }

    // ── FileSystemWatcher Hot-Reloading ────────────────────────────────

    private FileSystemWatcher? InitializeWatcher()
    {
        try
        {
            EnsureUserFilesExist();

            var watcher = new FileSystemWatcher(AppDataDir)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                Filter = "*.txt",
                EnableRaisingEvents = true
            };

            watcher.Changed += OnUserFileChanged;
            watcher.Created += OnUserFileChanged;
            watcher.Renamed += OnUserFileChanged;

            return watcher;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Could not start FileSystemWatcher: {ex.Message}");
            return null;
        }
    }

    private void OnUserFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_isDisposed) return;

        string fileName = Path.GetFileName(e.FullPath);
        if (fileName.Equals("user_whitelist.txt", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("user_blacklist.txt", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                _debounceTimer?.Change(300, Timeout.Infinite);
            }
            catch (ObjectDisposedException)
            {
                // Timer disposed during shutdown — ignore safely
            }
        }
    }

    private void DebounceTimerCallback(object? state)
    {
        ReloadUserLists();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= OnUserFileChanged;
                _watcher.Created -= OnUserFileChanged;
                _watcher.Renamed -= OnUserFileChanged;
                _watcher.Dispose();
            }
        }
        catch { }

        try
        {
            _debounceTimer?.Dispose();
        }
        catch { }
    }
}
