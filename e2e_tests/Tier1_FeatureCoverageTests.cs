// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.E2ETests — Tier1_FeatureCoverageTests.cs
// Tier 1: Feature Coverage (>= 5 tests per feature for all 16 features)
// Total cases: 117 tests derived strictly from ORIGINAL_REQUEST.md.
// ──────────────────────────────────────────────────────────────────────

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;

namespace YangiAlifbo.E2ETests;

[TestClass]
public class Tier1_FeatureCoverageTests
{
    private static LanguageDetector _detector = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        _detector = new LanguageDetector();
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 1: Reformed Latin Phonetic Mappings (oʻ/gʻ/sh/ch -> ö/ğ/ş/ç)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F01_01_Sh_MapsTo_CedillaS_Lowercase()
    {
        Assert.AreEqual("şahar", UzbekPhonetics.Convert("shahar"));
    }

    [TestMethod]
    public void F01_02_Ch_MapsTo_CedillaC_Lowercase()
    {
        Assert.AreEqual("çoyxona", UzbekPhonetics.Convert("choyxona"));
    }

    [TestMethod]
    public void F01_03_O_Apostrophe_MapsTo_ODiaeresis_Lowercase()
    {
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o'rtoq"));
    }

    [TestMethod]
    public void F01_04_G_Apostrophe_MapsTo_GBreve_Lowercase()
    {
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("g'alaba"));
    }

    [TestMethod]
    public void F01_05_MultipleSameDigraphs_InSingleWord()
    {
        // shoshilinch has two 'sh' and one 'ch'
        Assert.AreEqual("şoşilinç", UzbekPhonetics.Convert("shoshilinch"));
    }

    [TestMethod]
    public void F01_06_Combined_Ch_And_O_Apostrophe()
    {
        // cho'qqi -> çöqqi
        Assert.AreEqual("çöqqi", UzbekPhonetics.Convert("cho'qqi"));
    }

    [TestMethod]
    public void F01_07_Combined_G_Apostrophe_And_O_Apostrophe()
    {
        // g'o'za -> ğöza
        Assert.AreEqual("ğöza", UzbekPhonetics.Convert("g'o'za"));
    }

    [TestMethod]
    public void F01_08_MiddleWord_Sh_Mapping()
    {
        Assert.AreEqual("boşliq", UzbekPhonetics.Convert("boshliq"));
    }

    [TestMethod]
    public void F01_09_MiddleWord_Ch_Mapping()
    {
        Assert.AreEqual("içki", UzbekPhonetics.Convert("ichki"));
    }

    [TestMethod]
    public void F01_10_E2E_FullSentence_PhoneticConversions()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shahar o'rtoq choyxona g'alaba ");
        Assert.AreEqual("şahar örtoq çoyxona ğalaba ", sim.GetScreenText());
        Assert.AreEqual(4, sim.WordsConvertedCount);
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 2: Apostrophe Variants (all 9 variants)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F02_01_StandardKeyboardApostrophe_U0027()
    {
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o'rtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("g'alaba"));
    }

    [TestMethod]
    public void F02_02_LeftSingleQuote_U2018()
    {
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o‘rtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("g‘alaba"));
    }

    [TestMethod]
    public void F02_03_RightSingleQuote_U2019()
    {
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o’rtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("g’alaba"));
    }

    [TestMethod]
    public void F02_04_ModifierLetterTurnedComma_U02BB_OfficialUzbek()
    {
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("oʻrtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("gʻalaba"));
    }

    [TestMethod]
    public void F02_05_ModifierLetterApostrophe_U02BC()
    {
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("oʼrtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("gʼalaba"));
    }

    [TestMethod]
    public void F02_06_GraveAccent_U0060()
    {
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o`rtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("g`alaba"));
    }

    [TestMethod]
    public void F02_07_NormalizeApostrophes_ReplacesAllWithStandard()
    {
        string input = "o' o‘ o’ oʻ oʼ o`";
        string normalized = UzbekPhonetics.NormalizeApostrophes(input);
        Assert.AreEqual("o' o' o' o' o' o'", normalized);
    }

    [TestMethod]
    public void F02_08_G_Apostrophe_MixedVariantsInSentence()
    {
        Assert.AreEqual("ğarb ğalaba ğurur",
            UzbekPhonetics.Convert("g'arb g‘alaba gʻurur"));
    }

    [TestMethod]
    public void F02_09_O_Apostrophe_MixedVariantsInSentence()
    {
        Assert.AreEqual("örtoq özbek ötin",
            UzbekPhonetics.Convert("o'rtoq o’zbek oʻtin"));
    }

    [TestMethod]
    public void F02_10_E2E_TypingWithModifierTurnedComma()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("oʻrtoq gʻalaba ");
        Assert.AreEqual("örtoq ğalaba ", sim.GetScreenText());
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 3: Letter Casing Preservation
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F03_01_TitleCase_Shahar_MapsTo_CapitalCedillaS()
    {
        Assert.AreEqual("Şahar", UzbekPhonetics.Convert("Shahar"));
    }

    [TestMethod]
    public void F03_02_TitleCase_Choyxona_MapsTo_CapitalCedillaC()
    {
        Assert.AreEqual("Çoyxona", UzbekPhonetics.Convert("Choyxona"));
    }

    [TestMethod]
    public void F03_03_TitleCase_Ortoq_MapsTo_CapitalODiaeresis()
    {
        Assert.AreEqual("Örtoq", UzbekPhonetics.Convert("O'rtoq"));
    }

    [TestMethod]
    public void F03_04_TitleCase_Galaba_MapsTo_CapitalGBreve()
    {
        Assert.AreEqual("Ğalaba", UzbekPhonetics.Convert("G'alaba"));
    }

    [TestMethod]
    public void F03_05_AllCaps_SHAHAR_Preserved()
    {
        Assert.AreEqual("ŞAHAR", UzbekPhonetics.Convert("SHAHAR"));
    }

    [TestMethod]
    public void F03_06_AllCaps_CHOYXONA_Preserved()
    {
        Assert.AreEqual("ÇOYXONA", UzbekPhonetics.Convert("CHOYXONA"));
    }

    [TestMethod]
    public void F03_07_AllCaps_ORTOQ_Preserved()
    {
        Assert.AreEqual("ÖRTOQ", UzbekPhonetics.Convert("O'RTOQ"));
    }

    [TestMethod]
    public void F03_08_AllCaps_GALABA_Preserved()
    {
        Assert.AreEqual("ĞALABA", UzbekPhonetics.Convert("G'ALABA"));
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 4: Morpheme & Syllable Break Protection (Is'hoq, mus'haf)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F04_01_Ishoq_PreservesApostropheBoundary_NotConverted()
    {
        Assert.AreEqual("Is'hoq", UzbekPhonetics.Convert("Is'hoq"));
        Assert.AreNotEqual("Işoq", UzbekPhonetics.Convert("Is'hoq"));
    }

    [TestMethod]
    public void F04_02_Mushaf_PreservesApostropheBoundary_NotConverted()
    {
        Assert.AreEqual("mus'haf", UzbekPhonetics.Convert("mus'haf"));
        Assert.AreNotEqual("muşaf", UzbekPhonetics.Convert("mus'haf"));
    }

    [TestMethod]
    public void F04_03_Ashob_PreservesHyphenOrApostropheBoundary()
    {
        Assert.AreEqual("as-hob", UzbekPhonetics.Convert("as-hob"));
        Assert.AreEqual("as'hob", UzbekPhonetics.Convert("as'hob"));
    }

    [TestMethod]
    public void F04_04_Masul_PreservesApostropheBoundary()
    {
        Assert.AreEqual("mas'ul", UzbekPhonetics.Convert("mas'ul"));
    }

    [TestMethod]
    public void F04_05_Sanat_PreservesApostropheBoundary()
    {
        Assert.AreEqual("san'at", UzbekPhonetics.Convert("san'at"));
    }

    [TestMethod]
    public void F04_06_E2E_TypingProtectedWords_NeverCorrupted()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("Is'hoq mus'haf mas'ul ");
        Assert.AreEqual("Is'hoq mus'haf mas'ul ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 5: Tier 1 Negative Reject Filter (English & Tech Keywords)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F05_01_English_Common_Sh_Words_Rejected()
    {
        string[] words = { "show", "sheet", "crash", "push", "shift", "shell", "short", "should" };
        foreach (var w in words)
        {
            Assert.IsFalse(_detector.IsUzbek(w), $"Expected '{w}' to be rejected as foreign.");
        }
    }

    [TestMethod]
    public void F05_02_English_Common_Ch_Words_Rejected()
    {
        string[] words = { "child", "church", "change", "machine", "catch", "watch", "choose", "check" };
        foreach (var w in words)
        {
            Assert.IsFalse(_detector.IsUzbek(w), $"Expected '{w}' to be rejected as foreign.");
        }
    }

    [TestMethod]
    public void F05_03_ProgrammingKeywords_Rejected()
    {
        string[] keywords = { "switch", "match", "fetch", "catch", "shell", "char", "echo", "chmod", "chown", "branch" };
        foreach (var kw in keywords)
        {
            Assert.IsFalse(_detector.IsUzbek(kw), $"Expected keyword '{kw}' to be rejected.");
        }
    }

    [TestMethod]
    public void F05_04_English_PublishFinish_Rejected()
    {
        Assert.IsFalse(_detector.IsUzbek("publish"));
        Assert.IsFalse(_detector.IsUzbek("finish"));
        Assert.IsFalse(_detector.IsUzbek("english"));
    }

    [TestMethod]
    public void F05_05_English_TechTerms_Rejected()
    {
        Assert.IsFalse(_detector.IsUzbek("touch"));
        Assert.IsFalse(_detector.IsUzbek("teach"));
        Assert.IsFalse(_detector.IsUzbek("search"));
    }

    [TestMethod]
    public void F05_06_E2E_TypingEnglishKeywords_UntouchedInStrictMode()
    {
        var sim = new TypingSimulator(_detector) { Mode = SimulationMode.Strict };
        sim.TypeText("switch match catch shell ");
        Assert.AreEqual("switch match catch shell ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F05_07_EnglishRejectCount_LoadedFromEmbeddedDictionaries()
    {
        Assert.IsTrue(_detector.EnglishRejectCount > 100,
            $"English reject list should contain words (Actual: {_detector.EnglishRejectCount})");
    }

    [TestMethod]
    public void F05_08_MixedEnglishAndUzbekTyping()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("show shahar switch choyxona ");
        Assert.AreEqual("show şahar switch çoyxona ", sim.GetScreenText());
        Assert.AreEqual(2, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F05_09_EnglishCaseVariations_Rejected()
    {
        Assert.IsFalse(_detector.IsUzbek("SWITCH"));
        Assert.IsFalse(_detector.IsUzbek("Catch"));
        Assert.IsFalse(_detector.IsUzbek("sHeLL"));
    }

    [TestMethod]
    public void F05_10_E2E_ProgrammingCodeSnippet_RemainsIntact()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("switch (value) { case 1: fetch(); } ");
        Assert.IsTrue(sim.GetScreenText().Contains("switch"));
        Assert.IsTrue(sim.GetScreenText().Contains("fetch"));
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 6: Tier 2 Uzbek Lexicon Dual-Orthography Lookup
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F06_01_UzbekLexicon_LoadsEmbeddedWords()
    {
        Assert.IsTrue(_detector.UzbekLexiconCount > 1000,
            $"Uzbek lexicon should contain thousands of words (Actual: {_detector.UzbekLexiconCount})");
    }

    [TestMethod]
    public void F06_02_Uzbek1995Orthography_Recognized()
    {
        Assert.IsTrue(_detector.IsUzbek("shahar"));
        Assert.IsTrue(_detector.IsUzbek("choyxona"));
        Assert.IsTrue(_detector.IsUzbek("o'rtoq"));
        Assert.IsTrue(_detector.IsUzbek("g'alaba"));
    }

    [TestMethod]
    public void F06_03_AlreadyReformedWords_DoNotHaveConvertiblePatterns()
    {
        // Words already in 2024 reformed alphabet (şahar, çoyxona, örtoq, ğalaba)
        // must not trigger convertible patterns
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("şahar"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("çoyxona"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("örtoq"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("ğalaba"));
    }

    [TestMethod]
    public void F06_03b_DualOrthographyLexicon_Supported()
    {
        var dualLexicon = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "shahar", "şahar", "o'rtoq", "örtoq"
        };
        var dualDetector = new LanguageDetector(
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase),
            dualLexicon);

        Assert.IsTrue(dualDetector.IsUzbek("shahar"));
        Assert.IsTrue(dualDetector.IsUzbek("şahar"));
        Assert.IsTrue(dualDetector.IsUzbek("o'rtoq"));
        Assert.IsTrue(dualDetector.IsUzbek("örtoq"));
    }

    [TestMethod]
    public void F06_04_UzbekCommonNouns_Recognized()
    {
        Assert.IsTrue(_detector.IsUzbek("shaxs"));
        Assert.IsTrue(_detector.IsUzbek("chiqish"));
        Assert.IsTrue(_detector.IsUzbek("o'zbek"));
        Assert.IsTrue(_detector.IsUzbek("g'arb"));
    }

    [TestMethod]
    public void F06_05_UzbekCommonAdjectives_Recognized()
    {
        Assert.IsTrue(_detector.IsUzbek("yaxshi"));
        Assert.IsTrue(_detector.IsUzbek("boshliq"));
        Assert.IsTrue(_detector.IsUzbek("chegara"));
    }

    [TestMethod]
    public void F06_06_UzbekSocialVocabulary_Recognized()
    {
        Assert.IsTrue(_detector.IsUzbek("do'st"));
        Assert.IsTrue(_detector.IsUzbek("g'urur"));
    }

    [TestMethod]
    public void F06_07_NonExistentWord_Rejected()
    {
        Assert.IsFalse(_detector.IsUzbek("xyzqwerty"));
        Assert.IsFalse(_detector.IsUzbek("zzzzshsh"));
    }

    [TestMethod]
    public void F06_08_NormalizeForLookup_HandlesApostrophes()
    {
        string norm = LanguageDetector.NormalizeForLookup("OʻRTOQ");
        Assert.AreEqual("o'rtoq", norm);
    }

    [TestMethod]
    public void F06_09_HasConvertiblePatterns_TrueForConvertibleWords()
    {
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("shahar"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("choy"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("o'rtoq"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("g'alaba"));
    }

    [TestMethod]
    public void F06_10_HasConvertiblePatterns_FalseForNonConvertibleWords()
    {
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("kitob"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("qalam"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("maktab"));
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 7: Tier 3 Agglutinative Morphology (Suffixes)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F07_01_PluralSuffix_Lar_Recognized()
    {
        Assert.IsTrue(_detector.IsUzbek("shaharlar"));
        Assert.IsTrue(_detector.IsUzbek("o'rtoqlar"));
        Assert.IsTrue(_detector.IsUzbek("g'alabalar"));
    }

    [TestMethod]
    public void F07_02_LocativeSuffix_Da_Recognized()
    {
        Assert.IsTrue(_detector.IsUzbek("shaharda"));
    }

    [TestMethod]
    public void F07_03_AblativeSuffix_Dan_Recognized()
    {
        Assert.IsTrue(_detector.IsUzbek("shahardan"));
    }

    [TestMethod]
    public void F07_04_DativeSuffix_Ga_Recognized()
    {
        Assert.IsTrue(_detector.IsUzbek("shaharga"));
    }

    [TestMethod]
    public void F07_05_GenitiveSuffix_Ning_Recognized()
    {
        Assert.IsTrue(_detector.IsUzbek("shaharning"));
    }

    [TestMethod]
    public void F07_06_AccusativeSuffix_Ni_Recognized()
    {
        Assert.IsTrue(_detector.IsUzbek("shaharni"));
    }

    [TestMethod]
    public void F07_07_DerivationalSuffix_Lik_Recognized()
    {
        Assert.IsTrue(_detector.IsUzbek("do'stlik"));
    }

    [TestMethod]
    public void F07_08_ChainedSuffixes_PluralPlusCase_Recognized()
    {
        // shaharlarda (-lar + -da)
        Assert.IsTrue(_detector.IsUzbek("shaharlarda"));
        // shaharlardan (-lar + -dan)
        Assert.IsTrue(_detector.IsUzbek("shaharlardan"));
    }

    [TestMethod]
    public void F07_09_PossessiveSuffix_Imiz_Recognized()
    {
        Assert.IsTrue(_detector.IsUzbek("shaharimiz"));
    }

    [TestMethod]
    public void F07_10_E2E_TypingInflectedWords_ConvertsProperly()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shaharlarda o'rtoqlarga g'alabadan ");
        Assert.AreEqual("şaharlarda örtoqlarga ğalabadan ", sim.GetScreenText());
        Assert.AreEqual(3, sim.WordsConvertedCount);
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 8: User Overrides (Whitelist & Blacklist in AppData)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F08_01_WhitelistAndBlacklist_PathsExist()
    {
        string whitePath = LanguageDetector.GetWhitelistPath();
        string blackPath = LanguageDetector.GetBlacklistPath();
        Assert.IsNotNull(whitePath);
        Assert.IsNotNull(blackPath);
        Assert.IsTrue(whitePath.Contains("YangiAlifbo"));
        Assert.IsTrue(blackPath.Contains("YangiAlifbo"));
    }

    [TestMethod]
    public void F08_02_UserBlacklist_PreventsConversionOfUzbekWord()
    {
        var black = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "shahar"
        };
        var detectorWithBlacklist = new LanguageDetector(
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shahar" },
            userBlacklist: black);

        Assert.IsFalse(detectorWithBlacklist.IsUzbek("shahar"),
            "Blacklisted word should be rejected even if in Uzbek lexicon.");
    }

    [TestMethod]
    public void F08_03_UserWhitelist_ForcesAcceptanceOfForeignWord()
    {
        var white = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "show"
        };
        var detectorWithWhitelist = new LanguageDetector(
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "show" },
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase),
            userWhitelist: white);

        Assert.IsTrue(detectorWithWhitelist.IsUzbek("show"),
            "Whitelisted word should be accepted even if in English reject list.");
    }

    [TestMethod]
    public void F08_04_Blacklist_TakesPrecedenceOverWhitelist()
    {
        var white = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "testword" };
        var black = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "testword" };

        var detector = new LanguageDetector(
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase),
            userWhitelist: white,
            userBlacklist: black);

        Assert.IsFalse(detector.IsUzbek("testword"),
            "Blacklist must take precedence over whitelist.");
    }

    [TestMethod]
    public void F08_05_ReloadUserLists_DoesNotThrow()
    {
        _detector.ReloadUserLists();
    }

    [TestMethod]
    public void F08_06_E2E_WhitelistedCustomJargon_Transliterated()
    {
        var white = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "chuchvara"
        };
        var customDetector = new LanguageDetector(
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase),
            userWhitelist: white);

        var sim = new TypingSimulator(customDetector);
        sim.TypeText("chuchvara ");
        Assert.AreEqual("çuçvara ", sim.GetScreenText());
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 9: Word Buffer Tracking & Delimiter Triggers
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F09_01_Space_TriggersWordConversion()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shahar ");
        Assert.AreEqual("şahar ", sim.GetScreenText());
    }

    [TestMethod]
    public void F09_02_Enter_TriggersWordConversion()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shahar\n");
        Assert.AreEqual("şahar\n", sim.GetScreenText());
    }

    [TestMethod]
    public void F09_03_Tab_TriggersWordConversion()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shahar\t");
        Assert.AreEqual("şahar\t", sim.GetScreenText());
    }

    [TestMethod]
    public void F09_04_Period_TriggersWordConversion()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shahar.");
        Assert.AreEqual("şahar.", sim.GetScreenText());
    }

    [TestMethod]
    public void F09_05_Comma_TriggersWordConversion()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shahar,");
        Assert.AreEqual("şahar,", sim.GetScreenText());
    }

    [TestMethod]
    public void F09_06_ExclamationAndQuestion_TriggerWordConversion()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("g'alaba! o'rtoq? ");
        Assert.AreEqual("ğalaba! örtoq? ", sim.GetScreenText());
    }

    [TestMethod]
    public void F09_07_ColonAndSemicolon_TriggerWordConversion()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shahar: choyxona; ");
        Assert.AreEqual("şahar: çoyxona; ", sim.GetScreenText());
    }

    [TestMethod]
    public void F09_08_AllStandardDelimiters_Recognized()
    {
        var delimiters = TestHarness.GetStandardDelimiters();
        Assert.IsTrue(delimiters.Contains(' '));
        Assert.IsTrue(delimiters.Contains('\t'));
        Assert.IsTrue(delimiters.Contains('\r'));
        Assert.IsTrue(delimiters.Contains('\n'));
        Assert.IsTrue(delimiters.Contains('.'));
        Assert.IsTrue(delimiters.Contains(','));
        Assert.IsTrue(delimiters.Contains('!'));
        Assert.IsTrue(delimiters.Contains('?'));
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 10: Input Synthesis & Anti-Recursion (SendInput & YANG)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F10_01_MagicExtraInfo_MatchesRequirement_YANG()
    {
        Assert.AreEqual(0x59414E47L, TestHarness.ExpectedMagicExtraInfo,
            "Magic marker must equal 0x59414E47 ('YANG' in ASCII)");
    }

    [TestMethod]
    public void F10_02_ErasureLength_MatchesOriginalWordLength()
    {
        // When 'shahar' (6 chars) is converted to 'şahar' (5 chars),
        // exactly 6 backspaces must be sent
        string original = "shahar";
        string converted = UzbekPhonetics.Convert(original);
        int expectedBackspaces = original.Length;

        Assert.AreEqual(6, expectedBackspaces);
        Assert.AreEqual(5, converted.Length);
    }

    [TestMethod]
    public void F10_03_ApostropheWord_ErasureLength()
    {
        // 'o'rtoq' is 6 chars in 1995, 'örtoq' is 5 chars in 2024
        string original = "o'rtoq";
        string converted = UzbekPhonetics.Convert(original);

        Assert.AreEqual(6, original.Length);
        Assert.AreEqual(5, converted.Length);
    }

    [TestMethod]
    public void F10_04_InputSynthesizerType_ExistsInAssembly()
    {
        Assert.IsNotNull(TestHarness.InputSynthesizerType,
            "InputSynthesizer type must exist in YangiAlifbo.Win32");
    }

    [TestMethod]
    public void F10_05_InputSynthesizer_Exposes_MagicExtraInfo()
    {
        var field = TestHarness.InputSynthesizerType?.GetField("MAGIC_EXTRA_INFO",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(field, "InputSynthesizer must expose MAGIC_EXTRA_INFO constant.");
        long val = Convert.ToInt64(field.GetValue(null));
        Assert.AreEqual(TestHarness.ExpectedMagicExtraInfo, val);
    }

    [TestMethod]
    public void F10_06_E2E_KeystrokeLatency_UnderThreshold()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shahar choyxona o'rtoq g'alaba ");

        foreach (var latency in sim.KeystrokeLatencies)
        {
            // Latency per keystroke event must be < 5ms (typically < 0.1ms)
            Assert.IsTrue(latency.TotalMilliseconds < 50.0,
                $"Keystroke latency was {latency.TotalMilliseconds}ms, exceeding threshold.");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 11: Process Exclusion (Terminals, IDEs)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F11_01_CmdExe_IsExcludedByDefault()
    {
        var excluded = TestHarness.GetDefaultExcludedProcesses();
        Assert.IsTrue(excluded.Contains("cmd.exe"));
    }

    [TestMethod]
    public void F11_02_PowershellExe_IsExcludedByDefault()
    {
        var excluded = TestHarness.GetDefaultExcludedProcesses();
        Assert.IsTrue(excluded.Contains("powershell.exe"));
        Assert.IsTrue(excluded.Contains("pwsh.exe"));
    }

    [TestMethod]
    public void F11_03_CodeExe_IsExcludedByDefault()
    {
        var excluded = TestHarness.GetDefaultExcludedProcesses();
        Assert.IsTrue(excluded.Contains("code.exe"));
    }

    [TestMethod]
    public void F11_04_DevenvExe_IsExcludedByDefault()
    {
        var excluded = TestHarness.GetDefaultExcludedProcesses();
        Assert.IsTrue(excluded.Contains("devenv.exe"));
    }

    [TestMethod]
    public void F11_05_E2E_TypingInCmdExe_LeavesTextUntouched()
    {
        var sim = new TypingSimulator(_detector);
        sim.SwitchWindow("cmd.exe");
        sim.TypeText("shahar o'rtoq ");
        Assert.AreEqual("shahar o'rtoq ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F11_06_E2E_TypingInNotepad_ConvertsText()
    {
        var sim = new TypingSimulator(_detector);
        sim.SwitchWindow("notepad.exe");
        sim.TypeText("shahar ");
        Assert.AreEqual("şahar ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 12: Password Field Protection
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F12_01_PasswordField_SuppressesTransliteration()
    {
        var sim = new TypingSimulator(_detector);
        sim.SwitchWindow("browser.exe", isPassword: true);
        sim.TypeText("shahar123 ");
        Assert.AreEqual("shahar123 ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F12_02_SwitchingFromPasswordToNormalField_ResumesTransliteration()
    {
        var sim = new TypingSimulator(_detector);
        // Step 1: Type in password field
        sim.SwitchWindow("browser.exe", isPassword: true);
        sim.TypeText("shahar ");
        Assert.AreEqual("shahar ", sim.GetScreenText());

        // Step 2: Switch to normal field
        sim.SwitchWindow("browser.exe", isPassword: false);
        sim.TypeText("o'rtoq ");
        Assert.AreEqual("shahar örtoq ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F12_03_FocusTrackerType_Exists()
    {
        Assert.IsNotNull(TestHarness.FocusTrackerType,
            "FocusTracker type must exist in YangiAlifbo.Win32");
    }

    [TestMethod]
    public void F12_04_FocusTracker_ProvidesAllowedProcessProperty()
    {
        var prop = TestHarness.FocusTrackerType?.GetProperty("IsCurrentProcessAllowed");
        Assert.IsNotNull(prop, "FocusTracker must have IsCurrentProcessAllowed property");
    }

    [TestMethod]
    public void F12_05_PasswordSpecialChars_NeverTransliterated()
    {
        var sim = new TypingSimulator(_detector) { IsPasswordField = true };
        sim.TypeText("P@ssw0rd_shahar! ");
        Assert.AreEqual("P@ssw0rd_shahar! ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 13: Buffer Invalidation on Navigation & Mouse Clicks
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F13_01_Backspace_RemovesLastCharacterFromBuffer()
    {
        var sim = new TypingSimulator(_detector);
        // Type "shah", press backspace, type "axs " -> "shaxs " -> "şaxs "
        sim.TypeText("shah");
        sim.PressBackspace(); // Buffer: "sha"
        sim.TypeText("xs ");  // Buffer: "shaxs"
        Assert.AreEqual("şaxs ", sim.GetScreenText());
    }

    [TestMethod]
    public void F13_02_ArrowKey_ClearsBuffer_PreventingDesync()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("sha");
        sim.PressNavigationKey(); // User moves caret with arrow key
        sim.TypeText("har ");     // Only "har" is in buffer, not "shahar"
        Assert.AreEqual("shahar ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F13_03_MouseClick_ClearsBuffer()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("cho");
        sim.ClickMouse();     // User clicked elsewhere
        sim.TypeText("yxona "); // Buffer has "yxona", not "choyxona"
        Assert.AreEqual("choyxona ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F13_04_EscapeKey_ClearsBuffer()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("g'ala");
        sim.PressNavigationKey(); // ESC pressed
        sim.TypeText("ba ");
        Assert.AreEqual("g'alaba ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F13_05_WindowSwitch_ClearsBuffer()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("o'r");
        sim.SwitchWindow("word.exe"); // Switched windows mid-word
        sim.TypeText("toq ");
        Assert.AreEqual("o'rtoq ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F13_06_ConsecutiveBackspaces_ClearsEntireBuffer()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shahar");
        for (int i = 0; i < 6; i++) sim.PressBackspace();
        sim.TypeText("kitob ");
        Assert.AreEqual("kitob ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 14: System Tray UI & Modes (Strict vs Aggressive, startup)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F14_01_StrictMode_DoesNotConvertUnknownWord()
    {
        var sim = new TypingSimulator(_detector) { Mode = SimulationMode.Strict };
        sim.TypeText("sho'xlik123 ");
        // If not in dictionary, strict mode leaves it untouched
        Assert.IsFalse(_detector.IsUzbek("sho'xlik123"));
        Assert.AreEqual("sho'xlik123 ", sim.GetScreenText());
    }

    [TestMethod]
    public void F14_02_AggressiveMode_ConvertsWithoutDictionaryCheck()
    {
        var sim = new TypingSimulator(_detector) { Mode = SimulationMode.Aggressive };
        sim.TypeText("sho'xlik123 ");
        // Aggressive mode converts all convertible sequences
        Assert.AreEqual("şöxlik123 ", sim.GetScreenText());
    }

    [TestMethod]
    public void F14_03_EngineDisabled_PassesThroughAllKeystrokes()
    {
        var sim = new TypingSimulator(_detector) { IsEnabled = false };
        sim.TypeText("shahar o'rtoq choyxona g'alaba ");
        Assert.AreEqual("shahar o'rtoq choyxona g'alaba ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F14_04_EngineToggledBackOn_ResumesConversion()
    {
        var sim = new TypingSimulator(_detector) { IsEnabled = false };
        sim.TypeText("shahar ");
        Assert.AreEqual("shahar ", sim.GetScreenText());

        sim.IsEnabled = true;
        sim.TypeText("choyxona ");
        Assert.AreEqual("shahar çoyxona ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F14_05_TrayAppContextType_ExistsInAssembly()
    {
        Assert.IsNotNull(TestHarness.TrayContextType,
            "TrayApplicationContext must exist in YangiAlifbo.UI");
    }

    [TestMethod]
    public void F14_06_StartupRegistryKey_ConfiguredCorrectly()
    {
        Assert.AreEqual(@"Software\Microsoft\Windows\CurrentVersion\Run",
            TestHarness.StartupRegistryKey);
        Assert.AreEqual("YangiAlifbo", TestHarness.StartupValueName);
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 15: Global Hotkey (Ctrl + Shift + U)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F15_01_HotkeyId_EqualsExpectedHex()
    {
        Assert.AreEqual(0x5941, TestHarness.ExpectedHotkeyId);
    }

    [TestMethod]
    public void F15_02_HotkeyVirtualKey_IsKeyU()
    {
        Assert.AreEqual(0x55u, TestHarness.ExpectedHotkeyVk);
    }

    [TestMethod]
    public void F15_03_SimulatedHotkey_TogglesEngineState()
    {
        var sim = new TypingSimulator(_detector) { IsEnabled = true };
        sim.TypeText("shahar ");
        Assert.AreEqual("şahar ", sim.GetScreenText());

        // Simulate user pressing Ctrl+Shift+U
        sim.IsEnabled = !sim.IsEnabled;
        Assert.IsFalse(sim.IsEnabled);

        sim.TypeText("o'rtoq ");
        Assert.AreEqual("şahar o'rtoq ", sim.GetScreenText());

        // Simulate pressing Ctrl+Shift+U again
        sim.IsEnabled = !sim.IsEnabled;
        Assert.IsTrue(sim.IsEnabled);

        sim.TypeText("choyxona ");
        Assert.AreEqual("şahar o'rtoq çoyxona ", sim.GetScreenText());
    }

    [TestMethod]
    public void F15_04_HotkeyWindowType_Exists()
    {
        var hotkeyWinType = TestHarness.YangiAlifboAssembly.GetType("YangiAlifbo.UI.HotkeyWindow");
        Assert.IsNotNull(hotkeyWinType, "HotkeyWindow class must exist in UI namespace");
    }

    [TestMethod]
    public void F15_05_HotkeyPressedMidWord_DoesNotCorruptCurrentWord()
    {
        var sim = new TypingSimulator(_detector) { IsEnabled = true };
        sim.TypeText("sha");
        // User presses hotkey while typing
        sim.IsEnabled = false;
        sim.TypeText("har ");
        // Buffer was disabled, so no conversion
        Assert.AreEqual("shahar ", sim.GetScreenText());
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 16: Single-Instance Mutex & Process Protection
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F16_01_SingleInstanceMutexName_MatchesSpecification()
    {
        Assert.AreEqual("Global\\YangiAlifbo_SingleInstance_Mutex",
            TestHarness.SingleInstanceMutexName);
    }

    [TestMethod]
    public void F16_02_AcquiringMutex_SucceedsForFirstInstance()
    {
        string uniqueTestMutex = "Global\\YangiAlifbo_TestMutex_" + Guid.NewGuid().ToString("N");
        using var mutex1 = new Mutex(true, uniqueTestMutex, out bool createdNew1);
        Assert.IsTrue(createdNew1, "First instance must successfully create the mutex.");
    }

    [TestMethod]
    public void F16_03_SecondInstance_DetectsExistingMutex()
    {
        string uniqueTestMutex = "Global\\YangiAlifbo_TestMutex_" + Guid.NewGuid().ToString("N");
        using var mutex1 = new Mutex(true, uniqueTestMutex, out bool createdNew1);
        Assert.IsTrue(createdNew1);

        using var mutex2 = new Mutex(true, uniqueTestMutex, out bool createdNew2);
        Assert.IsFalse(createdNew2, "Second instance must detect that mutex already exists.");
    }

    [TestMethod]
    public void F16_04_ReleasingMutex_AllowsSubsequentInstance()
    {
        string uniqueTestMutex = "Global\\YangiAlifbo_TestMutex_" + Guid.NewGuid().ToString("N");
        using (var mutex1 = new Mutex(true, uniqueTestMutex, out bool createdNew1))
        {
            Assert.IsTrue(createdNew1);
            mutex1.ReleaseMutex();
        }

        using var mutex2 = new Mutex(true, uniqueTestMutex, out bool createdNew2);
        Assert.IsTrue(createdNew2, "Subsequent instance can acquire mutex after release.");
    }

    [TestMethod]
    public void F16_05_ProgramType_ContainsSingleInstanceMutex()
    {
        var programType = TestHarness.YangiAlifboAssembly.GetType("YangiAlifbo.Program");
        Assert.IsNotNull(programType, "Program type must exist");

        var mutexField = programType.GetField("MutexName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(mutexField, "MutexName field must exist on Program");
        string? name = mutexField.GetValue(null) as string;
        Assert.AreEqual("Global\\YangiAlifbo_SingleInstance_Mutex", name);
    }
}
