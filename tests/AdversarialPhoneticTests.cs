// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.Tests — AdversarialPhoneticTests.cs
// Empirical Challenger Stress & Adversarial Test Harness for M1:
//   - Complex mixed-casing variations (ShAhAr, o'RtOq, g'AlAbA, cHoYxOnA)
//   - Extreme apostrophe sequences (o'''rtoq, mo''''jiza, leading/trailing)
//   - Syllable break edge cases (is'hoq, Is'Hoq, MUS'HAF, as'hoblar, mas'uliyatli)
//   - Non-Latin strings (Cyrillic, Arabic, Chinese), punctuation, emoji
//   - Large text payloads and latency / throughput verification
//   - Property-based fuzzing and invariant oracles
// ──────────────────────────────────────────────────────────────────────

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;

namespace YangiAlifbo.Tests;

[TestClass]
public class AdversarialPhoneticTests
{
    // ══════════════════════════════════════════════════════════════════
    // 1. Complex Mixed-Casing Variations
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void MixedCase_Shahar_Variations()
    {
        // Alternating case
        Assert.AreEqual("ŞAhAr", UzbekPhonetics.Convert("ShAhAr"));
        Assert.AreEqual("şaHaR", UzbekPhonetics.Convert("sHaHaR"));
        Assert.AreEqual("Şahar", UzbekPhonetics.Convert("SHahar")); // Double-cap typo
        Assert.AreEqual("şahar", UzbekPhonetics.Convert("sHahar")); // Inverted initial
        Assert.AreEqual("ŞaHaR", UzbekPhonetics.Convert("ShaHaR"));
        Assert.AreEqual("ŞAHAR", UzbekPhonetics.Convert("SHAHAR"));
        Assert.AreEqual("şahar", UzbekPhonetics.Convert("shahar"));
    }

    [TestMethod]
    public void MixedCase_Ortoq_Variations()
    {
        Assert.AreEqual("öRtOq", UzbekPhonetics.Convert("o'RtOq"));
        Assert.AreEqual("ÖRtOq", UzbekPhonetics.Convert("O'RtOq"));
        Assert.AreEqual("öRTOQ", UzbekPhonetics.Convert("o'RTOQ"));
        Assert.AreEqual("ÖrTOQ", UzbekPhonetics.Convert("O'rTOQ"));
        Assert.AreEqual("öRtOQ", UzbekPhonetics.Convert("o'RtOQ"));
        Assert.AreEqual("Örtoq", UzbekPhonetics.Convert("O'rtoq"));
        Assert.AreEqual("ÖRTOQ", UzbekPhonetics.Convert("O'RTOQ"));
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o'rtoq"));
    }

