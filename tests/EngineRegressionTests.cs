// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.Tests — EngineRegressionTests.cs
// Engine bugs found by running the standard dictionary through the engine
// (.ai/PLAN_engine.md). Each group proves one fix.
// ──────────────────────────────────────────────────────────────────────

using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;
using YangiAlifbo.Win32;

namespace YangiAlifbo.Tests;

[TestClass]
public class EngineRegressionTests
{
    private static readonly LanguageDetector D = new();

    private static string? Word(string w) => WordConverter.Convert(D, w, TransliterationMode.Strict, true);

    // ── Step 1: slang rules must not corrupt standard words ─────────────

    [DataTestMethod]
    [DataRow("organik", null)]
    [DataRow("organizm", null)]
    [DataRow("organizatsiya", null)]
    [DataRow("ozgina", null)]
    [DataRow("ortiq", null)]
    [DataRow("ortiqcha", "ortiqça")]
    [DataRow("qosh", "qoş")]
    [DataRow("oshamoq", "oşamoq")]
    [DataRow("oshatmoq", "oşatmoq")]
    [DataRow("zor-intizor", null)]
    [DataRow("zor-mushtoq", "zor-muştoq")]
    public void StandardWord_IsNotRewrittenAsSlang(string word, string? expected)
        => Assert.AreEqual(expected, Word(word));

    [DataTestMethod]
    [DataRow("zor", "zör")]
    [DataRow("yoq", "yöq")]
    [DataRow("soz", "söz")]
    [DataRow("osha", "öşa")]
    [DataRow("organish", "örganiş")]
    [DataRow("organdim", "örgandim")]
    [DataRow("qoshdim", "qöşdim")]
    [DataRow("ortada", "örtada")]
    [DataRow("kordm", "kördim")]
    public void ChatForms_StillConvert(string word, string expected)
        => Assert.AreEqual(expected, Word(word));

    /// <summary>
    /// Standard words that chat writes far more often for another word; the
    /// slang reading wins there (yoq = yo'q, zor = zo'r, bolish = bo'lish …).
    /// The zo'r- words are in the dictionary without the tutuq by mistake.
    /// </summary>
    private static readonly HashSet<string> ChatWins = new(StringComparer.Ordinal)
    {
        "bol", "kor", "yol", "yoq", "zor", "soz", "osha", "bolish", "otish", "oylab",
        "zora", "zoraki", "zoriqmoq", "zorlanmoq", "zorlik", "zormanda", "zor-zor", "zora-mora",
    };

    [TestMethod]
    public void StandardDictionary_IsConvertedAsTyped()
    {
        var bad = new List<string>();
        foreach (string w in StandardWords())
        {
            if (ChatWins.Contains(w)) continue;
            string? expected = UzbekPhonetics.HasConvertiblePatterns(w) ? UzbekPhonetics.Convert(w) : null;
            string? got = Word(w);
            if (got != null && got != expected) bad.Add($"{w} -> {got}");
        }
        Assert.AreEqual(0, bad.Count, string.Join(", ", bad.Take(40)));
    }

    // ── Step 2: Uzbek compounds with an English-looking half ───────────

    [DataTestMethod]
    [DataRow("gap-so'z", "gap-söz")]
    [DataRow("boy-bo'y", "boy-böy")]
    [DataRow("his-tuyg'u", "his-tuyğu")]
    [DataRow("osh-non", "oş-non")]
    [DataRow("it-mushuk", "it-muşuk")]
    [DataRow("op-ochiq", "op-oçiq")]
    [DataRow("dod-fig'on", "dod-fiğon")]
    [DataRow("chop-chop", "çop-çop")]
    [DataRow("shop-shalop", "şop-şalop")]
    public void UzbekCompound_WithEnglishLookingHalf_Converts(string word, string expected)
        => Assert.AreEqual(expected, Word(word));

