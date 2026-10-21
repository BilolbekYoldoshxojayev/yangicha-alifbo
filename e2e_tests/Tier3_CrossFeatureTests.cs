// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.E2ETests — Tier3_CrossFeatureTests.cs
// Tier 3: Cross-Feature Combinations (>= 16 pairwise combinations)
// Total cases: 25 pairwise and combinatorial feature interactions.
// ──────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;

namespace YangiAlifbo.E2ETests;

[TestClass]
public class Tier3_CrossFeatureTests
{
    private static LanguageDetector _detector = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        _detector = new LanguageDetector();
    }

    [TestMethod]
    public void P01_ApostropheVariant_TitleCase_Space()
    {
        // Feature 1 (o' -> ö) + Feature 2 (Modifier Turned Comma U+02BB) + Feature 3 (TitleCase) + Feature 9 (Space)
        var sim = new TypingSimulator(_detector);
        sim.TypeText("Oʻrtoq ");
        Assert.AreEqual("Örtoq ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P02_ApostropheVariant_AllCaps_Exclamation()
    {
        // Feature 1 (g' -> ğ) + Feature 2 (Left quote U+2018) + Feature 3 (ALL CAPS) + Feature 9 (Exclamation)
        var sim = new TypingSimulator(_detector);
        sim.TypeText("G‘ALABA! ");
        Assert.AreEqual("ĞALABA! ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P03_AgglutinativeSuffix_ApostropheVariant_Ablative()
    {
        // Feature 1 + Feature 2 + Feature 7 (Agglutinative Plural + Ablative) + Feature 9
        var sim = new TypingSimulator(_detector);
        sim.TypeText("gʻalabalardan! ");
        Assert.AreEqual("ğalabalardan! ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P04_SyllableBreakProtection_AllCaps_Comma()
    {
        // Feature 3 (ALL CAPS) + Feature 4 (Syllable break Is'hoq) + Feature 9 (Comma)
        var sim = new TypingSimulator(_detector);
        sim.TypeText("IS'HOQ, ");
        Assert.AreEqual("IS'HOQ, ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P05_SyllableBreakProtection_TitleCase_Affix_Question()
    {
        // Feature 3 (TitleCase) + Feature 4 (Syllable break) + Feature 7 (Genitive suffix) + Feature 9 (Question)
        var sim = new TypingSimulator(_detector);
        sim.TypeText("Is'hoqning? ");
        Assert.AreEqual("Is'hoqning? ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P06_EnglishReject_StrictMode_SpaceDelimiter()
    {
        // Feature 5 (Negative filter) + Feature 9 (Space delimiter) + Feature 14 (Strict mode)
        var sim = new TypingSimulator(_detector) { Mode = SimulationMode.Strict };
        sim.TypeText("switch ");
        Assert.AreEqual("switch ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P07_EnglishReject_AggressiveMode_SpaceDelimiter()
    {
        // Feature 5 + Feature 9 (Space delimiter) + Feature 14 (Aggressive mode)
        var sim = new TypingSimulator(_detector) { Mode = SimulationMode.Aggressive };
        sim.TypeText("switch ");
        // In aggressive mode, dictionary filter is bypassed: switch -> switç
        Assert.AreEqual("switç ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P08_UserBlacklist_UzbekLexicon_EnterDelimiter()
    {
        // Feature 6 (Uzbek word) + Feature 8 (Blacklist override) + Feature 9 (Enter delimiter)
        var black = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shahar" };
        var detectorWithBlack = new LanguageDetector(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shahar" },
            userBlacklist: black);

        var sim = new TypingSimulator(detectorWithBlack);
        sim.TypeText("shahar\n");
        Assert.AreEqual("shahar\n", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P09_UserWhitelist_EnglishWord_TabDelimiter()
    {
        // Feature 5 (English word) + Feature 8 (Whitelist override) + Feature 9 (Tab delimiter)
        var white = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "show" };
        var detectorWithWhite = new LanguageDetector(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "show" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            userWhitelist: white);

        var sim = new TypingSimulator(detectorWithWhite);
        sim.TypeText("show\t");
        Assert.AreEqual("şow\t", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P10_BackspaceEditing_UzbekWord_Casing()
    {
        // Feature 3 (Casing) + Feature 6 (Uzbek word) + Feature 13 (Backspace editing)
        var sim = new TypingSimulator(_detector);
        sim.TypeText("Shax");
        sim.PressBackspace(); // Buffer: 'Sha'
        sim.TypeText("har ");  // Buffer: 'Shahar'
        Assert.AreEqual("Şahar ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P11_NavigationKey_PartialUzbekWord_Punctuation()
    {
        // Feature 6 (Uzbek word) + Feature 9 (Period) + Feature 13 (Navigation key)
        var sim = new TypingSimulator(_detector);
        sim.TypeText("sha");
        sim.PressNavigationKey(); // Caret moved
        sim.TypeText("har.");     // Only 'har' buffered
        Assert.AreEqual("shahar.", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P12_ProcessExclusion_Terminal_UzbekWord()
    {
        // Feature 6 (Uzbek word) + Feature 11 (Process exclusion: cmd.exe)
        var sim = new TypingSimulator(_detector);
        sim.SwitchWindow("cmd.exe");
        sim.TypeText("shahar o'rtoq ");
        Assert.AreEqual("shahar o'rtoq ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P13_ProcessExclusion_IDE_AggressiveMode()
    {
        // Feature 11 (Process exclusion: code.exe) + Feature 14 (Aggressive mode)
        var sim = new TypingSimulator(_detector) { Mode = SimulationMode.Aggressive };
        sim.SwitchWindow("code.exe");
        sim.TypeText("shahar choyxona ");
        Assert.AreEqual("shahar choyxona ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P14_PasswordField_ApostropheVariant_Agglutinative()
    {
        // Feature 2 (Apostrophe) + Feature 7 (Agglutinative) + Feature 12 (Password field)
        var sim = new TypingSimulator(_detector);
        sim.SwitchWindow("browser.exe", isPassword: true);
        sim.TypeText("oʻrtoqlarimiz123! ");
        Assert.AreEqual("oʻrtoqlarimiz123! ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P15_DisabledEngine_UzbekWord_AllDelimiters()
    {
        // Feature 9 (Delimiters) + Feature 14 (IsEnabled = false)
        var sim = new TypingSimulator(_detector) { IsEnabled = false };
        sim.TypeText("shahar; choyxona: o'rtoq- ");
        Assert.AreEqual("shahar; choyxona: o'rtoq- ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P16_MultipleDigraphs_ChainedSuffixes_Parentheses()
    {
        // Feature 1 (sh+ch) + Feature 7 (Agglutinative) + Feature 9 (Parentheses)
        var sim = new TypingSimulator(_detector);
        sim.TypeText("(shoshilinch) ");
        Assert.AreEqual("(şoşilinç) ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P17_TitleCase_GraveAccent_PluralDative()
    {
        // Feature 2 (Grave accent) + Feature 3 (TitleCase) + Feature 7 (Plural+Dative)
        var sim = new TypingSimulator(_detector);
        sim.TypeText("G`alabalarga ");
        Assert.AreEqual("Ğalabalarga ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P18_ChainedPossessiveCase_RightSingleQuote()
    {
        // Feature 2 (Right quote) + Feature 7 (Plural + Possessive + Case)
        var sim = new TypingSimulator(_detector);
        sim.TypeText("o’rtoqlarimizga ");
        Assert.AreEqual("örtoqlarimizga ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P19_RapidBurst_MultipleApostropheVariants_MixedSentence()
    {
        // Feature 1 + Feature 2 + Feature 10 (Latency)
        var sim = new TypingSimulator(_detector);
        sim.TypeText("o'rtoq oʻzbek g'alaba g`urur ");
        Assert.AreEqual("örtoq özbek ğalaba ğurur ", sim.GetScreenText());
        Assert.AreEqual(4, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P20_WindowSwitch_DuringWordEntry_DifferentDelimiters()
    {
        // Feature 9 + Feature 11 + Feature 13 (Window switch)
        var sim = new TypingSimulator(_detector);
        sim.TypeText("o'");
        sim.SwitchWindow("browser.exe");
        sim.TypeText("rtoq; ");
        Assert.AreEqual("o'rtoq; ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P21_MouseClick_DuringAgglutinativeWord_Space()
    {
        // Feature 7 (Agglutinative) + Feature 13 (Mouse click)
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shaharlar");
        sim.ClickMouse();
        sim.TypeText("da ");
        Assert.AreEqual("shaharlarda ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P22_HotkeyToggle_MidSentence_MixedOutput()
    {
        // Feature 1 + Feature 14 + Feature 15 (Hotkey toggle)
        var sim = new TypingSimulator(_detector);
        sim.TypeText("shahar "); // Active -> converted
        Assert.AreEqual("şahar ", sim.GetScreenText());

        sim.IsEnabled = false;   // Hotkey toggled OFF
        sim.TypeText("choyxona ");
        Assert.AreEqual("şahar choyxona ", sim.GetScreenText());

        sim.IsEnabled = true;    // Hotkey toggled ON
        sim.TypeText("o'rtoq ");
        Assert.AreEqual("şahar choyxona örtoq ", sim.GetScreenText());
        Assert.AreEqual(2, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P23_SingleInstanceMutex_WithSimulatedTyping()
    {
        // Feature 1 + Feature 16 (Single instance mutex)
        string testMutex = "Global\\YangiAlifbo_P23_" + Guid.NewGuid().ToString("N");
        using var mutex = new Mutex(true, testMutex, out bool created);
        Assert.IsTrue(created);

        var sim = new TypingSimulator(_detector);
        sim.TypeText("g'alaba ");
        Assert.AreEqual("ğalaba ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P24_AggressiveMode_WithSyllableBreak_Protected()
    {
        // Feature 4 (Syllable break) + Feature 14 (Aggressive mode)
        var sim = new TypingSimulator(_detector) { Mode = SimulationMode.Aggressive };
        sim.TypeText("mus'haf ");
        // Even in Aggressive mode, syllable break rule must prevent corrupting mus'haf
        Assert.AreEqual("mus'haf ", sim.GetScreenText());
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    [TestMethod]
    public void P25_ComplexAgglutinativeAlternation_DativeApostrophe()
    {
        // Feature 1 (ch + o') + Feature 7 (Agglutinative) + Feature 9
        var sim = new TypingSimulator(_detector);
        sim.TypeText("cho'qqilarga ");
        Assert.AreEqual("çöqqilarga ", sim.GetScreenText());
        Assert.AreEqual(1, sim.WordsConvertedCount);
    }
}