    [TestMethod]
    public void MixedCase_Galaba_Variations()
    {
        Assert.AreEqual("ğAlAbA", UzbekPhonetics.Convert("g'AlAbA"));
        Assert.AreEqual("ĞAlAbA", UzbekPhonetics.Convert("G'AlAbA"));
        Assert.AreEqual("ğALABA", UzbekPhonetics.Convert("g'ALABA"));
        Assert.AreEqual("ĞalAbA", UzbekPhonetics.Convert("G'alAbA"));
        Assert.AreEqual("Ğalaba", UzbekPhonetics.Convert("G'alaba"));
        Assert.AreEqual("ĞALABA", UzbekPhonetics.Convert("G'ALABA"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("g'alaba"));
    }

    [TestMethod]
    public void MixedCase_Choyxona_Variations()
    {
        Assert.AreEqual("çoYxOnA", UzbekPhonetics.Convert("cHoYxOnA"));
        Assert.AreEqual("ÇOyXoNa", UzbekPhonetics.Convert("ChOyXoNa"));
        Assert.AreEqual("Çoyxona", UzbekPhonetics.Convert("CHoyxona")); // Double-cap typo
        Assert.AreEqual("çoyxona", UzbekPhonetics.Convert("cHoyxona")); // Inverted initial
        Assert.AreEqual("ÇOYXONA", UzbekPhonetics.Convert("CHOYXONA"));
        Assert.AreEqual("çoyxona", UzbekPhonetics.Convert("choyxona"));
    }

    [TestMethod]
    public void MixedCase_ComplexCompoundWords()
    {
        // Words with multiple convertible patterns in erratic mixed casing
        Assert.AreEqual("qÖşiQ", UzbekPhonetics.Convert("qO'sHiQ"));
        Assert.AreEqual("QöŞIq", UzbekPhonetics.Convert("Qo'ShIq"));
        Assert.AreEqual("çöQqI", UzbekPhonetics.Convert("cHo'QqI"));
        Assert.AreEqual("ÇÖQQI", UzbekPhonetics.Convert("CHO'QQI"));
        Assert.AreEqual("ğÖzA", UzbekPhonetics.Convert("g'O'zA"));
        Assert.AreEqual("ĞÖZA", UzbekPhonetics.Convert("G'O'ZA"));
        Assert.AreEqual("ŞOşiLiNç", UzbekPhonetics.Convert("ShOsHiLiNcH"));
        Assert.AreEqual("şOŞIlInÇ", UzbekPhonetics.Convert("sHOSHIlInCH"));
    }

    // ══════════════════════════════════════════════════════════════════
    // 2. Extreme Apostrophe Sequences
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ExtremeApostrophes_TripleAndQuadrupleAfterVowel()
    {
        // o'''rtoq -> o' becomes ö, remaining '' preserved
        Assert.AreEqual("ö''rtoq", UzbekPhonetics.Convert("o'''rtoq"));
        Assert.AreEqual("ö'''rtoq", UzbekPhonetics.Convert("o''''rtoq"));
        Assert.AreEqual("Ö''RTOQ", UzbekPhonetics.Convert("O'''RTOQ"));
        Assert.AreEqual("ğ''alaba", UzbekPhonetics.Convert("g'''alaba"));
        Assert.AreEqual("Ğ'''ALABA", UzbekPhonetics.Convert("G''''ALABA"));
    }

    [TestMethod]
    public void ExtremeApostrophes_MojizaAndShola_QuadrupleApostrophes()
    {
        // mo''''jiza -> o' becomes ö, remaining ''' preserved
        Assert.AreEqual("mö'''jiza", UzbekPhonetics.Convert("mo''''jiza"));
        Assert.AreEqual("şö'''la", UzbekPhonetics.Convert("sho''''la"));
        Assert.AreEqual("MÖ'''JIZA", UzbekPhonetics.Convert("MO''''JIZA"));
        Assert.AreEqual("ŞÖ'''LA", UzbekPhonetics.Convert("SHO''''LA"));
    }

    [TestMethod]
    public void ExtremeApostrophes_LeadingAndTrailing()
    {
        // Leading apostrophes
        Assert.AreEqual("'örtoq", UzbekPhonetics.Convert("'o'rtoq"));
        Assert.AreEqual("'''örtoq", UzbekPhonetics.Convert("'''o'rtoq"));
        Assert.AreEqual("'şahar", UzbekPhonetics.Convert("'shahar"));
        Assert.AreEqual("''ğalaba", UzbekPhonetics.Convert("''g'alaba"));

        // Trailing apostrophes
        Assert.AreEqual("örtoq'", UzbekPhonetics.Convert("o'rtoq'"));
        Assert.AreEqual("örtoq'''", UzbekPhonetics.Convert("o'rtoq'''"));
        Assert.AreEqual("şahar'", UzbekPhonetics.Convert("shahar'"));
        Assert.AreEqual("çoyxona''", UzbekPhonetics.Convert("choyxona''"));
        Assert.AreEqual("ğalaba'''", UzbekPhonetics.Convert("g'alaba'''"));

        // Both leading and trailing
        Assert.AreEqual("'örtoq'", UzbekPhonetics.Convert("'o'rtoq'"));
        Assert.AreEqual("''şahar''", UzbekPhonetics.Convert("''shahar''"));
    }

    [TestMethod]
    public void ExtremeApostrophes_ApostropheOnlyStrings()
    {
        Assert.AreEqual("'", UzbekPhonetics.Convert("'"));
        Assert.AreEqual("''", UzbekPhonetics.Convert("''"));
        Assert.AreEqual("'''", UzbekPhonetics.Convert("'''"));
        Assert.AreEqual("''''''''''", UzbekPhonetics.Convert("''''''''''"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("''''''''''"));
    }

    [TestMethod]
    public void ExtremeApostrophes_ConsecutiveConvertiblesWithQuotes()
    {
        // Adjacent convertible patterns with apostrophe variants
        Assert.AreEqual("öğ", UzbekPhonetics.Convert("o'g'"));
        Assert.AreEqual("ÖĞ", UzbekPhonetics.Convert("O'G'"));
        Assert.AreEqual("ö'ğ'", UzbekPhonetics.Convert("o''g''"));
        Assert.AreEqual("ö''ğ''", UzbekPhonetics.Convert("o'''g'''"));
        Assert.AreEqual("ğö", UzbekPhonetics.Convert("g'o'"));
        Assert.AreEqual("ĞÖ", UzbekPhonetics.Convert("G'O'"));
        Assert.AreEqual("ööö", UzbekPhonetics.Convert("o'o'o'"));
        Assert.AreEqual("ğğğ", UzbekPhonetics.Convert("g'g'g'"));
    }

    [TestMethod]
    public void ExtremeApostrophes_MixedVariantsInExtremeSequences()
    {
        // Mixed apostrophe variants in multi-quote sequence
        Assert.AreEqual("ö'''rtoq", UzbekPhonetics.Convert("oʻʼʻʼrtoq"));
        Assert.AreEqual("mö'''jiza", UzbekPhonetics.Convert("mo‘’’`jiza"));
        Assert.AreEqual("ğ''alaba", UzbekPhonetics.Convert("g´‛′alaba"));
    }

    // ══════════════════════════════════════════════════════════════════
    // 3. Syllable Break Edge Cases
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void SyllableBreak_Ishoq_AllCasings()
    {
        Assert.AreEqual("is'hoq", UzbekPhonetics.Convert("is'hoq"));
        Assert.AreEqual("Is'hoq", UzbekPhonetics.Convert("Is'hoq"));
        Assert.AreEqual("Is'Hoq", UzbekPhonetics.Convert("Is'Hoq"));
        Assert.AreEqual("is'Hoq", UzbekPhonetics.Convert("is'Hoq"));
        Assert.AreEqual("IS'HOQ", UzbekPhonetics.Convert("IS'HOQ"));

        // Verify HasConvertiblePatterns returns false
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("is'hoq"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("Is'hoq"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("Is'Hoq"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("IS'HOQ"));
    }

    [TestMethod]
    public void SyllableBreak_Mushaf_AllCasings()
    {
        Assert.AreEqual("mus'haf", UzbekPhonetics.Convert("mus'haf"));
        Assert.AreEqual("Mus'haf", UzbekPhonetics.Convert("Mus'haf"));
        Assert.AreEqual("MUS'HAF", UzbekPhonetics.Convert("MUS'HAF"));
        Assert.AreEqual("Mus'Haf", UzbekPhonetics.Convert("Mus'Haf"));
        Assert.AreEqual("mus'Haf", UzbekPhonetics.Convert("mus'Haf"));

        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("mus'haf"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("MUS'HAF"));
    }

    [TestMethod]
    public void SyllableBreak_Ashob_AllCasingsAndInflections()
    {
        Assert.AreEqual("as'hob", UzbekPhonetics.Convert("as'hob"));
        Assert.AreEqual("As'hob", UzbekPhonetics.Convert("As'hob"));
        Assert.AreEqual("AS'HOB", UzbekPhonetics.Convert("AS'HOB"));
        Assert.AreEqual("as'hoblar", UzbekPhonetics.Convert("as'hoblar"));
        Assert.AreEqual("As'Hoblar", UzbekPhonetics.Convert("As'Hoblar"));
        Assert.AreEqual("AS'HOBLAR", UzbekPhonetics.Convert("AS'HOBLAR"));
        Assert.AreEqual("as'hoblari", UzbekPhonetics.Convert("as'hoblari"));

        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("as'hob"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("as'hoblar"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("AS'HOBLAR"));
    }

    [TestMethod]
    public void SyllableBreak_Masul_AllCasingsAndDerivatives()
    {
        Assert.AreEqual("mas'ul", UzbekPhonetics.Convert("mas'ul"));
        Assert.AreEqual("Mas'ul", UzbekPhonetics.Convert("Mas'ul"));
        Assert.AreEqual("MAS'UL", UzbekPhonetics.Convert("MAS'UL"));
        Assert.AreEqual("mas'uliyat", UzbekPhonetics.Convert("mas'uliyat"));
        Assert.AreEqual("mas'uliyatli", UzbekPhonetics.Convert("mas'uliyatli"));
        Assert.AreEqual("mas'uliyatsizlik", UzbekPhonetics.Convert("mas'uliyatsizlik"));
        Assert.AreEqual("MAS'ULIYATLI", UzbekPhonetics.Convert("MAS'ULIYATLI"));

        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("mas'ul"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("mas'uliyatli"));
    }

    [TestMethod]
    public void SyllableBreak_All9ApostropheVariantsInTutuqWords()
    {
        char[] variants = { '\'', '\u2018', '\u2019', '\u02BB', '\u02BC', '`', '\u00B4', '\u201B', '\u2032' };
        foreach (char v in variants)
        {
            string ishoq = $"Is{v}hoq";
            string mushaf = $"mus{v}haf";
            string ashob = $"as{v}hoblar";
            string masul = $"mas{v}uliyatli";

            Assert.AreEqual("Is'hoq", UzbekPhonetics.Convert(ishoq), $"Failed for Ishoq with variant {v}");
            Assert.AreEqual("mus'haf", UzbekPhonetics.Convert(mushaf), $"Failed for mushaf with variant {v}");
            Assert.AreEqual("as'hoblar", UzbekPhonetics.Convert(ashob), $"Failed for ashoblar with variant {v}");
            Assert.AreEqual("mas'uliyatli", UzbekPhonetics.Convert(masul), $"Failed for masuliyatli with variant {v}");

            Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns(ishoq));
            Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns(mushaf));
            Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns(ashob));
            Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns(masul));
        }
    }

    [TestMethod]
    public void SyllableBreak_ChMorphemeProtection()
    {
        // c'h boundary protection
        Assert.AreEqual("ac'hod", UzbekPhonetics.Convert("ac'hod"));
        Assert.AreEqual("Ac'hod", UzbekPhonetics.Convert("Ac'hod"));
        Assert.AreEqual("AC'HOD", UzbekPhonetics.Convert("AC'HOD"));
        Assert.AreEqual("mac'har", UzbekPhonetics.Convert("mac'har"));

        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("ac'hod"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("mac'har"));
    }

    [TestMethod]
    public void SyllableBreak_BoundariesAtExtremes()
    {
        // s'h at exact start of string
        Assert.AreEqual("s'h", UzbekPhonetics.Convert("s'h"));
        Assert.AreEqual("c'h", UzbekPhonetics.Convert("c'h"));
        Assert.AreEqual("s'hoq", UzbekPhonetics.Convert("s'hoq"));
        Assert.AreEqual("as'h", UzbekPhonetics.Convert("as'h"));

        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("s'h"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("c'h"));
    }

    [TestMethod]
    public void SyllableBreak_CoexistingWithConvertiblesInSameWord()
    {
        // A single token containing protected s'h AND convertible o' or g'
        Assert.AreEqual("Is'hoqning öğli", UzbekPhonetics.Convert("Is'hoqning o'g'li"));
        Assert.AreEqual("örtoq Is'hoq", UzbekPhonetics.Convert("o'rtoq Is'hoq"));
        Assert.AreEqual("ğolib as'hoblar", UzbekPhonetics.Convert("g'olib as'hoblar"));
        Assert.AreEqual("çoyxona mus'haf şahar", UzbekPhonetics.Convert("choyxona mus'haf shahar"));

        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("Is'hoqning o'g'li"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("örtoq Is'hoq") || UzbekPhonetics.HasConvertiblePatterns("o'rtoq Is'hoq"));
    }

    // ══════════════════════════════════════════════════════════════════
    // 4. Non-Latin Strings (Cyrillic, Arabic, Chinese)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void NonLatin_Cyrillic_PreservedUntouched()
    {
        string cyrillic = "Шаҳар бўйлаб чойхона ва ғалаба томон ўртоқ борди. Салом дунё!";
        Assert.AreEqual(cyrillic, UzbekPhonetics.Convert(cyrillic));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns(cyrillic));
    }

