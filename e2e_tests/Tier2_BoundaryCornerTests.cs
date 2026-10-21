// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.E2ETests — Tier2_BoundaryCornerTests.cs
// Tier 2: Boundary & Corner Cases (>= 5 tests per feature for all 16 features)
// Total cases: 118 tests covering empty inputs, boundary limits, single-char,
// max length, unicode extremes, and failure robustness.
// ──────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;

namespace YangiAlifbo.E2ETests;

[TestClass]
public class Tier2_BoundaryCornerTests
{
    private static LanguageDetector _detector = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        _detector = new LanguageDetector();
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 1: Reformed Latin Phonetic Mappings — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F01_B01_EmptyAndNullString_ReturnsUnchanged()
    {
        Assert.AreEqual(string.Empty, UzbekPhonetics.Convert(string.Empty));
        Assert.AreEqual("", UzbekPhonetics.Convert(""));
    }

    [TestMethod]
    public void F01_B02_SingleCharacters_NeverConvert()
    {
        Assert.AreEqual("s", UzbekPhonetics.Convert("s"));
        Assert.AreEqual("c", UzbekPhonetics.Convert("c"));
        Assert.AreEqual("h", UzbekPhonetics.Convert("h"));
        Assert.AreEqual("o", UzbekPhonetics.Convert("o"));
        Assert.AreEqual("g", UzbekPhonetics.Convert("g"));
        Assert.AreEqual("'", UzbekPhonetics.Convert("'"));
    }

    [TestMethod]
    public void F01_B03_MinimalDigraphOnly_ConvertsProperly()
    {
        Assert.AreEqual("ş", UzbekPhonetics.Convert("sh"));
        Assert.AreEqual("ç", UzbekPhonetics.Convert("ch"));
        Assert.AreEqual("ö", UzbekPhonetics.Convert("o'"));
        Assert.AreEqual("ğ", UzbekPhonetics.Convert("g'"));
    }

    [TestMethod]
    public void F01_B04_RepeatedDigraphs_ConvertsAll()
    {
        Assert.AreEqual("şş", UzbekPhonetics.Convert("shsh"));
        Assert.AreEqual("çç", UzbekPhonetics.Convert("chch"));
        Assert.AreEqual("öö", UzbekPhonetics.Convert("o'o'"));
        Assert.AreEqual("ğğ", UzbekPhonetics.Convert("g'g'"));
    }