    [DataTestMethod]
    [DataRow("e-mail")]
    [DataRow("show-biznes")]
    [DataRow("fresh-juice")]
    [DataRow("push-up")]
    public void ForeignCompound_StaysProtected(string word)
        => Assert.IsNull(Word(word));

    // ── Step 3: « » “ ” „ … end a word, as on Linux and macOS ──────────

    [DataTestMethod]
    [DataRow("«shahar»", "«şahar»")]
    [DataRow("“shahar”", "“şahar”")]
    [DataRow("„choy“", "„çoy“")]
    [DataRow("shahar… o'zbek", "şahar… özbek")]
    [DataRow("‹o'g'il›", "‹öğil›")]
    [DataRow("shahar$", "şahar$")]
    [DataRow("5-chi «o'rin»", "5-çi «örin»")]
    public void TextConverter_ConvertsWordsNextToAnyPunctuation(string input, string expected)
        => Assert.AreEqual(expected, TextConverter.Convert(input, D, TransliterationMode.Strict, true, out _));

    [DataTestMethod]
    [DataRow('»', true)]
    [DataRow('«', true)]
    [DataRow('”', true)]
    [DataRow('„', true)]
    [DataRow('…', true)]
    [DataRow('$', true)]
    [DataRow('a', false)]
    [DataRow('ş', false)]
    [DataRow('5', false)]
    [DataRow('\'', false)]
    [DataRow('’', false)]
    [DataRow('ʻ', false)]
    [DataRow('\b', false)]
    [DataRow('\u001b', false)]
    public void KeyboardHook_EndsWordOnAnyPrintablePunctuation(char ch, bool isEnd)
    {
        var m = typeof(KeyboardHook).GetMethod("IsDelimiter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        Assert.AreEqual(isEnd, (bool)m.Invoke(null, new object[] { ch, 0 })!);
    }

    // ── Step 4: a quoted word keeps its final tutuq ─────────────────────

    [DataTestMethod]
    [DataRow("'tog''", "'toğ'")]
    [DataRow("‘bog'’", "‘boğ’")]
    [DataRow("'o'z'", "'öz'")]
    [DataRow("'shahar'", "'şahar'")]
    [DataRow("tog'", "toğ")]
    [DataRow("o'z'", "öz'")]
    public void QuotedWord_KeepsItsTutuq(string word, string expected)
        => Assert.AreEqual(expected, Word(word));

    // ── Step 5: the user's apostrophe character is kept ─────────────────

    [DataTestMethod]
    [DataRow("ta’sirchan", "ta’sirçan")]
    [DataRow("taʼsirchan", "taʼsirçan")]
    [DataRow("Is’hoq shahar", "Is’hoq şahar")]
    [DataRow("mas’uliyatli o‘zbek", "mas’uliyatli özbek")]
    [DataRow("san`at shahri", "san`at şahri")]
    public void TutuqCharacter_IsKept(string input, string expected)
    {
        Assert.AreEqual(expected, TextConverter.Convert(input, D, TransliterationMode.Strict, true, out _));
        Assert.AreEqual(expected, UzbekPhonetics.Convert(input));
    }

    [TestMethod]
    public void KeyboardHook_BuffersTheApostropheAsTyped()
    {
        string src = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Win32", "KeyboardHook.cs"));
        Assert.IsFalse(src.Contains("_wordBuffer.Append('\\'')"), "the hook must keep the typed apostrophe character");
    }

    // ── Step 6: frequent chat forms from the Telegram corpus ────────────

    [DataTestMethod]
    [DataRow("kere", "kerak")]
    [DataRow("kerek", "kerak")]
    [DataRow("keremi", "kerakmi")]
    [DataRow("keyn", "keyin")]
    [DataRow("kegin", "keyin")]
    [DataRow("buldi", "böldi")]
    [DataRow("bulsa", "bölsa")]
    [DataRow("busa", "bölsa")]
    [DataRow("buladimi", "böladimi")]
    [DataRow("bogan", "bölgan")]
    [DataRow("bomidi", "bölmaydi")]
    [DataRow("uzi", "özi")]
    [DataRow("uzim", "özim")]
    [DataRow("zur", "zör")]
    [DataRow("tugri", "töğri")]
    [DataRow("kup", "köp")]
    [DataRow("kumir", "kömir")]
    [DataRow("kotta", "katta")]
    [DataRow("royxatdan", "röyxatdan")]
    [DataRow("ruyxatdan", "röyxatdan")]
    [DataRow("ruyhatdan", "röyxatdan")]
    [DataRow("oquv", "öquv")]
    [DataRow("talim", "ta'lim")]
    [DataRow("malumot", "ma'lumot")]
    [DataRow("etibor", "e'tibor")]
    [DataRow("yani", "ya'ni")]
    [DataRow("bazi", "ba'zi")]
    [DataRow("maqul", "ma'qul")]
    [DataRow("xozir", "hozir")]
    [DataRow("hozi", "hozir")]
    [DataRow("xamma", "hamma")]
    [DataRow("hursand", "xursand")]
    [DataRow("hohlasa", "xohlasa")]
    [DataRow("xoxlasa", "xohlasa")]
    [DataRow("habar", "xabar")]
    [DataRow("huddi", "xuddi")]
    [DataRow("hullas", "xullas")]
    [DataRow("suxbat", "suhbat")]
    [DataRow("nme", "nima")]
    [DataRow("qande", "qanday")]
    [DataRow("lkn", "lekin")]
    [DataRow("szga", "sizga")]
    [DataRow("qalesan", "qalaysan")]
    [DataRow("qalesiz", "qalaysiz")]
    [DataRow("sog", "soğ")]
    [DataRow("mablag", "mablağ")]
    [DataRow("qib", "qilib")]
    [DataRow("Kere", "Kerak")]
    [DataRow("XOZIR", "HOZIR")]
    public void FrequentChatForm_IsNormalized(string word, string expected)
        => Assert.AreEqual(expected, Word(word));

    // ── Helpers ─────────────────────────────────────────────────────────

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "YangiAlifbo.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }

    private static readonly Dictionary<char, string> Cyr = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['ж'] = "j", ['з'] = "z",
        ['и'] = "i", ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n", ['о'] = "o",
        ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u", ['ф'] = "f", ['х'] = "x",
        ['ч'] = "ch", ['ш'] = "sh", ['ъ'] = "'", ['ь'] = "", ['э'] = "e", ['ю'] = "yu", ['я'] = "ya",
        ['ё'] = "yo", ['ў'] = "o'", ['қ'] = "q", ['ғ'] = "g'", ['ҳ'] = "h", ['ц'] = "ts", ['щ'] = "sh",
        ['ы'] = "i", ['-'] = "-",
    };

    /// <summary>The standard Cyrillic dictionary in 1995 Latin spelling.</summary>
    internal static IEnumerable<string> StandardWords()
    {
        string path = Path.Combine(RepoRoot(), "src", "YangiAlifbo.Core", "Dictionaries", "uzwords_raw.txt");
        var seen = new HashSet<string>();
        foreach (string line in File.ReadLines(path, Encoding.UTF8))
        {
            string w = line.Trim().TrimStart('﻿').ToLowerInvariant();
            if (w.Length < 2 || w.Contains(' ')) continue;
            var sb = new StringBuilder();
            bool ok = true;
            for (int i = 0; i < w.Length && ok; i++)
            {
                char c = w[i];
                if (c == 'е') sb.Append(i == 0 || "аеёиоуўэюяъ".Contains(w[i - 1]) ? "ye" : "e");
                else if (Cyr.TryGetValue(c, out string? l)) sb.Append(l);
                else ok = false;
            }
            if (ok && seen.Add(sb.ToString())) yield return sb.ToString();
        }
    }
}
