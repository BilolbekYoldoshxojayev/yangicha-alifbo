// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.Tests — EmpiricalChallengeM2Tests.cs
// Adversarial Empirical Challenge Suite for Milestone 2 (M2).
// Focus areas:
// 1. Complex agglutinative chains: plurals + possessives + cases + relative
//    (shaharlarimizdagilardan, o'rtoqlarimiznikida, qishlog'imizdagilar, etc.)
// 2. Verbal forms and compound inflections:
//    (boshlamoqchiman, gapiryapti, eshitmaganlar, qilayotgandik, etc.)
// 3. Consonant alternations (q <-> g', k <-> g, reformed ğ <-> q).
// 4. Homographs discrimination: bare words (chin, chop, ship) rejected in
//    Strict mode; inflected forms (chindan, chopmoq, etc.) accepted.
// 5. Negative reject safety: 50+ programming keywords, CLI tools, technical
//    terms, and English words with sh/ch.
// 6. Concurrency and ultra-low latency verification (< 1ms per lookup).
// ──────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;

namespace YangiAlifbo.Tests;

[TestClass]
public class EmpiricalChallengeM2Tests
{
    private static LanguageDetector _detector = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        _detector = new LanguageDetector();
    }

    // ══════════════════════════════════════════════════════════════════
    // 1. Complex Agglutinative Chains
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    [DataRow("shaharlarimizdagilardan")]
    [DataRow("shaharlarimizdagi")]
    [DataRow("shaharlarimizdan")]
    [DataRow("qishlog'imizdagilar")]
    [DataRow("maktabimizdagilarga")]
    [DataRow("o'quvchilarimizdan")]
    [DataRow("do'stlarimizga")]
    [DataRow("do'stlarimizda")]
    [DataRow("do'stlarimizdan")]
    [DataRow("o'rtoqlarimizdan")]
    public void AgglutinativeChains_PluralPossessiveCase_AreRecognized(string word)
    {
        Assert.IsTrue(_detector.IsUzbek(word),
            $"Complex agglutinative chain '{word}' should be recognized as Uzbek.");
    }

    [TestMethod]
    [DataRow("o'rtoqlarimiznikida")]
    [DataRow("o'rtoqlarimiznikiga")]
    [DataRow("o'rtoqlarimiznikidan")]
    [DataRow("o'rtoqlarimizniki")]
    [DataRow("do'stlarimiznikida")]
    [DataRow("do'stlarimiznikidan")]
    public void AgglutinativeChains_RelativePossessiveNiki_EmpiricalTest(string word)
    {
        bool recognized = _detector.IsUzbek(word);
        Assert.IsTrue(recognized,
            $"Agglutinative relative possessive chain '{word}' should be recognized as Uzbek.");
    }

    [TestMethod]
    [DataRow("shahargacha")]
    [DataRow("qishloqqacha")]
    [DataRow("maktabgacha")]
    public void AgglutinativeChains_TerminativeGacha_EmpiricalTest(string word)
    {
        bool recognized = _detector.IsUzbek(word);
        Assert.IsTrue(recognized,
            $"Terminative suffix form '{word}' should be recognized as Uzbek.");
    }

    [TestMethod]
    [DataRow("shaharday")]
    [DataRow("o'rtoqday")]
    public void AgglutinativeChains_SimilativeDay_EmpiricalTest(string word)
    {
        bool recognized = _detector.IsUzbek(word);
        Assert.IsTrue(recognized,
            $"Similative suffix form '{word}' should be recognized as Uzbek.");
    }

    [TestMethod]
    [DataRow("shaharmi")]
    [DataRow("o'rtoqmi")]
    public void AgglutinativeChains_QuestionParticleMi_EmpiricalTest(string word)
    {
        bool recognized = _detector.IsUzbek(word);
        Assert.IsTrue(recognized,
            $"Question particle form '{word}' should be recognized as Uzbek.");
    }

    // ══════════════════════════════════════════════════════════════════
    // 2. Verbal Forms & Compound Inflections
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    [DataRow("boshlamoqchiman")]
    [DataRow("gapiryapti")]
    [DataRow("eshitmaganlar")]
    [DataRow("qilayotgandik")]
    [DataRow("ishlayotgan")]
    [DataRow("boshlaganlar")]
    [DataRow("tushunmaganlar")]
    [DataRow("gapirmoqchimiz")]
    [DataRow("ko'rmoqchiman")]
    public void VerbalForms_CompoundInflections_AreRecognized(string word)
    {
        Assert.IsTrue(_detector.IsUzbek(word),
            $"Verbal inflection '{word}' should be recognized as Uzbek.");
    }

    // ══════════════════════════════════════════════════════════════════
    // 3. Consonant Alternations (q <-> g', k <-> g, reformed ğ <-> q)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    [DataRow("o'rtog'i")]
    [DataRow("o'rtog'imiz")]
    [DataRow("o'rtog'im")]
    [DataRow("o'rtog'ing")]
    [DataRow("o'rtog'idan")]
    [DataRow("qishlog'i")]
    [DataRow("qishlog'ida")]
    [DataRow("qishlog'imiz")]
    [DataRow("qishlog'imizda")]
    [DataRow("qishlog'imizdan")]
    [DataRow("yuragim")]
    [DataRow("yuragimiz")]
    [DataRow("yuragida")]
    [DataRow("eshigimiz")]
    [DataRow("eshigi")]
    [DataRow("eshigidan")]
    public void ConsonantAlternations_1995Orthography_AreRecognized(string word)
    {
        Assert.IsTrue(_detector.IsUzbek(word),
            $"Consonant alternation word '{word}' should be recognized as Uzbek.");
    }

    [TestMethod]
    [DataRow("örtoği")]
    [DataRow("örtoğimiz")]
    [DataRow("örtoğim")]
    [DataRow("qişloği")]
    [DataRow("qişloğida")]
    [DataRow("qişloğimizda")]
    [DataRow("eşigimiz")]
    [DataRow("eşigi")]
    [DataRow("kökragi")]
    [DataRow("yuragim")] // Uzbek 2024 reform keeps 'u' as 'u' (not 'ü')
    public void ConsonantAlternations_2024ReformedOrthography_AreRecognized(string word)
    {
        Assert.IsTrue(_detector.IsUzbek(word),
            $"Reformed consonant alternation word '{word}' should be recognized as Uzbek.");
    }

    // ══════════════════════════════════════════════════════════════════
    // 4. Homograph Discrimination (Strict vs Aggressive Mode)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    [DataRow("chin")]
    [DataRow("chop")]
    [DataRow("ship")]
    public void Homographs_BareWords_RejectedInStrictMode(string word)
    {
        Assert.IsFalse(_detector.IsUzbek(word),
            $"Bare homograph '{word}' MUST be rejected in Strict mode to prevent corrupting English.");
        Assert.IsFalse(_detector.ShouldConvert(word, ConversionMode.Strict),
            $"ShouldConvert for bare homograph '{word}' MUST return false in Strict mode.");
    }

    [TestMethod]
    [DataRow("chin")]
    [DataRow("chop")]
    [DataRow("ship")]
    public void Homographs_BareWords_AllowedInAggressiveMode(string word)
    {
        Assert.IsTrue(_detector.ShouldConvert(word, ConversionMode.Aggressive),
            $"Aggressive mode should allow converting '{word}'.");
    }

    [TestMethod]
    [DataRow("chindan")]
    [DataRow("chinakam")]
    [DataRow("chindagi")]
    [DataRow("chopmoq")]
    [DataRow("chopildi")]
    [DataRow("chopish")]
    [DataRow("chopsa")]
    [DataRow("chopgan")]
    public void Homographs_InflectedForms_AcceptedInStrictMode(string word)
    {
        Assert.IsTrue(_detector.IsUzbek(word),
            $"Inflected homograph form '{word}' MUST be recognized as genuine Uzbek.");
        Assert.IsTrue(_detector.ShouldConvert(word, ConversionMode.Strict),
            $"ShouldConvert for inflected homograph form '{word}' MUST return true.");
    }

    // ══════════════════════════════════════════════════════════════════
    // 5. Negative Reject Safety: 50+ Programming Keywords & Tech Terms
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void NegativeSafety_ProgrammingKeywordsAndTechTerms_AreRejected()
    {
        string[] techTerms =
        [
            "switch", "match", "fetch", "catch", "shell", "shift", "char", "echo",
            "chmod", "chown", "branch", "checkout", "patch", "push", "watch", "crash",
            "short", "byte", "shared", "schema", "scheduler", "dispatch", "cache", "channel",
            "checksum", "chunk", "hash", "flush", "trash", "shutdown", "stylesheet", "shadow",
            "show", "sheet", "benchmark", "blockchain", "batch", "attach", "detach", "watcher",
            "launch", "chart", "chrome", "chromium", "machine", "method", "mesh", "search",
            "child", "church", "change", "reach", "teach", "touch", "speech", "stretch",
            "which", "rich", "pitch", "beach", "bench", "coach", "shark", "sharp",
            "shelf", "shield", "shine", "shirt", "shock", "shoot", "shoe", "shoulder"
        ];

        Assert.IsTrue(techTerms.Length >= 50, $"Expected at least 50 tech terms, found {techTerms.Length}");

        var failedRejects = new List<string>();

        foreach (string term in techTerms)
        {
            if (_detector.IsUzbek(term) || _detector.ShouldConvert(term, ConversionMode.Strict))
            {
                failedRejects.Add(term);
            }
        }

        Assert.AreEqual(0, failedRejects.Count,
            $"The following {failedRejects.Count} tech/English terms were falsely recognized as Uzbek: {string.Join(", ", failedRejects)}");
    }

    [TestMethod]
    [DataRow("SWITCH")]
    [DataRow("Switch")]
    [DataRow("MATCH")]
    [DataRow("Match")]
    [DataRow("FETCH")]
    [DataRow("Fetch")]
    [DataRow("CATCH")]
    [DataRow("Catch")]
    [DataRow("SHELL")]
    [DataRow("Shell")]
    [DataRow("MACHINE")]
    [DataRow("Machine")]
    public void NegativeSafety_CasedKeywords_AreRejected(string word)
    {
        Assert.IsFalse(_detector.IsUzbek(word),
            $"Cased programming keyword '{word}' should be rejected.");
        Assert.IsFalse(_detector.ShouldConvert(word, ConversionMode.Strict),
            $"ShouldConvert for cased keyword '{word}' should return false.");
    }

    // ══════════════════════════════════════════════════════════════════
    // 6. Concurrency and Thread Safety
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Concurrency_HighThroughputParallelAccess_IsThreadSafe()
    {
        string[] testWords =
        [
            "shahar", "shaharlar", "shaharlarimizdagilardan",
            "switch", "match", "fetch", "catch", "shell",
            "chin", "chindan", "chop", "chopmoq",
            "o'rtoq", "o'rtog'i", "o'rtog'imiz",
            "boshlamoqchiman", "gapiryapti", "eshitmaganlar"
        ];

        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, 5000, new ParallelOptions { MaxDegreeOfParallelism = 32 }, i =>
        {
            try
            {
                string word = testWords[i % testWords.Length];
                bool isUz = _detector.IsUzbek(word);
                bool scStrict = _detector.ShouldConvert(word, ConversionMode.Strict);
                bool scAggr = _detector.ShouldConvert(word, ConversionMode.Aggressive);

                if (word == "switch")
                {
                    if (isUz || scStrict)
                        exceptions.Add(new Exception($"Thread safety breach: 'switch' passed as Uzbek at iter {i}"));
                }
                else if (word == "shahar")
                {
                    if (!isUz || !scStrict)
                        exceptions.Add(new Exception($"Thread safety breach: 'shahar' failed at iter {i}"));
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.AreEqual(0, exceptions.Count,
            $"Concurrent evaluation produced {exceptions.Count} failures: {exceptions.FirstOrDefault()?.Message}");
    }

    // ══════════════════════════════════════════════════════════════════
    // 7. Latency and Throughput Verification (< 1ms target)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Performance_DiscriminationLatency_IsSubMillisecond()
    {
        string[] sampleWords =
        [
            "shaharlarimizdagilardan", "switch", "o'rtog'imizdan", "machine",
            "chindan", "chin", "boshlamoqchiman", "catch", "qishlog'imizda"
        ];

        // Warmup
        for (int i = 0; i < 500; i++)
        {
            _detector.ShouldConvert(sampleWords[i % sampleWords.Length], ConversionMode.Strict);
        }

        const int iterations = 10000;
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            _detector.ShouldConvert(sampleWords[i % sampleWords.Length], ConversionMode.Strict);
        }

        sw.Stop();

        double totalMs = sw.Elapsed.TotalMilliseconds;
        double avgUs = (totalMs / iterations) * 1000.0; // microseconds

        Assert.IsTrue(avgUs < 1000.0,
            $"Average latency per word discrimination was {avgUs:F2} µs, exceeding 1ms requirement!");
    }
}
