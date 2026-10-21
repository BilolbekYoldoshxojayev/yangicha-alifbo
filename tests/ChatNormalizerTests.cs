// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.Tests — ChatNormalizerTests.cs
// Comprehensive unit tests for Telegram chat slang, Russian 'w' substitutions,
// vowel-dropped contractions, unaccented 'o' roots, and colloquial words.
// ──────────────────────────────────────────────────────────────────────

using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;

namespace YangiAlifbo.Tests;

[TestClass]
public class ChatNormalizerTests
{
    private static LanguageDetector _detector = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        _detector = new LanguageDetector();
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _detector.Dispose();
    }

    [DataTestMethod]
    // ── Vowel-dropped contractions & unaccented roots ──────────────
    [DataRow("korishamz", "körişamiz")]
    [DataRow("Korishamz", "Körişamiz")]
    [DataRow("KORISHAMZ", "KÖRIŞAMIZ")]
    [DataRow("gaplashamz", "gaplaşamiz")]
    [DataRow("kirasz", "kirasiz")]
    [DataRow("bilmadm", "bilmadim")]
    [DataRow("yozdm", "yozdim")]
    [DataRow("tushundm", "tuşundim")]
    [DataRow("kordm", "kördim")]
    [DataRow("yaxshimisz", "yaxşimisiz")]

    // ── Unaccented 'o' high-frequency roots ────────────────────────
    [DataRow("boldi", "böldi")]
    [DataRow("boladi", "böladi")]
    [DataRow("bolsa", "bölsa")]
    [DataRow("bolib", "bölib")]
    [DataRow("togri", "töğri")]
    [DataRow("ozim", "özim")]
    [DataRow("zor", "zör")]
    [DataRow("kop", "köp")]

    // ── Russian 'w' for 'sh' in Uzbek texting ───────────────────────
    [DataRow("yaxwi", "yaxşi")]
    [DataRow("Yaxwi", "Yaxşi")]
    [DataRow("wunaqa", "şunaqa")]
    [DataRow("wunga", "şunga")]
    [DataRow("wunday", "şunday")]
    [DataRow("wuni", "şuni")]
    [DataRow("tuwundm", "tuşundim")]
    [DataRow("boliwi", "bölişi")]
    [DataRow("bowlanadi", "boşlanadi")]
    [DataRow("owa", "öşa")]
    [DataRow("uwa", "öşa")]

    // ── Colloquial stems & spoken verb forms ───────────────────────
    [DataRow("ashi", "aşi")]
    [DataRow("ashini", "aşini")]
    [DataRow("osha", "öşa")]
    [DataRow("oshani", "öşani")]
    [DataRow("bopti", "böpti")]
    [DataRow("bovotti", "bövotti")]
    [DataRow("chotki", "çotki")]
    [DataRow("gaplashvotti", "gaplaşvotti")]
    [DataRow("chiqvotti", "çiqvotti")]
    public void TestChatConversion(string input, string expected)
    {
        // 1. Should not be rejected as foreign
        Assert.IsFalse(_detector.IsForeignOrReject(input), $"Word '{input}' should not be rejected as foreign.");

        // 2. Normalize chat input
        string normalized = ChatNormalizer.Normalize(input);

        // 3. Convert to reformed Latin
        string converted;
        if (UzbekPhonetics.HasConvertiblePatterns(normalized))
        {
            converted = UzbekPhonetics.Convert(normalized);
        }
        else
        {
            converted = normalized;
        }

        Assert.AreEqual(expected, converted);
    }

    [DataTestMethod]
    // ── English words that must NEVER be touched ────────────────────
    [DataRow("bold")]
    [DataRow("show")]
    [DataRow("we")]
    [DataRow("when")]
    [DataRow("what")]
    [DataRow("where")]
    [DataRow("work")]
    [DataRow("with")]
    [DataRow("would")]
    [DataRow("will")]
    [DataRow("world")]
    [DataRow("cost")]
    [DataRow("soft")]
    [DataRow("sport")]
    [DataRow("post")]
    [DataRow("camp")]
    [DataRow("voice")]
    [DataRow("course")]
    [DataRow("candidate")]
    [DataRow("test")]
    [DataRow("idk")]
    public void TestEnglishWordsAreRejected(string englishWord)
    {
        Assert.IsTrue(_detector.IsForeignOrReject(englishWord),
            $"English word '{englishWord}' MUST be detected as foreign/reject.");
    }

    [TestMethod]
    public void TestSpeedOptimization_1000Iterations_Under20Milliseconds()
    {
        string[] sampleWords = new[]
        {
            "korishamz", "boldi", "yaxwi", "boladi", "wunaqa",
            "tushundm", "gaplashamz", "kirasz", "bopti", "bovotti"
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            string word = sampleWords[i % sampleWords.Length];
            string norm = ChatNormalizer.Normalize(word);
            string conv = UzbekPhonetics.Convert(norm);
        }
        sw.Stop();

        // 1000 iterations must take well under 20ms (< 0.02 ms per word)
        Assert.IsTrue(sw.ElapsedMilliseconds < 25,
            $"Expected 1,000 normalizations to take < 25ms, took {sw.ElapsedMilliseconds}ms ({sw.Elapsed.TotalMicroseconds / 1000:F2} µs/op).");
    }

    [TestMethod]
    public void TestDashboardForm_InstantiationAndLivePreview()
    {
        using var hook = new YangiAlifbo.Win32.KeyboardHook(_detector, new YangiAlifbo.Win32.FocusTracker());
        using var form = new YangiAlifbo.UI.DashboardForm(hook, _detector, _ => { }, () => true);

        Assert.IsNotNull(form);
        Assert.AreEqual("YangiAlifbo — Boshqaruv Paneli (Control Center)", form.Text);
    }
}
