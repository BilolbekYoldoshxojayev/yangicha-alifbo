// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.Tests — DiscriminationTests.cs
// Unit tests verifying that foreign words (English, programming keywords)
// are strictly rejected, while Uzbek words (1995 & 2024 reformed),
// agglutinative morphology inflections, consonant alternations,
// verb infinitives, and user overrides behave correctly.
// ──────────────────────────────────────────────────────────────────────

using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;

namespace YangiAlifbo.Tests;

[TestClass]
public class DiscriminationTests
{
    private static LanguageDetector _detector = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        _detector = new LanguageDetector();
    }

    // ── Lexicon size requirements (F5) ─────────────────────────────────

    [TestMethod]
    public void Lexicon_UzbekWordCount_MeetsExpandedTarget()
    {
        Assert.IsTrue(_detector.UzbekLexiconCount >= 80000,
            $"Uzbek lexicon should contain ~80,000+ words/stems. Actual: {_detector.UzbekLexiconCount}");
    }

    [TestMethod]
    public void Lexicon_EnglishRejectCount_MeetsTarget()
    {
        Assert.IsTrue(_detector.EnglishRejectCount >= 10000,
            $"English reject list should contain 10,000+ entries. Actual: {_detector.EnglishRejectCount}");
    }

    // ── English words with 'sh' must be REJECTED ───────────────────────

    [DataTestMethod]
    [DataRow("show")]
    [DataRow("sheet")]
    [DataRow("crash")]
    [DataRow("push")]
    [DataRow("shift")]
    [DataRow("shell")]
    [DataRow("short")]
    [DataRow("should")]
    [DataRow("shop")]
    [DataRow("ship")]
    [DataRow("shine")]
    [DataRow("shock")]
    [DataRow("shoe")]
    [DataRow("shoot")]
    [DataRow("shore")]
    [DataRow("shut")]
    [DataRow("shadow")]
    [DataRow("shake")]
    [DataRow("shame")]
    [DataRow("sharp")]
    [DataRow("finish")]
    [DataRow("publish")]
    [DataRow("english")]
    public void EnglishShWords_AreRejected(string word)
    {
        Assert.IsFalse(_detector.IsUzbek(word),
            $"Word '{word}' should NOT be recognized as Uzbek.");
    }

    // ── English words with 'ch' must be REJECTED ───────────────────────

    [DataTestMethod]
    [DataRow("child")]
    [DataRow("church")]
    [DataRow("change")]
    [DataRow("machine")]
    [DataRow("catch")]
    [DataRow("watch")]
    [DataRow("choose")]
    [DataRow("check")]
    [DataRow("chair")]
    [DataRow("chain")]
    [DataRow("channel")]
    [DataRow("chapter")]
    [DataRow("charge")]
    [DataRow("cheap")]
    [DataRow("cheese")]
    [DataRow("chemical")]
    [DataRow("chest")]
    [DataRow("touch")]
    [DataRow("teach")]
    [DataRow("reach")]
    [DataRow("search")]
    public void EnglishChWords_AreRejected(string word)
    {
        Assert.IsFalse(_detector.IsUzbek(word),
            $"Word '{word}' should NOT be recognized as Uzbek.");
    }

    // ── Programming keywords must be REJECTED ──────────────────────────

    [DataTestMethod]
    [DataRow("switch")]
    [DataRow("match")]
    [DataRow("fetch")]
    [DataRow("catch")]
    [DataRow("shell")]
    [DataRow("char")]
    [DataRow("echo")]
    [DataRow("chmod")]
    [DataRow("chown")]
    [DataRow("branch")]
    public void ProgrammingKeywords_AreRejected(string word)
    {
        Assert.IsFalse(_detector.IsUzbek(word),
            $"Programming keyword '{word}' should NOT be recognized as Uzbek.");
    }

    // ── Homograph collision handling (Strict mode) ─────────────────────

    [DataTestMethod]
    [DataRow("chin")]
    [DataRow("chop")]
    [DataRow("ship")]
    public void BareEnglishUzbekHomographs_AreRejectedByDefault(string word)
    {
        Assert.IsFalse(_detector.IsUzbek(word),
            $"Bare homograph '{word}' should be rejected in Strict mode to prevent corrupting English.");
    }

    [DataTestMethod]
    [DataRow("chindan")]
    [DataRow("chinakam")]
    [DataRow("chopmoq")]
    [DataRow("chopildi")]
    [DataRow("chopish")]
    public void InflectedHomographForms_AreAcceptedAsUzbek(string word)
    {
        Assert.IsTrue(_detector.IsUzbek(word),
            $"Inflected Uzbek form '{word}' should pass Tier 1 and be recognized as Uzbek.");
    }

    // ── Genuine Uzbek words MUST BE ACCEPTED (1995 orthography) ───────

    [DataTestMethod]
    [DataRow("shahar")]
    [DataRow("choyxona")]
    [DataRow("o'rtoq")]
    [DataRow("g'alaba")]
    [DataRow("shaxs")]
    [DataRow("chiqish")]
    [DataRow("o'zbek")]
    [DataRow("g'arb")]
    [DataRow("mashina")]
    [DataRow("yaxshi")]
    [DataRow("boshliq")]
    [DataRow("chegara")]
    [DataRow("do'st")]
    [DataRow("g'urur")]
    public void UzbekWords_AreAccepted(string word)
    {
        Assert.IsTrue(_detector.IsUzbek(word),
            $"Word '{word}' should be recognized as Uzbek.");
    }

    // ── Dual 2024 Reformed Orthography Indexing (F5) ───────────────────

    [DataTestMethod]
    [DataRow("şahar")]
    [DataRow("çoyxona")]
    [DataRow("örtoq")]
    [DataRow("ğalaba")]
    [DataRow("öqituvçi")]
    [DataRow("döst")]
    [DataRow("yaxşi")]
    public void DualOrthography_ReformedWords_AreAccepted(string word)
    {
        Assert.IsTrue(_detector.IsUzbek(word),
            $"Reformed 2024 word '{word}' should be recognized in dual-indexed lexicon.");
    }

    // ── Uzbek words with standard suffixes (Tier 3 morphology) ────────

    [DataTestMethod]
    [DataRow("shaharlar")]
    [DataRow("shaharda")]
    [DataRow("shahardan")]
    [DataRow("shaharga")]
    [DataRow("o'rtoqlar")]
    [DataRow("g'alabalar")]
    [DataRow("do'stlik")]
    public void UzbekInflectedWords_AreAccepted(string word)
    {
        Assert.IsTrue(_detector.IsUzbek(word),
            $"Inflected word '{word}' should be recognized via morphology/affixes.");
    }

    // ── Consonant Alternation: g' -> q and g -> k (F6) ─────────────────

    [DataTestMethod]
    [DataRow("o'rtog'i")]
    [DataRow("o'rtog'imiz")]
    [DataRow("o'rtog'im")]
    [DataRow("qishlog'i")]
    [DataRow("qishlog'ida")]
    [DataRow("qishlog'imizda")]
    [DataRow("yuragim")]
    [DataRow("yuragimiz")]
    [DataRow("eshigimiz")]
    public void ConsonantAlternation_FormsAreRecognized(string word)
    {
        Assert.IsTrue(_detector.IsUzbek(word),
            $"Word '{word}' with consonant alternation should be recognized as Uzbek.");
    }

    // ── Verb Infinitive Stem Resolution: stem + "moq" (F6) ─────────────

    [DataTestMethod]
    [DataRow("boshlayapti")]
    [DataRow("boshlagan")]
    [DataRow("boshlamoqchiman")]
    [DataRow("gapiryapti")]
    [DataRow("tushundi")]
    [DataRow("eshitmagan")]
    public void VerbInflections_AreRecognizedViaInfinitive(string word)
    {
        Assert.IsTrue(_detector.IsUzbek(word),
            $"Verbal inflection '{word}' should be resolved to its infinitive stem.");
    }

    // ── Deep suffix chaining (Tier 3) ──────────────────────────────────

    [DataTestMethod]
    [DataRow("shaharlarimizda")]
    [DataRow("o'qituvchilarimizdan")]
    [DataRow("do'stlikka")]
    [DataRow("yaxshiroq")]
    [DataRow("chiqayotgan")]
    public void DeepChainedSuffixes_AreRecognized(string word)
    {
        Assert.IsTrue(_detector.IsUzbek(word),
            $"Deeply chained word '{word}' should be recognized.");
    }

    // ── User Whitelist & Blacklist Overrides (F7) ───────────────────────

    [TestMethod]
    public void Whitelist_OverridesEnglishReject()
    {
        var white = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "show", "chin" };
        var customDetector = new LanguageDetector(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "show", "chin" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            userWhitelist: white);

        Assert.IsTrue(customDetector.IsUzbek("show"));
        Assert.IsTrue(customDetector.IsUzbek("chin"));
    }

    [TestMethod]
    public void Blacklist_OverridesUzbekLexicon()
    {
        var black = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shahar" };
        var customDetector = new LanguageDetector(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shahar" },
            userBlacklist: black);

        Assert.IsFalse(customDetector.IsUzbek("shahar"));
    }

    [TestMethod]
    public void Blacklist_TakesPrecedenceOverWhitelist()
    {
        var white = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shahar" };
        var black = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shahar" };
        var customDetector = new LanguageDetector(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shahar" },
            userWhitelist: white,
            userBlacklist: black);

        Assert.IsFalse(customDetector.IsUzbek("shahar"));
    }

    // ── ConversionMode & ShouldConvert ─────────────────────────────────

    [TestMethod]
    public void ShouldConvert_StrictVsAggressiveMode()
    {
        // Word not in dictionary, but has convertible pattern
        string unknown = "shabadaqwerty";

        Assert.IsFalse(_detector.ShouldConvert(unknown, ConversionMode.Strict),
            "Strict mode should reject unknown word without valid stem.");

        Assert.IsTrue(_detector.ShouldConvert(unknown, ConversionMode.Aggressive),
            "Aggressive mode should convert any word with convertible sequences unless blacklisted.");
    }

    [TestMethod]
    public void ShouldConvert_ReturnsFalse_ForNonConvertibleWords()
    {
        Assert.IsFalse(_detector.ShouldConvert("kitob", ConversionMode.Aggressive));
        Assert.IsFalse(_detector.ShouldConvert("qalam", ConversionMode.Strict));
    }

    [TestMethod]
    public void UserReported_RealWorldSentenceWords_AreRecognizedAndTransliterated()
    {
        // Sentence: "o'zbekcha gaplashsak ham bo'laveradi"
        Assert.IsTrue(_detector.IsUzbek("o'zbekcha"), "o'zbekcha must be recognized");
        Assert.AreEqual("özbekça", UzbekPhonetics.Convert("o'zbekcha"));

        Assert.IsTrue(_detector.IsUzbek("gaplashsak"), "gaplashsak must be recognized as Uzbek verb conditional");
        Assert.AreEqual("gaplaşsak", UzbekPhonetics.Convert("gaplashsak"));

        Assert.IsTrue(_detector.IsUzbek("bo'laveradi"), "bo'laveradi must be recognized as Uzbek continuative aspect verb");
        Assert.AreEqual("bölaveradi", UzbekPhonetics.Convert("bo'laveradi"));

        // Related conversational forms
        Assert.IsTrue(_detector.IsUzbek("bo'l"), "bare root bo'l must be recognized");
        Assert.AreEqual("böl", UzbekPhonetics.Convert("bo'l"));

        Assert.IsTrue(_detector.IsUzbek("bo'laver"), "bo'laver must be recognized");
        Assert.AreEqual("bölaver", UzbekPhonetics.Convert("bo'laver"));

        Assert.IsTrue(_detector.IsUzbek("gaplashaylik"), "gaplashaylik must be recognized");
        Assert.AreEqual("gaplaşaylik", UzbekPhonetics.Convert("gaplashaylik"));

        Assert.IsTrue(_detector.IsUzbek("gaplashamiz"), "gaplashamiz must be recognized");
        Assert.AreEqual("gaplaşamiz", UzbekPhonetics.Convert("gaplashamiz"));
    }

    [TestMethod]
    public void ConversationalUzbekWords_AreAllRecognizedAndTransliterated()
    {
        var words = new (string input, string expected)[]
        {
            ("o'zim", "özim"),
            ("o'zingiz", "özingiz"),
            ("bo'ldi", "böldi"),
            ("bo'lmadi", "bölmadi"),
            ("bo'lsa", "bölsa"),
            ("bo'lib", "bölib"),
            ("bo'lgan", "bölgan"),
            ("bo'lmoqchi", "bölmoqçi"),
            ("gaplashdik", "gaplaşdik"),
            ("gaplashyapmiz", "gaplaşyapmiz"),
            ("gaplashamiz", "gaplaşamiz"),
            ("ishlayapman", "işlayapman"),
            ("ishlayapsiz", "işlayapsiz"),
            ("ishlash", "işlaş"),
            ("yaxshi", "yaxşi"),
            ("yaxshimisiz", "yaxşimisiz"),
            ("chiroyli", "çiroyli"),
            ("choyxona", "çoyxona"),
            ("shoshiling", "şoşiling"),
            ("og'ir", "oğir"),
            ("tog'da", "toğda"),
            ("to'g'ri", "töğri"),
            ("o'quvchi", "öquvçi"),
            ("o'qituvchi", "öqituvçi"),
            ("ko'rishguncha", "körişgunça"),
            ("qo'shiqchi", "qöşiqçi"),
            ("qulog'im", "quloğim"),
            ("tushundim", "tuşundim"),
            ("tushunaman", "tuşunaman"),
            ("kechirasiz", "keçirasiz"),
            ("kecha", "keça"),
            ("o'ylab", "öylab"),
            ("o'ylayman", "öylayman"),
            ("chiqamiz", "çiqamiz"),
            ("uchrashuv", "uçraşuv"),
            ("boshlaymiz", "boşlaymiz"),
            ("eshitdim", "eşitdim"),
            ("eshityapsiz", "eşityapsiz"),
            ("shunaqa", "şunaqa"),
            ("shunday", "şunday"),
            ("chunki", "çunki")
        };

        foreach (var (input, expected) in words)
        {
            Assert.IsTrue(_detector.IsUzbek(input), $"Word '{input}' must be recognized as Uzbek.");
            Assert.AreEqual(expected, UzbekPhonetics.Convert(input), $"Transliteration of '{input}' should be '{expected}'.");
        }
    }
}
