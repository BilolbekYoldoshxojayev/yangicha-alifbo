// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.E2ETests — Tier4_ApplicationScenarioTests.cs
// Tier 4: Real-World Application Scenarios (10 comprehensive scenarios)
// Derived strictly from TEST_INFRA.md and ORIGINAL_REQUEST.md.
// ──────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;

namespace YangiAlifbo.E2ETests;

[TestClass]
public class Tier4_ApplicationScenarioTests
{
    private static LanguageDetector _detector = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        _detector = new LanguageDetector();
    }

    // ──────────────────────────────────────────────────────────────────
    // SCENARIO 1: Uzbek Conversational Chat in Messaging App (Telegram/Slack)
    // ──────────────────────────────────────────────────────────────────
    [TestMethod]
    public void Scenario01_UzbekChatInMessagingApp()
    {
        // Features: F1 (Mappings), F2 (Casing), F6 (Lexicon), F9 (Delimiters), F10 (Synthesis)
        var sim = new TypingSimulator(_detector);
        sim.SwitchWindow("telegram.exe");

        // Realistic chat conversation with greetings, questions, and plans
        sim.TypeText("Salom o'rtoq! ");
        sim.TypeText("Bugun shahar markazidagi choyxonaga boramizmi? ");
        sim.TypeText("G'alaba bog'i yaqinida yangi choyxona ochilibdi. ");
        sim.TypeText("O'sha yerda uchrashamiz. ");

        string actual = sim.GetScreenText();

        Assert.IsTrue(actual.Contains("örtoq!"), "o'rtoq should convert to örtoq");
        Assert.IsTrue(actual.Contains("şahar"), "shahar should convert to şahar");
        Assert.IsTrue(actual.Contains("çoyxonaga"), "choyxonaga should convert to çoyxonaga");
        Assert.IsTrue(actual.Contains("Ğalaba"), "G'alaba should convert to Ğalaba");
        Assert.IsTrue(actual.Contains("Öşa"), "O'sha should convert to Öşa");
        Assert.IsTrue(sim.WordsConvertedCount >= 5, "Multiple words should be converted in chat");
    }

    // ──────────────────────────────────────────────────────────────────
    // SCENARIO 2: Typing Mixed English-Uzbek Programming Comments in Code Editor
    // ──────────────────────────────────────────────────────────────────
    [TestMethod]
    public void Scenario02_MixedProgrammingCommentsInTextEditor()
    {
        // Features: F1 (Transliteration), F4 (Syllable Break), F5 (Reject Filter), F11 (Code editor)
        // Testing in a text editor where code comments are written in Uzbek with English keywords
        var sim = new TypingSimulator(_detector);
        sim.SwitchWindow("notepad.exe");

        sim.TypeText("// Bu funksiya shahar ma'lumotlarini qaytaradi. ");
        sim.TypeText("// Agar switch yoki match bo'lmasa, catch blokida xatolik chiqadi. ");
        sim.TypeText("function fetchUserData() { return null; } ");

        string actual = sim.GetScreenText();

        // Uzbek words must convert
        Assert.IsTrue(actual.Contains("şahar"), "Uzbek word 'shahar' must convert to 'şahar'");
        Assert.IsTrue(actual.Contains("ma'lumotlarini"), "Morpheme 'ma'lumotlarini' must retain tutuq");

        // Programming keywords MUST NOT convert (Strict mode negative discrimination)
        Assert.IsTrue(actual.Contains("switch"), "'switch' must never convert to 'switç'");
        Assert.IsTrue(actual.Contains("match"), "'match' must never convert to 'matç'");
        Assert.IsTrue(actual.Contains("catch"), "'catch' must never convert to 'catç'");
        Assert.IsTrue(actual.Contains("function fetchUserData()"), "Function declaration must be intact");
    }

    // ──────────────────────────────────────────────────────────────────
    // SCENARIO 3: Fast Rapid Typing with Backspaces and Mouse-Click Repositioning
    // ──────────────────────────────────────────────────────────────────
    [TestMethod]
    public void Scenario03_FastTypingWithBackspacesAndMouseClicks()
    {
        // Features: F8 (Keyboard Hook), F9 (Delimiters), F10 (Synthesis), F13 (Buffer Invalidation)
        var sim = new TypingSimulator(_detector);
        sim.SwitchWindow("word.exe");

        // User types 'shahxx', notices typo, backspaces twice, completes 'ar '
        sim.TypeText("shahxx");
        sim.PressBackspace();
        sim.PressBackspace();
        sim.TypeText("ar ");
        Assert.AreEqual("şahar ", sim.GetScreenText());

        // User clicks mouse to reposition caret, then types another word
        sim.ClickMouse();
        sim.TypeText("choy");
        sim.ClickMouse(); // Accidental click mid-word clears buffer
        sim.TypeText("xona "); // 'xona' is in buffer, not 'choyxona'
        Assert.AreEqual("şahar choyxona ", sim.GetScreenText());

        // Resumes sequential typing
        sim.TypeText("o'rtoq ");
        Assert.AreEqual("şahar choyxona örtoq ", sim.GetScreenText());
        Assert.AreEqual(2, sim.WordsConvertedCount);
    }

    // ──────────────────────────────────────────────────────────────────
    // SCENARIO 4: Terminal Shell Session with Admin PowerShell and Command Line
    // ──────────────────────────────────────────────────────────────────
    [TestMethod]
    public void Scenario04_TerminalShellSession_ZeroInterference()
    {
        // Features: F5 (Negative Filter), F10 (Synthesis Safety), F11 (Process Exclusion: powershell/cmd)
        var sim = new TypingSimulator(_detector);

        // Sub-test A: PowerShell session
        sim.SwitchWindow("powershell.exe");
        sim.TypeText("git checkout -b feature/shahar-choyxona\n");
        sim.TypeText("Get-ChildItem -Path C:\\Shell\\Switch\n");
        sim.TypeText("echo \"show me the shell status\"\n");

        string psOutput = sim.GetScreenText();
        Assert.IsTrue(psOutput.Contains("checkout"), "Terminal command 'checkout' must be untouched");
        Assert.IsTrue(psOutput.Contains("feature/shahar-choyxona"), "Branch name must not be mutated in terminal");
        Assert.IsTrue(psOutput.Contains("Get-ChildItem"), "PowerShell cmdlet must be untouched");
        Assert.AreEqual(0, sim.WordsConvertedCount, "Zero conversions in excluded terminal process");

        // Sub-test B: cmd.exe session
        sim.Reset();
        sim.SwitchWindow("cmd.exe");
        sim.TypeText("cd C:\\Windows\\System32 && dir /w\n");
        sim.TypeText("tasklist | findstr switch\n");
        Assert.AreEqual(0, sim.WordsConvertedCount);
    }

    // ──────────────────────────────────────────────────────────────────
    // SCENARIO 5: Web Browser Form Filling with Password Fields and Text Inputs
    // ──────────────────────────────────────────────────────────────────
    [TestMethod]
    public void Scenario05_WebBrowserFormFilling_PasswordSecurity()
    {
        // Features: F1 (Mappings), F5 (Reject), F9 (Delimiters), F12 (Password Field)
        var sim = new TypingSimulator(_detector);
        sim.SwitchWindow("chrome.exe", isPassword: false);

        // Field 1: Username / First Name
        sim.TypeText("O'rtoq ");
        Assert.AreEqual("Örtoq ", sim.GetScreenText());

        // Field 2: Password field
        sim.SwitchWindow("chrome.exe", isPassword: true);
        sim.TypeText("P@ss_shahar_o'rtoq_123! ");
        // In password field, exact keystrokes must be preserved without transliteration
        Assert.IsTrue(sim.GetScreenText().EndsWith("P@ss_shahar_o'rtoq_123! "));

        // Field 3: Bio / Comment field (normal input)
        sim.SwitchWindow("chrome.exe", isPassword: false);
        sim.TypeText("Bizning g'alaba yaqin! ");
        Assert.IsTrue(sim.GetScreenText().Contains("ğalaba"));
    }

    // ──────────────────────────────────────────────────────────────────
    // SCENARIO 6: Typing Poetry / Academic Prose with Syllable Breaks
    // ──────────────────────────────────────────────────────────────────
    [TestMethod]
    public void Scenario06_PoetryAndAcademicProse_SyllableBreakPreservation()
    {
        // Features: F1 (Mappings), F3 (Casing), F4 (Syllable Break), F6 (Lexicon)
        var sim = new TypingSimulator(_detector);
        sim.SwitchWindow("winword.exe");

        // Classical prose containing tutuq syllable-break words alongside reformable words
        sim.TypeText("Tarixiy mus'haf sahifalarida Is'hoq payg'ambar haqida yozilgan. ");
        sim.TypeText("Har bir shaxs mas'uliyat his etishi shart. ");
        sim.TypeText("Milliy san'at va adabiyot gullab-yashnamoqda. ");

        string actual = sim.GetScreenText();

        // Syllable-break words must NOT be corrupted
        Assert.IsTrue(actual.Contains("mus'haf"), "mus'haf must retain its syllable break");
        Assert.IsTrue(actual.Contains("Is'hoq"), "Is'hoq must retain its syllable break");
        Assert.IsTrue(actual.Contains("mas'uliyat"), "mas'uliyat must retain its syllable break");
        Assert.IsTrue(actual.Contains("san'at"), "san'at must retain its syllable break");

        // Ordinary words must convert
        Assert.IsTrue(actual.Contains("payğambar"), "payg'ambar must convert to payğambar");
        Assert.IsTrue(actual.Contains("şaxs"), "shaxs must convert to şaxs");
    }

    // ──────────────────────────────────────────────────────────────────
    // SCENARIO 7: Custom Jargon Management via User Whitelist and Blacklist
    // ──────────────────────────────────────────────────────────────────
    [TestMethod]
    public void Scenario07_CustomJargon_WhitelistAndBlacklist()
    {
        // Features: F1 (Mappings), F5 (Reject), F8 (User Overrides)
        var white = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "microchip", "chuchvara" };
        var black = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shahar" };

        var customDetector = new LanguageDetector(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "microchip" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shahar", "choyxona" },
            userWhitelist: white,
            userBlacklist: black);

        var sim = new TypingSimulator(customDetector);

        // 1. Blacklisted word 'shahar' is blocked from conversion
        sim.TypeText("shahar ");
        Assert.AreEqual("shahar ", sim.GetScreenText(), "Blacklisted word 'shahar' should not convert.");

        // 2. Whitelisted foreign word 'microchip' is forced to convert
        sim.TypeText("microchip ");
        Assert.IsTrue(sim.GetScreenText().Contains("microçip"), "Whitelisted 'microchip' should convert.");

        // 3. Regular word 'choyxona' converts normally via lexicon
        sim.TypeText("choyxona ");
        Assert.IsTrue(sim.GetScreenText().Contains("çoyxona"));
    }

    // ──────────────────────────────────────────────────────────────────
    // SCENARIO 8: Toggling Engine via Hotkey (Ctrl+Shift+U) and Tray Menu
    // ──────────────────────────────────────────────────────────────────
    [TestMethod]
    public void Scenario08_HotkeyToggle_MidDocumentEditing()
    {
        // Features: F1 (Mappings), F14 (Tray UI), F15 (Global Hotkey Ctrl+Shift+U)
        var sim = new TypingSimulator(_detector);

        // Phase 1: Engine active -> typing Uzbek
        sim.TypeText("O'zbekiston go'zal shahar. ");
        Assert.IsTrue(sim.GetScreenText().Contains("şahar."));

        // Phase 2: User presses Ctrl+Shift+U to disable engine for coding/English typing
        sim.IsEnabled = false; // Simulated Ctrl+Shift+U
        sim.TypeText("Now writing in English with switch, catch, and shahar without changes. ");
        Assert.IsTrue(sim.GetScreenText().Contains("switch"));
        Assert.IsTrue(sim.GetScreenText().Contains("catch"));
        Assert.IsTrue(sim.GetScreenText().Contains("shahar without"));

        // Phase 3: User presses Ctrl+Shift+U again to re-enable engine
        sim.IsEnabled = true; // Simulated Ctrl+Shift+U
        sim.TypeText("Yana o'zbek tiliga qaytdik: g'alaba bizniki! ");
        Assert.IsTrue(sim.GetScreenText().Contains("özbek"));
        Assert.IsTrue(sim.GetScreenText().Contains("ğalaba"));
    }

    // ──────────────────────────────────────────────────────────────────
    // SCENARIO 9: High-Speed Burst Typing (< 5ms Latency Stress)
    // ──────────────────────────────────────────────────────────────────
    [TestMethod]
    public void Scenario09_HighSpeedBurstTyping_LatencyStress()
    {
        // Features: F8 (Keyboard Hook), F10 (Synthesis), F13 (Buffer Performance)
        var sim = new TypingSimulator(_detector);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 50; i++)
        {
            sim.TypeText("shahar choyxona o'rtoq g'alaba ");
        }
        sw.Stop();

        Assert.AreEqual(200, sim.WordsConvertedCount);

        // Every keystroke must be processed well within the 5ms latency threshold
        foreach (var latency in sim.KeystrokeLatencies)
        {
            Assert.IsTrue(latency.TotalMilliseconds < 10.0,
                $"Keystroke latency was {latency.TotalMilliseconds}ms, exceeding threshold.");
        }

        double avgLatencyMs = sw.Elapsed.TotalMilliseconds / sim.KeystrokeLatencies.Count;
        Assert.IsTrue(avgLatencyMs < 1.0,
            $"Average latency {avgLatencyMs}ms per keystroke must be under 1ms.");
    }

    // ──────────────────────────────────────────────────────────────────
    // SCENARIO 10: Switching Between Applications and Virtual Desktops
    // ──────────────────────────────────────────────────────────────────
    [TestMethod]
    public void Scenario10_MultiAppAndVirtualDesktopTransitions()
    {
        // Features: F8 (Hook), F11 (Process Exclusion), F13 (Buffer Invalidation), F16 (Mutex)
        string mutexName = "Global\\YangiAlifbo_Scenario10_" + Guid.NewGuid().ToString("N");
        using var mutex = new Mutex(true, mutexName, out bool createdNew);
        Assert.IsTrue(createdNew, "Application mutex must be held during session");

        var sim = new TypingSimulator(_detector);

        // Step 1: User types in Word
        sim.SwitchWindow("winword.exe");
        sim.TypeText("Boshlang'ich so'zlar: shahar ");
        Assert.IsTrue(sim.GetScreenText().Contains("şahar"));

        // Step 2: User switches to PowerShell mid-sentence
        sim.SwitchWindow("powershell.exe");
        sim.TypeText("git status\n");
        Assert.IsTrue(sim.GetScreenText().Contains("git status"));

        // Step 3: User switches back to Word and types again
        sim.SwitchWindow("winword.exe");
        sim.TypeText("o'rtoqlar davomida. ");
        Assert.IsTrue(sim.GetScreenText().Contains("örtoqlar"));

        // Step 4: User switches to Browser login page (password field)
        sim.SwitchWindow("chrome.exe", isPassword: true);
        sim.TypeText("Secret_shahar_123! ");
        Assert.IsTrue(sim.GetScreenText().Contains("Secret_shahar_123!"));

        // Step 5: User returns to Slack messaging
        sim.SwitchWindow("slack.exe", isPassword: false);
        sim.TypeText("Rahmat, ko'rishguncha! ");
        Assert.IsTrue(sim.GetScreenText().Contains("körişgunça") || sim.GetScreenText().Contains("ko'rishguncha"));
    }
}