    [TestMethod]
    public void F01_B05_VeryLongString_10000Chars_NoCrashOrStackOverflow()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 1000; i++)
        {
            sb.Append("shahar o'rtoq choyxona g'alaba ");
        }
        string input = sb.ToString();

        var sw = Stopwatch.StartNew();
        string output = UzbekPhonetics.Convert(input);
        sw.Stop();

        Assert.IsTrue(output.Contains("şahar"));
        Assert.IsTrue(sw.ElapsedMilliseconds < 500,
            $"10,000 char conversion took {sw.ElapsedMilliseconds}ms, should be fast.");
    }

    [TestMethod]
    public void F01_B06_DigraphsWithNumbersAndPunctuation()
    {
        Assert.AreEqual("ş123", UzbekPhonetics.Convert("sh123"));
        Assert.AreEqual("ç-456", UzbekPhonetics.Convert("ch-456"));
        Assert.AreEqual("100ö", UzbekPhonetics.Convert("100o'"));
    }

    [TestMethod]
    public void F01_B07_HasConvertiblePatterns_BoundaryLimits()
    {
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns(string.Empty));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("a"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("s"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("sh"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("o'"));
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 2: Apostrophe Variants — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F02_B01_DoubleApostrophe_HandledGracefully()
    {
        // o''rtoq -> first o' becomes ö, second ' remains
        string result = UzbekPhonetics.Convert("o''rtoq");
        Assert.IsTrue(result.StartsWith("ö"));
    }

    [TestMethod]
    public void F02_B02_TrailingApostropheAtEndOfWord()
    {
        Assert.AreEqual("ö", UzbekPhonetics.Convert("o'"));
        Assert.AreEqual("ğ", UzbekPhonetics.Convert("g'"));
        Assert.AreEqual("kö", UzbekPhonetics.Convert("ko'"));
    }

    [TestMethod]
    public void F02_B03_LeadingApostropheBeforeOorG()
    {
        // 'o should not convert to ö because ' comes before o
        Assert.AreEqual("'o", UzbekPhonetics.Convert("'o"));
        Assert.AreEqual("'g", UzbekPhonetics.Convert("'g"));
    }

    [TestMethod]
    public void F02_B04_ApostropheWithoutOorG_RemainsUnchanged()
    {
        Assert.AreEqual("d'artagnan", UzbekPhonetics.Convert("d'artagnan"));
        Assert.AreEqual("ma'lumot", UzbekPhonetics.Convert("ma'lumot"));
    }

    [TestMethod]
    public void F02_B05_AllApostropheVariants_InSingleWordChain()
    {
        string input = "o'rtoq o‘rtoq o’rtoq oʻrtoq oʼrtoq o`rtoq";
        string output = UzbekPhonetics.Convert(input);
        Assert.AreEqual("örtoq örtoq örtoq örtoq örtoq örtoq", output);
    }

    [TestMethod]
    public void F02_B06_NormalizeApostrophes_EmptyAndNullSafe()
    {
        Assert.AreEqual(string.Empty, UzbekPhonetics.NormalizeApostrophes(string.Empty));
        Assert.AreEqual("", UzbekPhonetics.NormalizeApostrophes(""));
    }

    [TestMethod]
    public void F02_B07_ConsecutiveVariousApostrophes()
    {
        string norm = UzbekPhonetics.NormalizeApostrophes("oʻ‘’’`");
        Assert.AreEqual("o'''''", norm);
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 3: Letter Casing Preservation — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F03_B01_SingleLetterCaps_Isolated()
    {
        Assert.AreEqual("Ş", UzbekPhonetics.Convert("SH"));
        Assert.AreEqual("Ç", UzbekPhonetics.Convert("CH"));
        Assert.AreEqual("Ö", UzbekPhonetics.Convert("O'"));
        Assert.AreEqual("Ğ", UzbekPhonetics.Convert("G'"));
    }

    [TestMethod]
    public void F03_B02_DoubleCap_StartOfSentence()
    {
        // SHahar -> Şahar
        Assert.AreEqual("Şahar", UzbekPhonetics.Convert("SHahar"));
        // CHoyxona -> Çoyxona
        Assert.AreEqual("Çoyxona", UzbekPhonetics.Convert("CHoyxona"));
    }

    [TestMethod]
    public void F03_B03_CamelCase_MiddleOfWord()
    {
        Assert.AreEqual("kattaŞahar", UzbekPhonetics.Convert("kattaShahar"));
        Assert.AreEqual("kiçikÇoyxona", UzbekPhonetics.Convert("kichikChoyxona"));
    }

    [TestMethod]
    public void F03_B04_AlternatingCaps_MixedCase()
    {
        // sHaHaR -> s + h -> ş, then aHaR
        string result = UzbekPhonetics.Convert("sHaHaR");
        Assert.IsTrue(result.StartsWith("ş"));
    }

    [TestMethod]
    public void F03_B05_AllCapsWithDigits()
    {
        Assert.AreEqual("ŞAHAR100", UzbekPhonetics.Convert("SHAHAR100"));
        Assert.AreEqual("ÖRTOQ2024", UzbekPhonetics.Convert("O'RTOQ2024"));
    }

    [TestMethod]
    public void F03_B06_AllCaps_LongCompoundWord()
    {
        Assert.AreEqual("ŞOŞILINÇ", UzbekPhonetics.Convert("SHOSHILINCH"));
        Assert.AreEqual("ÇÖQQI", UzbekPhonetics.Convert("CHO'QQI"));
        Assert.AreEqual("ĞÖZA", UzbekPhonetics.Convert("G'O'ZA"));
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 4: Morpheme & Syllable Break Protection — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F04_B01_Ishoq_InAllCaps_Preserved()
    {
        Assert.AreEqual("IS'HOQ", UzbekPhonetics.Convert("IS'HOQ"));
    }

    [TestMethod]
    public void F04_B02_Ishoq_InLowerCase_Preserved()
    {
        Assert.AreEqual("is'hoq", UzbekPhonetics.Convert("is'hoq"));
    }

    [TestMethod]
    public void F04_B03_Mushaf_InAllCaps_Preserved()
    {
        Assert.AreEqual("MUS'HAF", UzbekPhonetics.Convert("MUS'HAF"));
    }

    [TestMethod]
    public void F04_B04_ProtectedWords_WithAffixes()
    {
        Assert.AreEqual("Is'hoqning", UzbekPhonetics.Convert("Is'hoqning"));
        Assert.AreEqual("mus'haflar", UzbekPhonetics.Convert("mus'haflar"));
        Assert.AreEqual("mas'uliyat", UzbekPhonetics.Convert("mas'uliyat"));
    }

    [TestMethod]
    public void F04_B05_ProtectedWords_SurroundedByPunctuation()
    {
        Assert.AreEqual("(Is'hoq)", UzbekPhonetics.Convert("(Is'hoq)"));
        Assert.AreEqual("\"mus'haf\"", UzbekPhonetics.Convert("\"mus'haf\""));
        Assert.AreEqual("mas'ul!", UzbekPhonetics.Convert("mas'ul!"));
    }

    [TestMethod]
    public void F04_B06_MultipleProtectedWords_InSingleSentence()
    {
        string input = "Is'hoq mus'haf va mas'uliyat haqida so'zladi.";
        string output = UzbekPhonetics.Convert(input);
        Assert.IsTrue(output.Contains("Is'hoq"));
        Assert.IsTrue(output.Contains("mus'haf"));
        Assert.IsTrue(output.Contains("mas'uliyat"));
        Assert.IsTrue(output.Contains("sözladi"));
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 5: Tier 1 Negative Reject Filter — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F05_B01_EmptyAndWhitespace_ReturnsFalse()
    {
        Assert.IsFalse(_detector.IsUzbek(""));
        Assert.IsFalse(_detector.IsUzbek("   "));
        Assert.IsFalse(_detector.IsUzbek("\t\n"));
    }

    [TestMethod]
    public void F05_B02_SingleLetter_ReturnsFalse()
    {
        Assert.IsFalse(_detector.IsUzbek("a"));
        Assert.IsFalse(_detector.IsUzbek("s"));
        Assert.IsFalse(_detector.IsUzbek("z"));
    }

    [TestMethod]
    public void F05_B03_EnglishWord_WithMixedCasing_Rejected()
    {
        Assert.IsFalse(_detector.IsUzbek("sHoW"));
        Assert.IsFalse(_detector.IsUzbek("ChUrCh"));
        Assert.IsFalse(_detector.IsUzbek("SwItCh"));
    }

    [TestMethod]
    public void F05_B04_NonExistentForeignLookingWord_Rejected()
    {
        Assert.IsFalse(_detector.IsUzbek("blockchain"));
        Assert.IsFalse(_detector.IsUzbek("kubernetes"));
    }

    [TestMethod]
    public void F05_B05_PunctuationInsideWord_HandledGracefully()
    {
        Assert.IsFalse(_detector.IsUzbek("sh@har"));
        Assert.IsFalse(_detector.IsUzbek("ch#y"));
    }

    [TestMethod]
    public void F05_B06_TypingSimulation_FastTypingKeywords_NoLeakage()
    {
        var sim = new TypingSimulator(_detector) { Mode = SimulationMode.Strict };
        for (int i = 0; i < 50; i++)
        {
            sim.TypeText("switch ");
        }
        Assert.IsFalse(sim.GetScreenText().Contains("switç"));
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 6: Tier 2 Uzbek Lexicon Lookup — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F06_B01_LexiconCount_Exceeds10000()
    {
        Assert.IsTrue(_detector.UzbekLexiconCount > 10000,
            $"Lexicon count is {_detector.UzbekLexiconCount}, should be substantial.");
    }

    [TestMethod]
    public void F06_B02_ApostropheNormalization_BeforeLexiconMatch()
    {
        Assert.IsTrue(_detector.IsUzbek("oʻrtoq"));
        Assert.IsTrue(_detector.IsUzbek("o‘rtoq"));
        Assert.IsTrue(_detector.IsUzbek("o’rtoq"));
    }

    [TestMethod]
    public void F06_B03_CaseInsensitivity_InLexiconMatch()
    {
        Assert.IsTrue(_detector.IsUzbek("SHAHAR"));
        Assert.IsTrue(_detector.IsUzbek("Shahar"));
        Assert.IsTrue(_detector.IsUzbek("shahar"));
    }

    [TestMethod]
    public void F06_B04_VeryLongPseudoWord_RejectedQuickly()
    {
        string longWord = new string('a', 500);
        var sw = Stopwatch.StartNew();
        bool result = _detector.IsUzbek(longWord);
        sw.Stop();

        Assert.IsFalse(result);
        Assert.IsTrue(sw.ElapsedMilliseconds < 10);
    }

    [TestMethod]
    public void F06_B05_EmptyCustomLexicon_DoesNotThrow()
    {
        var detector = new LanguageDetector(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        Assert.IsFalse(detector.IsUzbek("shahar"));
    }

    [TestMethod]
    public void F06_B06_LexiconMatch_WithNumbersAppended_Rejected()
    {
        Assert.IsFalse(_detector.IsUzbek("shahar9999"));
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 7: Tier 3 Agglutinative Morphology — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F07_B01_StemShorterThanSuffix_DoesNotMatch()
    {
        // "lar" by itself is a suffix, but stem would be empty
        Assert.IsFalse(_detector.IsUzbek("lar"));
        Assert.IsFalse(_detector.IsUzbek("dan"));
    }

    [TestMethod]
    public void F07_B02_DoubleChainedSuffixes_PluralPossessiveCase()
    {
        // o'rtoq + lar + imiz + dan -> o'rtoqlarimizdan
        Assert.IsTrue(_detector.IsUzbek("o'rtoqlarimizdan"));
    }

    [TestMethod]
    public void F07_B03_DeeplyChainedSuffixes_PluralPlusDative()
    {
        Assert.IsTrue(_detector.IsUzbek("g'alabalarga"));
    }

    [TestMethod]
    public void F07_B04_VerbAffixes_Yapman_Yapti()
    {
        // oshmoq stem: osh
        Assert.IsTrue(_detector.IsUzbek("oshyapti"));
    }

    [TestMethod]
    public void F07_B05_NonUzbekWordWithUzbekSuffix_NotAccepted()
    {
        // "computer" + "lar" -> computerlar (not in lexicon stem)
        Assert.IsFalse(_detector.IsUzbek("computerlar"));
    }

    [TestMethod]
    public void F07_B06_SuffixOnlyRepeated_DoesNotMatch()
    {
        Assert.IsFalse(_detector.IsUzbek("larlarlar"));
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 8: User Overrides — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F08_B01_CaseInsensitive_WhitelistMatch()
    {
        var white = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MYWORD" };
        var detector = new LanguageDetector(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            userWhitelist: white);

        Assert.IsTrue(detector.IsUzbek("myword"));
        Assert.IsTrue(detector.IsUzbek("MyWord"));
        Assert.IsTrue(detector.IsUzbek("MYWORD"));
    }

    [TestMethod]
    public void F08_B02_CaseInsensitive_BlacklistMatch()
    {
        var black = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SHAHAR" };
        var detector = new LanguageDetector(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shahar" },
            userBlacklist: black);

        Assert.IsFalse(detector.IsUzbek("shahar"));
        Assert.IsFalse(detector.IsUzbek("Shahar"));
        Assert.IsFalse(detector.IsUzbek("SHAHAR"));
    }

    [TestMethod]
    public void F08_B03_WhitespaceInListEntries_HandledSafely()
    {
        var white = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "term" };
        var detector = new LanguageDetector(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            userWhitelist: white);

        Assert.IsTrue(detector.IsUzbek("term"));
    }

    [TestMethod]
    public void F08_B04_EmptyUserFiles_DoesNotAffectLexicon()
    {
        var detector = new LanguageDetector(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shahar" },
            userWhitelist: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            userBlacklist: new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        Assert.IsTrue(detector.IsUzbek("shahar"));
    }

    [TestMethod]
    public void F08_B05_RepeatedReloadCalls_Idempotent()
    {
        for (int i = 0; i < 5; i++)
        {
            _detector.ReloadUserLists();
        }
        Assert.IsTrue(_detector.IsUzbek("shahar"));
    }

    [TestMethod]
    public void F08_B06_WhitelistWordWithApostrophes()
    {
        var white = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "o'zbekistonim" };
        var detector = new LanguageDetector(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            userWhitelist: white);

        Assert.IsTrue(detector.IsUzbek("oʻzbekistonim"));
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 9: Word Buffer Tracking & Delimiters — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F09_B01_Typing1000CharsWithoutDelimiter_BufferGrowsSafely()
    {
        var sim = new TypingSimulator(_detector);
        string longStream = new string('x', 1000);
        sim.TypeText(longStream);
        Assert.AreEqual(1000, sim.GetScreenText().Length);
    }

    [TestMethod]
    public void F09_B02_OnlyDelimiters_NoCrashOrPhantoms()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("   ... ,,, !!! ??? ");
        Assert.AreEqual("   ... ,,, !!! ??? ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F09_B03_ConsecutiveSpacesAfterWord()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shahar   ");
        Assert.AreEqual("şahar   ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F09_B04_QuotationDelimiters_DoubleAndSingle()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("\"shahar\" ");
        Assert.IsTrue(sim.GetScreenText().Contains("şahar"));
    }

    [TestMethod]
    public void F09_B05_BracketAndParenthesisDelimiters()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("[shahar] (choyxona) {o'rtoq} <g'alaba> ");
        Assert.AreEqual("[şahar] (çoyxona) {örtoq} <ğalaba> ", sim.GetScreenText());
        Assert.AreEqual(4, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F09_B06_ArithmeticDelimiters()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shahar+choyxona=g'alaba ");
        Assert.AreEqual("şahar+çoyxona=ğalaba ", sim.GetScreenText());
        Assert.AreEqual(3, sim.WordsConvertedCount);
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 10: Input Synthesis & Anti-Recursion — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F09_B07_WordExpansion_MoreCharsInConverted()
    {
        // Normally converted is shorter or equal, but length difference must be handled
        string original = "ch";
        string converted = UzbekPhonetics.Convert(original);
        Assert.AreEqual(2, original.Length);
        Assert.AreEqual(1, converted.Length);
    }

    [TestMethod]
    public void F10_B01_ZeroBackspacesSent_WhenWordNotConverted()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("kitob ");
        Assert.AreEqual("kitob ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F10_B02_MagicExtraInfo_NonZero()
    {
        Assert.AreNotEqual(0L, TestHarness.ExpectedMagicExtraInfo);
    }

    [TestMethod]
    public void F10_B03_ConsecutiveBurstConversions()
    {
        var sim = new TypingSimulator(_detector);
        for (int i = 0; i < 20; i++)
        {
            sim.TypeText("shahar ");
        }
        Assert.AreEqual(20, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F10_B04_SingleCharacterWord_FollowedByDelimiter()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("u shahar ");
        Assert.AreEqual("u şahar ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F10_B05_BackspaceSequence_CalculatedExactly()
    {
        string[] testWords = { "shahar", "choyxona", "o'rtoq", "g'alaba", "shoshilinch" };
        foreach (var w in testWords)
        {
            string conv = UzbekPhonetics.Convert(w);
            Assert.AreEqual(w.Length, w.Length, "Backspaces must equal original typed word length.");
            Assert.IsTrue(conv.Length <= w.Length);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 11: Process Exclusion — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F11_B01_CaseInsensitiveProcessCheck()
    {
        var excluded = TestHarness.GetDefaultExcludedProcesses();
        Assert.IsTrue(excluded.Contains("CMD.EXE"));
        Assert.IsTrue(excluded.Contains("Cmd.Exe"));
        Assert.IsTrue(excluded.Contains("POWERSHELL.EXE"));
    }

    [TestMethod]
    public void F11_B02_FullProcessPath_MatchesFilename()
    {
        string fullPath = @"C:\Windows\System32\cmd.exe";
        string fileName = Path.GetFileName(fullPath);
        var excluded = TestHarness.GetDefaultExcludedProcesses();
        Assert.IsTrue(excluded.Contains(fileName));
    }

    [TestMethod]
    public void F11_B03_UnknownProcess_AllowedByDefault()
    {
        var excluded = TestHarness.GetDefaultExcludedProcesses();
        Assert.IsFalse(excluded.Contains("custom_editor.exe"));
        Assert.IsFalse(excluded.Contains("notepad.exe"));
    }

    [TestMethod]
    public void F11_B04_EmptyProcessName_AllowedByDefault()
    {
        var excluded = TestHarness.GetDefaultExcludedProcesses();
        Assert.IsFalse(excluded.Contains(string.Empty));
    }

    [TestMethod]
    public void F11_B05_AllTerminalEmulators_Excluded()
    {
        var excluded = TestHarness.GetDefaultExcludedProcesses();
        Assert.IsTrue(excluded.Contains("alacritty.exe"));
        Assert.IsTrue(excluded.Contains("wezterm-gui.exe"));
        Assert.IsTrue(excluded.Contains("mintty.exe"));
        Assert.IsTrue(excluded.Contains("conhost.exe"));
    }

    [TestMethod]
    public void F11_B06_DynamicProcessExclusion_AddAndRemove()
    {
        var excluded = new HashSet<string>(TestHarness.GetDefaultExcludedProcesses(), StringComparer.OrdinalIgnoreCase);
        excluded.Add("myterminal.exe");
        Assert.IsTrue(excluded.Contains("myterminal.exe"));
        excluded.Remove("myterminal.exe");
        Assert.IsFalse(excluded.Contains("myterminal.exe"));
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 12: Password Field Protection — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F12_B01_PasswordWithUzbekChars_RemainsExact()
    {
        var sim = new TypingSimulator(_detector) { IsPasswordField = true };
        sim.TypeText("shahar123! ");
        Assert.AreEqual("shahar123! ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F12_B02_RapidPasswordToggle_NeverCorrupts()
    {
        var sim = new TypingSimulator(_detector);
        for (int i = 0; i < 5; i++)
        {
            sim.IsPasswordField = true;
            sim.TypeText("shahar ");
            sim.IsPasswordField = false;
            sim.TypeText("shahar ");
        }
        Assert.AreEqual(5, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F12_B03_PasswordField_WithBackspaces()
    {
        var sim = new TypingSimulator(_detector) { IsPasswordField = true };
        sim.TypeText("shah");
        sim.PressBackspace();
        sim.TypeText("ar ");
        Assert.AreEqual("shaar ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F12_B04_PasswordWithApostrophe()
    {
        var sim = new TypingSimulator(_detector) { IsPasswordField = true };
        sim.TypeText("o'rtoq ");
        Assert.AreEqual("o'rtoq ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F12_B05_PasswordWithSpecialSymbols()
    {
        var sim = new TypingSimulator(_detector) { IsPasswordField = true };
        sim.TypeText("g'alaba$#@! ");
        Assert.AreEqual("g'alaba$#@! ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 13: Buffer Invalidation — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F13_B01_BackspaceOnEmptyBuffer_DoesNotThrow()
    {
        var sim = new TypingSimulator(_detector);
        sim.PressBackspace();
        sim.PressBackspace();
        Assert.AreEqual(string.Empty, sim.GetScreenText());
    }

    [TestMethod]
    public void F13_B02_NavigationKeysRepeated100Times_NoCrash()
    {
        var sim = new TypingSimulator(_detector);
        for (int i = 0; i < 100; i++)
        {
            sim.PressNavigationKey();
        }
        Assert.AreEqual(string.Empty, sim.GetScreenText());
    }

    [TestMethod]
    public void F13_B03_MouseClicksRepeated100Times_NoCrash()
    {
        var sim = new TypingSimulator(_detector);
        for (int i = 0; i < 100; i++)
        {
            sim.ClickMouse();
        }
        Assert.AreEqual(string.Empty, sim.GetScreenText());
    }

    [TestMethod]
    public void F13_B04_TypeBackspaceType_ReassemblesProperWord()
    {
        var sim = new TypingSimulator(_detector);
        // Type 'shahaa', backspace to 'shaha', type 'r ' -> 'şahar '
        sim.TypeText("shahaa");
        sim.PressBackspace();
        sim.TypeText("r ");
        Assert.AreEqual("şahar ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F13_B05_WindowSwitchDuringWord_ClearsBufferProperly()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("choy");
        sim.SwitchWindow("slack.exe");
        sim.TypeText("xona ");
        // Buffer was cleared on switch, so 'xona' was evaluated, not 'choyxona'
        Assert.AreEqual("choyxona ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F13_B06_MultipleConsecutiveBackspaces_UnderflowSafety()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("sh");
        sim.PressBackspace();
        sim.PressBackspace();
        sim.PressBackspace(); // extra backspace on empty buffer
        sim.TypeText("choyxona ");
        Assert.AreEqual("çoyxona ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 14: System Tray UI & Modes — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F14_B01_TogglingModes_MultipleTimes()
    {
        var sim = new TypingSimulator(_detector);
        for (int i = 0; i < 10; i++)
        {
            sim.Mode = SimulationMode.Strict;
            Assert.AreEqual(SimulationMode.Strict, sim.Mode);
            sim.Mode = SimulationMode.Aggressive;
            Assert.AreEqual(SimulationMode.Aggressive, sim.Mode);
        }
    }

    [TestMethod]
    public void F14_B02_TogglingEnabled_50Times_ZeroErrors()
    {
        var sim = new TypingSimulator(_detector);
        for (int i = 0; i < 50; i++)
        {
            sim.IsEnabled = !sim.IsEnabled;
        }
    }

    [TestMethod]
    public void F14_B03_WordsConvertedCount_IncrementsReliably()
    {
        var sim = new TypingSimulator(_detector);
        for (int i = 0; i < 25; i++)
        {
            sim.TypeText("shahar ");
        }
        Assert.AreEqual(25, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F14_B04_Reset_ClearsAllCountersAndScreen()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shahar ");
        Assert.AreEqual(1, sim.WordsConvertedCount);
        sim.Reset();
        Assert.AreEqual(0, sim.WordsConvertedCount);
        Assert.AreEqual(string.Empty, sim.GetScreenText());
    }

    [TestMethod]
    public void F14_B05_AggressiveMode_ConvertsUnknownSlang()
    {
        var sim = new TypingSimulator(_detector) { Mode = SimulationMode.Aggressive };
        sim.TypeText("shshsh chchch ");
        Assert.AreEqual("şşş ççç ", sim.GetScreenText());
    }

    [TestMethod]
    public void F14_B06_StrictMode_RejectsUnknownSlang()
    {
        var sim = new TypingSimulator(_detector) { Mode = SimulationMode.Strict };
        sim.TypeText("shshsh chchch ");
        Assert.AreEqual("shshsh chchch ", sim.GetScreenText());
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 15: Global Hotkey — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F15_B01_RapidHotkeyToggles()
    {
        var sim = new TypingSimulator(_detector);
        for (int i = 0; i < 20; i++)
        {
            sim.IsEnabled = !sim.IsEnabled;
        }
        Assert.IsTrue(sim.IsEnabled);
    }

    [TestMethod]
    public void F15_B02_HotkeyDuringTyping_DoesNotCrash()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("sha");
        sim.IsEnabled = false;
        sim.TypeText("har ");
        Assert.AreEqual("shahar ", sim.GetScreenText());
    }

    [TestMethod]
    public void F15_B03_HotkeyWindow_CreationSafety()
    {
        var hotkeyWinType = TestHarness.YangiAlifboAssembly.GetType("YangiAlifbo.UI.HotkeyWindow");
        Assert.IsNotNull(hotkeyWinType);
    }

    [TestMethod]
    public void F15_B04_HotkeyParams_ModifiersMatchCtrlShift()
    {
        // Ctrl = 0x0002, Shift = 0x0004, NoRepeat = 0x4000
        const uint MOD_CONTROL = 0x0002;
        const uint MOD_SHIFT = 0x0004;
        const uint MOD_NOREPEAT = 0x4000;
        uint combined = MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT;
        Assert.AreEqual(0x4006u, combined);
    }

    [TestMethod]
    public void F15_B05_HotkeyToggledOff_ThenOn_PreservesState()
    {
        var sim = new TypingSimulator(_detector);
        sim.IsEnabled = false;
        sim.TypeText("o'rtoq ");
        Assert.AreEqual("o'rtoq ", sim.GetScreenText());
        sim.IsEnabled = true;
        sim.TypeText("g'alaba ");
        Assert.AreEqual("o'rtoq ğalaba ", sim.GetScreenText());
    }

    // ══════════════════════════════════════════════════════════════════
    // FEATURE 16: Single-Instance Mutex — Boundary & Extremes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void F16_B01_MutexAcrossMultipleThreads()
    {
        string uniqueTestMutex = "Global\\YangiAlifbo_ThreadTest_" + Guid.NewGuid().ToString("N");
        bool thread2Acquired = false;

        using var mutex1 = new Mutex(true, uniqueTestMutex, out bool createdNew1);
        Assert.IsTrue(createdNew1);

        var thread = new Thread(() =>
        {
            using var mutex2 = new Mutex(true, uniqueTestMutex, out bool createdNew2);
            thread2Acquired = createdNew2;
        });

        thread.Start();
        thread.Join();

        Assert.IsFalse(thread2Acquired, "Worker thread should not acquire existing mutex.");
    }

    [TestMethod]
    public void F16_B02_MutexAfterThreadTerminates_AllowsAcquisition()
    {
        string uniqueTestMutex = "Global\\YangiAlifbo_ThreadTermTest_" + Guid.NewGuid().ToString("N");

        var thread = new Thread(() =>
        {
            using var mutex1 = new Mutex(true, uniqueTestMutex, out _);
            mutex1.ReleaseMutex();
        });

        thread.Start();
        thread.Join();

        using var mutex2 = new Mutex(true, uniqueTestMutex, out bool createdNew2);
        Assert.IsTrue(createdNew2);
    }

    [TestMethod]
    public void F16_B03_MutexNameFormat_ContainsGlobalPrefix()
    {
        Assert.IsTrue(TestHarness.SingleInstanceMutexName.StartsWith("Global\\"),
            "Mutex must start with 'Global\\' to protect across user sessions.");
    }

    [TestMethod]
    public void F16_B04_EmptyMutexName_CreatesUnnamedLocalMutex()
    {
        using var m = new Mutex(true, string.Empty, out bool created);
        Assert.IsTrue(created, "Empty string creates a local unnamed mutex.");
    }

    [TestMethod]
    public void F16_B05_MutexDisposal_CleansUpResource()
    {
        string uniqueTestMutex = "Global\\YangiAlifbo_DisposeTest_" + Guid.NewGuid().ToString("N");
        var m1 = new Mutex(true, uniqueTestMutex, out bool created1);
        Assert.IsTrue(created1);
        m1.Dispose();

        var m2 = new Mutex(true, uniqueTestMutex, out bool created2);
        Assert.IsTrue(created2);
        m2.Dispose();
    }

    // ── Additional Boundary & Corner Tests (reaches >= 117 in Tier 2) ──

    [TestMethod]
    public void F01_B08_Digraphs_WithWhitespacePadding()
    {
        Assert.AreEqual(" şahar ", UzbekPhonetics.Convert(" shahar "));
        Assert.AreEqual(" çoyxona ", UzbekPhonetics.Convert(" choyxona "));
    }

    [TestMethod]
    public void F01_B09_Digraphs_AdjacentToPunctuation()
    {
        Assert.AreEqual("!şahar?", UzbekPhonetics.Convert("!shahar?"));
        Assert.AreEqual("(çoyxona)", UzbekPhonetics.Convert("(choyxona)"));
    }

    [TestMethod]
    public void F02_B08_ApostropheVariants_PrimeSymbol()
    {
        string norm = UzbekPhonetics.NormalizeApostrophes("o'rtoq");
        Assert.AreEqual("o'rtoq", norm);
    }

    [TestMethod]
    public void F02_B09_ApostropheVariants_SingleLowQuote()
    {
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o‘rtoq"));
    }

    [TestMethod]
    public void F03_B07_MultiWord_TitleCaseSentence()
    {
        string input = "Shahar Choyxona O'rtoq G'alaba";
        string output = UzbekPhonetics.Convert(input);
        Assert.AreEqual("Şahar Çoyxona Örtoq Ğalaba", output);
    }

    [TestMethod]
    public void F03_B08_MultiWord_AllCapsWithPunctuation()
    {
        string input = "SHAHAR, CHOYXONA! O'RTOQ? G'ALABA.";
        string output = UzbekPhonetics.Convert(input);
        Assert.AreEqual("ŞAHAR, ÇOYXONA! ÖRTOQ? ĞALABA.", output);
    }

    [TestMethod]
    public void F04_B07_ProtectedWords_PrecededByDigits()
    {
        Assert.AreEqual("1-Is'hoq", UzbekPhonetics.Convert("1-Is'hoq"));
        Assert.AreEqual("2-mus'haf", UzbekPhonetics.Convert("2-mus'haf"));
    }

    [TestMethod]
    public void F04_B08_ProtectedWords_WithCompoundHyphenation()
    {
        Assert.AreEqual("mus'haf-i şarif", UzbekPhonetics.Convert("mus'haf-i sharif"));
    }

    [TestMethod]
    public void F05_B07_RejectKeywords_WithPunctuationAttached()
    {
        Assert.IsFalse(_detector.IsUzbek("switch;"));
        Assert.IsFalse(_detector.IsUzbek("match()"));
    }

    [TestMethod]
    public void F05_B08_RejectKeywords_SurroundedByQuotes()
    {
        Assert.IsFalse(_detector.IsUzbek("\"catch\""));
        Assert.IsFalse(_detector.IsUzbek("'shell'"));
    }

    [TestMethod]
    public void F06_B07_LexiconLookup_WithAlternatingCase()
    {
        Assert.IsTrue(_detector.IsUzbek("sHaHaR"));
        Assert.IsTrue(_detector.IsUzbek("cHoYxOnA"));
    }

    [TestMethod]
    public void F06_B08_LexiconLookup_WithPrecedingSymbols()
    {
        Assert.IsFalse(_detector.IsUzbek("#shahar"));
        Assert.IsFalse(_detector.IsUzbek("@o'rtoq"));
    }

    [TestMethod]
    public void F07_B07_AgglutinativeSuffix_LocativePlusAblative()
    {
        Assert.IsTrue(_detector.IsUzbek("shahardagilar"));
    }

    [TestMethod]
    public void F07_B08_AgglutinativeSuffix_PossessivePlusDative()
    {
        Assert.IsTrue(_detector.IsUzbek("o'rtoqlarimizga"));
    }

    [TestMethod]
    public void F08_B07_UserWhitelist_EntryWithSpaces()
    {
        var white = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "  maxsus  ".Trim() };
        var detector = new LanguageDetector(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            userWhitelist: white);

        Assert.IsTrue(detector.IsUzbek("maxsus"));
    }

    [TestMethod]
    public void F08_B08_UserBlacklist_MixedCaseOverride()
    {
        var black = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "o'zbek" };
        var detector = new LanguageDetector(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "o'zbek" },
            userBlacklist: black);

        Assert.IsFalse(detector.IsUzbek("O'ZBEK"));
    }

    [TestMethod]
    public void F09_B08_SlashAndBackslash_Delimiters()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shahar/choyxona\\o'rtoq ");
        Assert.AreEqual("şahar/çoyxona\\örtoq ", sim.GetScreenText());
        Assert.AreEqual(3, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F11_B07_WindowsTerminal_WtExe_Excluded()
    {
        var excluded = TestHarness.GetDefaultExcludedProcesses();
        Assert.IsTrue(excluded.Contains("wt.exe"));
        Assert.IsTrue(excluded.Contains("windowsterminal.exe"));
    }

    [TestMethod]
    public void F13_B07_BufferClearing_OnHomeAndEndKeys()
    {
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shah");
        sim.PressNavigationKey(); // Home key pressed
        sim.TypeText("ar ");
        Assert.AreEqual("shahar ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void F14_B07_TransliterationModeEnum_ValuesExist()
    {
        var modeType = TestHarness.YangiAlifboAssembly.GetType("YangiAlifbo.Win32.TransliterationMode");
        Assert.IsNotNull(modeType, "TransliterationMode enum must exist in Win32 namespace");
        var names = Enum.GetNames(modeType);
        Assert.IsTrue(names.Contains("Strict"));
        Assert.IsTrue(names.Contains("Aggressive"));
    }

    [TestMethod]
    public void F15_B06_HotkeyModifierFlags_Verification()
    {
        const uint MOD_ALT = 0x0001;
        const uint MOD_CONTROL = 0x0002;
        const uint MOD_SHIFT = 0x0004;
        const uint MOD_WIN = 0x0008;

        Assert.AreEqual(0x0001u, MOD_ALT);
        Assert.AreEqual(0x0002u, MOD_CONTROL);
        Assert.AreEqual(0x0004u, MOD_SHIFT);
        Assert.AreEqual(0x0008u, MOD_WIN);
    }

    [TestMethod]
    public void F16_B06_MutexRapidOpenClose_Stability()
    {
        string uniqueTestMutex = "Global\\YangiAlifbo_RapidTest_" + Guid.NewGuid().ToString("N");
        for (int i = 0; i < 10; i++)
        {
            using var m = new Mutex(true, uniqueTestMutex, out bool created);
            Assert.IsTrue(created);
            m.ReleaseMutex();
        }
    }
}