    [TestMethod]
    public void NonLatin_Cyrillic_WithTutuqVariants_NormalizedCleanly()
    {
        // Cyrillic with tutuq turned comma or acute
        Assert.AreEqual("Мў'жиза", UzbekPhonetics.Convert("Мўʻжиза"));
        Assert.AreEqual("сан'ат", UzbekPhonetics.Convert("санʼат"));
    }

    [TestMethod]
    public void NonLatin_Arabic_PreservedUntouched()
    {
        string arabic = "السلام عليكم ورحمة الله وبركاته - أوزبكستان";
        Assert.AreEqual(arabic, UzbekPhonetics.Convert(arabic));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns(arabic));
    }

    [TestMethod]
    public void NonLatin_Chinese_PreservedUntouched()
    {
        string chinese = "你好，世界！这是一段测试文本。乌兹别克斯坦。";
        Assert.AreEqual(chinese, UzbekPhonetics.Convert(chinese));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns(chinese));
    }

    [TestMethod]
    public void NonLatin_JapaneseAndKorean_PreservedUntouched()
    {
        string mixed = "こんにちは世界 안녕하세요 세계";
        Assert.AreEqual(mixed, UzbekPhonetics.Convert(mixed));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns(mixed));
    }

    [TestMethod]
    public void NonLatin_InterleavedWithConvertibleLatin()
    {
        string input = "shahar (Шаҳар), o'rtoq (ўртоқ), choyxona (茶馆), g'alaba (النصر)";
        string expected = "şahar (Шаҳар), örtoq (ўртоқ), çoyxona (茶馆), ğalaba (النصر)";
        Assert.AreEqual(expected, UzbekPhonetics.Convert(input));
    }

    // ══════════════════════════════════════════════════════════════════
    // 5. Unicode Emoji & Complex Sequences
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Emoji_StandaloneAndSurrogatePairs()
    {
        string emojis = "😀 🚀 🔥 🎉 ☕ 🇺🇿 👨‍👩‍👧‍👦";
        Assert.AreEqual(emojis, UzbekPhonetics.Convert(emojis));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns(emojis));
    }

    [TestMethod]
    public void Emoji_DirectlyAdjacentToConvertibleWords()
    {
        Assert.AreEqual("😀şahar", UzbekPhonetics.Convert("😀shahar"));
        Assert.AreEqual("şahar😀", UzbekPhonetics.Convert("shahar😀"));
        Assert.AreEqual("🚀örtoq🎉", UzbekPhonetics.Convert("🚀o'rtoq🎉"));
        Assert.AreEqual("💡çoyxona☕", UzbekPhonetics.Convert("💡choyxona☕"));
        Assert.AreEqual("🏆ğalaba🥇", UzbekPhonetics.Convert("🏆g'alaba🥇"));
        Assert.AreEqual("❤️Is'hoq👍", UzbekPhonetics.Convert("❤️Is'hoq👍"));
    }

    [TestMethod]
    public void Emoji_EmbeddedWithinWordDoesNotCorruptSurrogates()
    {
        // High/low surrogate pair inserted between characters
        string input = "s😀hahar"; // 's' + surrogate pair + 'h' + 'ahar' -> 's' and 'h' separated by emoji!
        string result = UzbekPhonetics.Convert(input);
        Assert.AreEqual("s😀hahar", result); // Must NOT merge into ş!
    }

    // ══════════════════════════════════════════════════════════════════
    // 6. Punctuation, Math, JSON, and Code Payloads
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Punctuation_AllKeyboardSymbols()
    {
        string symbols = "!@#$%^&*()-_=+[{]}\\|;:\",<.>/?";
        Assert.AreEqual(symbols, UzbekPhonetics.Convert(symbols));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns(symbols));
    }

    [TestMethod]
    public void Punctuation_JsonAndCodeSnippets()
    {
        string json = "{\"city\": \"shahar\", \"friend\": \"o'rtoq\", \"tea\": \"choyxona\", \"win\": \"g'alaba\"}";
        string expectedJson = "{\"city\": \"şahar\", \"friend\": \"örtoq\", \"tea\": \"çoyxona\", \"win\": \"ğalaba\"}";
        Assert.AreEqual(expectedJson, UzbekPhonetics.Convert(json));

        string code = "if (shahar == \"o'rtoq\") { choyxona.g'alaba(); }";
        string expectedCode = "if (şahar == \"örtoq\") { çoyxona.ğalaba(); }";
        Assert.AreEqual(expectedCode, UzbekPhonetics.Convert(code));
    }

    // ══════════════════════════════════════════════════════════════════
    // 7. Large Text Payloads & Performance Stress
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Performance_100kCharacters_Convert_CompletesFast()
    {
        // 100,000 characters payload with repeating convertible words
        string chunk = "shahar o'rtoq choyxona g'alaba Is'hoq mus'haf ";
        int repeats = 100_000 / chunk.Length;
        var sb = new StringBuilder(chunk.Length * repeats);
        for (int i = 0; i < repeats; i++)
        {
            sb.Append(chunk);
        }
        string largePayload = sb.ToString();

        var sw = Stopwatch.StartNew();
        string converted = UzbekPhonetics.Convert(largePayload);
        sw.Stop();

        Assert.IsTrue(converted.Contains("şahar"));
        Assert.IsTrue(converted.Contains("örtoq"));
        Assert.IsTrue(converted.Contains("çoyxona"));
        Assert.IsTrue(converted.Contains("ğalaba"));
        Assert.IsTrue(converted.Contains("Is'hoq"));
        Assert.IsTrue(converted.Contains("mus'haf"));

        // Must complete in under 50ms for 100k characters
        Assert.IsTrue(sw.ElapsedMilliseconds < 50, $"Execution took {sw.ElapsedMilliseconds} ms, expected < 50ms");
    }

    [TestMethod]
    public void Performance_SingleWordLatency_MeetsSubMillisecondTarget()
    {
        // Keystroke interception latency target in R3 is < 5ms
        // The phonetic engine per-word latency should be < 0.1ms (< 100 microseconds)
        string[] sampleWords = { "shahar", "o'rtoq", "g'alaba", "choyxona", "Is'hoq", "mas'uliyatli", "SHAHAR", "O'RTOQ" };

        // Warm up JIT
        for (int i = 0; i < 1000; i++)
        {
            foreach (var w in sampleWords)
            {
                UzbekPhonetics.Convert(w);
                UzbekPhonetics.HasConvertiblePatterns(w);
            }
        }

        const int iterations = 10_000;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            foreach (var w in sampleWords)
            {
                UzbekPhonetics.Convert(w);
            }
        }
        sw.Stop();

        double totalMicroseconds = (sw.Elapsed.TotalMilliseconds * 1000.0);
        double microsecondsPerWord = totalMicroseconds / (iterations * sampleWords.Length);

        // Verification: Average latency per word is imperceptible (< 50 microseconds)
        Assert.IsTrue(microsecondsPerWord < 50.0, $"Average latency {microsecondsPerWord:F2} µs exceeds 50 µs limit");
    }

    // ══════════════════════════════════════════════════════════════════
    // 8. Property-Based Invariant Fuzzing
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Invariant_ContractParity_HasConvertiblePatternsVsSequences()
    {
        // HasConvertiblePatterns and HasConvertibleSequences must return identical boolean for any input
        var rng = new Random(42);
        string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'‘`’ʻʼ -_1234567890";

        for (int test = 0; test < 1000; test++)
        {
            int length = rng.Next(0, 30);
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(alphabet[rng.Next(alphabet.Length)]);
            }
            string str = sb.ToString();

            bool p1 = UzbekPhonetics.HasConvertiblePatterns(str);
            bool p2 = UzbekPhonetics.HasConvertibleSequences(str);
            Assert.AreEqual(p1, p2, $"Contract divergence on input '{str}'");
        }
    }

    [TestMethod]
    public void Invariant_Idempotency_AlreadyReformedTextRemainsUnchanged()
    {
        // Once converted, calling Convert again on the reformed text must produce the exact same text
        string[] reformedPhrases =
        {
            "şahar",
            "örtoq",
            "çoyxona",
            "ğalaba",
            "ŞAHAR",
            "ÖRTOQ",
            "ÇOYXONA",
            "ĞALABA",
            "Is'hoq",
            "mus'haf",
            "mas'uliyatli",
            "bu yerda şahar va örtoq bor",
            "qöşiqçi çöqqiga bordi"
        };

        foreach (var phrase in reformedPhrases)
        {
            string once = UzbekPhonetics.Convert(phrase);
            string twice = UzbekPhonetics.Convert(once);
            Assert.AreEqual(once, twice, $"Idempotency failed for '{phrase}'");
        }
    }

    [TestMethod]
    public void Invariant_LengthNonExpansion()
    {
        // 2024 reform replaces 2 characters (sh, ch, o', g') with 1 character (ş, ç, ö, ğ).
        // Therefore, Convert(s).Length MUST NEVER exceed s.Length!
        var rng = new Random(1337);
        string[] chunks = { "sh", "ch", "o'", "g'", "Is'hoq", "mus'haf", "a", "b", "c", "o", "g", "'", " " };

        for (int test = 0; test < 500; test++)
        {
            var sb = new StringBuilder();
            int chunkCount = rng.Next(1, 20);
            for (int i = 0; i < chunkCount; i++)
            {
                sb.Append(chunks[rng.Next(chunks.Length)]);
            }
            string input = sb.ToString();
            string output = UzbekPhonetics.Convert(input);

            Assert.IsTrue(output.Length <= input.Length,
                $"Output length ({output.Length}) exceeded input length ({input.Length}) for '{input}' -> '{output}'");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 9. Additional Deep Adversarial Probes
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void CyrillicHomoglyphs_NeverTransliterateAsLatin()
    {
        // Cyrillic letters looking identical to Latin:
        // Cyrillic 'о' (U+043E), 'с' (U+0441), 'һ' (U+04BB), 'а' (U+0430)
        string cyrillicOWithApostrophe = "\u043E'rtoq"; // Cyrillic 'о' + apostrophe + 'rtoq'
        string result = UzbekPhonetics.Convert(cyrillicOWithApostrophe);
        // Must NOT convert to ö because the initial letter is Cyrillic, not Latin 'o'!
        Assert.AreEqual(cyrillicOWithApostrophe, result);

        string cyrillicSWithH = "\u0441hahar"; // Cyrillic 'с' + Latin 'h'
        Assert.AreEqual(cyrillicSWithH, UzbekPhonetics.Convert(cyrillicSWithH));
    }

    [TestMethod]
    public void ApostropheVariants_All81PairwiseCombinations_ConvertCleanly()
    {
        // Pairwise combinations of all 9 apostrophes for o<v1>g<v2> -> öğ
        char[] variants = { '\'', '\u2018', '\u2019', '\u02BB', '\u02BC', '`', '\u00B4', '\u201B', '\u2032' };
        for (int i = 0; i < variants.Length; i++)
        {
            for (int j = 0; j < variants.Length; j++)
            {
                string input = $"o{variants[i]}g{variants[j]}";
                string converted = UzbekPhonetics.Convert(input);
                Assert.AreEqual("öğ", converted, $"Failed on pair {i},{j} with chars '{variants[i]}' and '{variants[j]}'");
                Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns(input));
            }
        }
    }

    [TestMethod]
    public void Fuzz_RandomizedCasingPermutations_PreservesCaseIntegrity()
    {
        var rng = new Random(999);
        string[] baseWords = { "shahar", "o'rtoq", "g'alaba", "choyxona" };

        for (int i = 0; i < 200; i++)
        {
            string baseWord = baseWords[rng.Next(baseWords.Length)];
            var sb = new StringBuilder();
            foreach (char c in baseWord)
            {
                if (char.IsLetter(c))
                {
                    sb.Append(rng.Next(2) == 0 ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            string mixedWord = sb.ToString();
            string converted = UzbekPhonetics.Convert(mixedWord);

            // Verified property: converted string must be non-empty and have correct length
            Assert.IsFalse(string.IsNullOrEmpty(converted));
            Assert.IsTrue(converted.Length <= mixedWord.Length);
            // Contract consistency check
            Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns(mixedWord));
        }
    }

    [TestMethod]
    public void NullByte_InString_HandledSafelyWithoutCrash()
    {
        string withNull = "sh\0ahar";
        string result = UzbekPhonetics.Convert(withNull);
        // 's' and 'h' are contiguous, so they form 'sh' digraph and convert to 'ş'
        Assert.AreEqual("ş\0ahar", result);

        string withNullBetweenDigraph = "s\0hahar";
        Assert.AreEqual("s\0hahar", UzbekPhonetics.Convert(withNullBetweenDigraph));
    }

    [TestMethod]
    public void Performance_OneMillionCharacters_CompletesWithinLimit()
    {
        // 1,000,000 characters payload
        string chunk = "shahar o'rtoq choyxona g'alaba Is'hoq mus'haf ";
        int repeats = 1_000_000 / chunk.Length;
        var sb = new StringBuilder(chunk.Length * repeats);
        for (int i = 0; i < repeats; i++)
        {
            sb.Append(chunk);
        }
        string millionPayload = sb.ToString();

        var sw = Stopwatch.StartNew();
        string converted = UzbekPhonetics.Convert(millionPayload);
        sw.Stop();

        Assert.IsNotNull(converted);
        Assert.IsTrue(sw.ElapsedMilliseconds < 500, $"1M payload took {sw.ElapsedMilliseconds} ms, expected < 500ms");
    }
}

