// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.Tests — EmpiricalChallengeM1Tests.cs
// Adversarial Empirical Challenge Suite for Milestone 1 (M1).
// Focus areas:
// 1. Non-corruption of English words, numbers, code symbols, whitespace,
//    and non-convertible Uzbek words.
// 2. High-concurrency multi-threaded stress testing (128+ parallel threads).
// 3. High-precision latency and throughput benchmarking (< 1ms target).
// 4. Boundary extremes, fuzzing samples, and Unicode edge cases.
// ──────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;

namespace YangiAlifbo.Tests;

[TestClass]
public class EmpiricalChallengeM1Tests
{
    // ══════════════════════════════════════════════════════════════════
    // 1. Non-Corruption Empirical Verification
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void NonCorruption_EnglishWords_RemainUnchanged()
    {
        // 100+ English words that do not contain 1995 Uzbek phonemes (o', g', sh, ch)
        string[] englishWords =
        [
            "apple", "banana", "orange", "keyboard", "monitor", "mouse", "system",
            "computer", "network", "server", "client", "database", "memory", "storage",
            "algorithm", "variable", "constant", "function", "parameter", "argument",
            "statement", "expression", "operator", "boolean", "integer", "string",
            "double", "float", "decimal", "array", "list", "dictionary", "queue",
            "stack", "tree", "graph", "node", "edge", "loop", "while", "for",
            "until", "break", "continue", "return", "yield", "class", "struct",
            "interface", "record", "enum", "delegate", "lambda", "event", "property",
            "field", "indexer", "generic", "type", "object", "null", "true", "false",
            "public", "private", "protected", "internal", "static", "readonly",
            "volatile", "virtual", "override", "abstract", "sealed", "async", "await",
            "task", "thread", "mutex", "lock", "semaphore", "barrier", "pipe",
            "stream", "reader", "writer", "file", "path", "directory", "process",
            "module", "assembly", "binary", "package", "build", "test", "debug",
            "release", "deploy", "runtime", "framework", "platform", "window",
            "desktop", "application", "service", "domain", "entity", "service"
        ];

        foreach (string word in englishWords)
        {
            string converted = UzbekPhonetics.Convert(word);
            Assert.AreEqual(word, converted, $"English word '{word}' was corrupted to '{converted}'!");
        }
    }

    [TestMethod]
    public void NonCorruption_NumbersAndNumericFormats_RemainUnchanged()
    {
        string[] numericSamples =
        [
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
            "42", "-100", "+999", "1234567890",
            "3.141592653589793", "0.00001", "-273.15",
            "1.23e10", "4.56E-7", "-9.87e+2",
            "0xDEADBEEF", "0x1A2B3C", "0b10101010",
            "$100.50", "€49.99", "£12.00", "¥1000",
            "2026-10-21", "11:42:00.123", "192.168.1.1", "127.0.0.1:8080",
            "1,000,000", "50%", "360°"
        ];

        foreach (string num in numericSamples)
        {
            string converted = UzbekPhonetics.Convert(num);
            Assert.AreEqual(num, converted, $"Numeric input '{num}' was corrupted to '{converted}'!");
        }
    }

    [TestMethod]
    public void NonCorruption_CodeSymbolsAndSnippets_RemainUnchanged()
    {
        string[] codeSnippets =
        [
            "public static int Add(int a, int b) => a + b;",
            "def foo(x, y):\n    return x * y + 42",
            "{\"id\": 101, \"name\": \"test_item\", \"active\": true, \"values\": [1, 2, 3]}",
            "SELECT id, name, created_at FROM users WHERE status = 1;",
            "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$",
            "int[] arr = new int[] { 1, 2, 3, 4, 5 };",
            "for (int i = 0; i < 10; i++) { sum += i; }",
            "a && b || !c ^ (d & ~e) | (f >> 2) | (g << 3)",
            "x += 1; y -= 2; z *= 3; w /= 4; m %= 5;",
            "<div><span class=\"item\">Text Content</span></div>",
            "<!DOCTYPE html><html><head><title>Title</title></head></html>",
            "/* Multi-line comment\n * with asterisk formatting\n */",
            "// Single-line comment with special symbols: !@#$%^&*()_+",
            "http://localhost:5000/api/v1/resource?id=123&token=abc"
        ];

        foreach (string code in codeSnippets)
        {
            string converted = UzbekPhonetics.Convert(code);
            Assert.AreEqual(code, converted, $"Code snippet was corrupted!\nOriginal:\n{code}\nConverted:\n{converted}");
        }
    }

    [TestMethod]
    public void NonCorruption_WhitespaceAndControlCharacters_RemainUnchanged()
    {
        string[] whitespaceSamples =
        [
            " ",
            "  ",
            "\t",
            "\t\t",
            "\r",
            "\n",
            "\r\n",
            "\r\n\r\n",
            "   \t  \r\n  \t ",
            "hello world",
            "hello\tworld",
            "hello\r\nworld",
            "  leading and trailing whitespace  ",
            "\n\n\nmultiple\nnewlines\n\n\n"
        ];

        foreach (string ws in whitespaceSamples)
        {
            string converted = UzbekPhonetics.Convert(ws);
            Assert.AreEqual(ws, converted, $"Whitespace input was corrupted!");
        }
    }

    [TestMethod]
    public void NonCorruption_NonConvertibleUzbekWords_RemainUnchanged()
    {
        // Uzbek words without o', g', sh, ch
        string[] nonConvertibleUzbek =
        [
            "kitob", "maktab", "qalam", "daftar", "ustoz", "dars", "suv", "non",
            "daryo", "yigit", "qiz", "ona", "ota", "aka", "uka", "opa", "singil",
            "bahor", "yoz", "kuz", "kun", "tun", "tong", "bor", "kel", "ket",
            "yur", "tur", "yot", "uxla", "gul", "daraxt", "barg", "novda",
            "meva", "olma", "anor", "uzum", "anjir", "nok", "qovun", "tarvuz",
            "koz", "quloq", "burun", "ogiz", "til", "yuz",
            "qol", "oyoq", "yurak", "jigar", "qon", "suyak", "teri"
        ];

        foreach (string word in nonConvertibleUzbek)
        {
            string converted = UzbekPhonetics.Convert(word);
            Assert.AreEqual(word, converted, $"Non-convertible Uzbek word '{word}' was corrupted to '{converted}'!");
        }
    }

    [TestMethod]
    public void NonCorruption_MorphemeBoundariesAndTutuq_Preserved()
    {
        var tutuqCases = new Dictionary<string, string>
        {
            // s'h boundary protection (must NOT become ş)
            { "Is'hoq", "Is'hoq" },
            { "IS'HOQ", "IS'HOQ" },
            { "is'hoq", "is'hoq" },
            { "mus'haf", "mus'haf" },
            { "MUS'HAF", "MUS'HAF" },
            { "as'hob", "as'hob" },
            { "AS'HOB", "AS'HOB" },

            // c'h boundary protection (must NOT become ç)
            { "c'h", "c'h" },
            { "C'H", "C'H" },

            // tutuq belgisi without s/c or without h
            { "mas'ul", "mas'ul" },
            { "MAS'UL", "MAS'UL" },
            { "mas'uliyat", "mas'uliyat" },
            { "san'at", "san'at" },
            { "ta'lim", "ta'lim" },
            { "ma'no", "ma'no" },
            { "sur'at", "sur'at" },
            { "a'lo", "a'lo" },
            { "e'tibor", "e'tibor" },
            { "in'om", "in'om" },

            // tutuq in word with legitimate reform patterns elsewhere
            { "Is'hoqning o'g'li", "Is'hoqning öğli" },
            { "mus'hafdagi oyatlar", "mus'hafdagi oyatlar" },
            { "mas'uliyatli shahar", "mas'uliyatli şahar" }
        };

        foreach (var (input, expected) in tutuqCases)
        {
            string actual = UzbekPhonetics.Convert(input);
            Assert.AreEqual(expected, actual, $"Boundary/tutuq word '{input}' failed conversion!");
        }
    }

    [TestMethod]
    public void NonCorruption_TutuqBoundaries_All9ApostropheVariants()
    {
        char[] variants = { '\'', '\u2018', '\u2019', '\u02BB', '\u02BC', '`', '\u00B4', '\u201B', '\u2032' };

        foreach (char v in variants)
        {
            string input = $"Is{v}hoq";
            string converted = UzbekPhonetics.Convert(input);
            Assert.AreEqual("Is'hoq", converted, $"Variant U+{(int)v:X4} failed boundary preservation in Is{v}hoq!");

            string mushaf = $"mus{v}haf";
            string convertedMushaf = UzbekPhonetics.Convert(mushaf);
            Assert.AreEqual("mus'haf", convertedMushaf, $"Variant U+{(int)v:X4} failed boundary preservation in mus{v}haf!");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 2. High-Concurrency Multi-Threaded Empirical Verification
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Concurrency_Convert_128ParallelThreads_StrictlyThreadSafe()
    {
        const int threadCount = 128;
        const int iterationsPerThread = 1000;

        var testCorpus = new (string Input, string Expected)[]
        {
            ("shahar", "şahar"),
            ("Shahar", "Şahar"),
            ("SHAHAR", "ŞAHAR"),
            ("choyxona", "çoyxona"),
            ("Choyxona", "Çoyxona"),
            ("CHOYXONA", "ÇOYXONA"),
            ("o'rtoq", "örtoq"),
            ("O'rtoq", "Örtoq"),
            ("O'RTOQ", "ÖRTOQ"),
            ("g'alaba", "ğalaba"),
            ("G'alaba", "Ğalaba"),
            ("G'ALABA", "ĞALABA"),
            ("Is'hoq", "Is'hoq"),
            ("mus'haf", "mus'haf"),
            ("mas'ul", "mas'ul"),
            ("mo''jiza", "mö'jiza"),
            ("sho''la", "şö'la"),
            ("shoshilinch", "şoşilinç"),
            ("cho'qqi", "çöqqi"),
            ("qo'shiq", "qöşiq"),
            ("kitob", "kitob"),
            ("system", "system"),
            ("12345", "12345")
        };

        var errors = new ConcurrentBag<string>();

        Parallel.For(0, threadCount, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, threadId =>
        {
            try
            {
                for (int iter = 0; iter < iterationsPerThread; iter++)
                {
                    int index = (threadId + iter) % testCorpus.Length;
                    var (input, expected) = testCorpus[index];

                    string actual = UzbekPhonetics.Convert(input);
                    if (actual != expected)
                    {
                        errors.Add($"Thread {threadId} Iter {iter}: Input '{input}', expected '{expected}', got '{actual}'");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Thread {threadId} threw exception: {ex.GetType().Name} - {ex.Message}");
            }
        });

        Assert.IsTrue(errors.IsEmpty, $"Concurrent execution encountered {errors.Count} errors! First error: {errors.FirstOrDefault()}");
    }

    [TestMethod]
    public void Concurrency_NormalizeApostrophes_128ParallelThreads_StrictlyThreadSafe()
    {
        const int threadCount = 128;
        const int iterationsPerThread = 1000;

        string[] testSamples =
        [
            "o'rtoq",
            "o\u2018rtoq",
            "o\u2019rtoq",
            "o\u02BBrtoq",
            "o\u02BCrtoq",
            "o`rtoq",
            "o\u00B4rtoq",
            "o\u201Brtoq",
            "o\u2032rtoq",
            "g'alaba",
            "g\u02BBalaba",
            "Is\u02BBhoq",
            "plain ascii text without quotes"
        ];

        var errors = new ConcurrentBag<string>();

        Parallel.For(0, threadCount, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, threadId =>
        {
            try
            {
                for (int iter = 0; iter < iterationsPerThread; iter++)
                {
                    string sample = testSamples[(threadId + iter) % testSamples.Length];
                    string normalized = UzbekPhonetics.NormalizeApostrophes(sample);

                    // Must never contain non-ASCII apostrophes
                    if (normalized.Contains('\u2018') || normalized.Contains('\u2019') ||
                        normalized.Contains('\u02BB') || normalized.Contains('\u02BC') ||
                        normalized.Contains('`') || normalized.Contains('\u00B4') ||
                        normalized.Contains('\u201B') || normalized.Contains('\u2032'))
                    {
                        errors.Add($"Thread {threadId}: Normalized output '{normalized}' retained unnormalized variant!");
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Thread {threadId} threw exception: {ex.GetType().Name} - {ex.Message}");
            }
        });

        Assert.IsTrue(errors.IsEmpty, $"NormalizeApostrophes concurrency errors: {errors.FirstOrDefault()}");
    }

    // ══════════════════════════════════════════════════════════════════
    // 3. High-Precision Latency & Throughput Benchmark
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Benchmark_Latency_PerWordConversion_FarBelowOneMillisecond()
    {
        string[] benchmarkCorpus =
        [
            "shahar",
            "choyxona",
            "o'rtoq",
            "g'alaba",
            "shoshilinch",
            "cho'qqi",
            "qo'shiq",
            "Is'hoqning",
            "mus'haf",
            "mas'uliyat",
            "mo''jiza",
            "sho''la",
            "O'zbekiston",
            "G'ildirak",
            "Chaqmoq",
            "Shiddatli",
            "kitob",
            "kompyuter",
            "алгоритм",
            "System.Threading.Tasks.Parallel"
        ];

        // 1. Warm-up JIT and CPU branch predictor
        for (int i = 0; i < 20_000; i++)
        {
            string s = benchmarkCorpus[i % benchmarkCorpus.Length];
            _ = UzbekPhonetics.Convert(s);
        }

        // 2. High-precision measurement: 100,000 iterations
        const int iterations = 100_000;
        var latenciesInTicks = new long[iterations];

        var swTotal = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            string s = benchmarkCorpus[i % benchmarkCorpus.Length];

            long start = Stopwatch.GetTimestamp();
            _ = UzbekPhonetics.Convert(s);
            long end = Stopwatch.GetTimestamp();

            latenciesInTicks[i] = end - start;
        }

        swTotal.Stop();

        // 3. Compute statistics
        double totalMs = swTotal.Elapsed.TotalMilliseconds;
        double avgLatencyUs = (totalMs * 1000.0) / iterations; // microseconds per word
        double wordsPerSecond = iterations / swTotal.Elapsed.TotalSeconds;

        Array.Sort(latenciesInTicks);
        double tickToUs = 1_000_000.0 / Stopwatch.Frequency;
        double p50Us = latenciesInTicks[(int)(iterations * 0.50)] * tickToUs;
        double p95Us = latenciesInTicks[(int)(iterations * 0.95)] * tickToUs;
        double p99Us = latenciesInTicks[(int)(iterations * 0.99)] * tickToUs;
        double maxUs = latenciesInTicks[iterations - 1] * tickToUs;

        TestContext?.WriteLine($"[Benchmark Results]");
        TestContext?.WriteLine($"Total iterations: {iterations:N0}");
        TestContext?.WriteLine($"Total elapsed:    {totalMs:F2} ms");
        TestContext?.WriteLine($"Throughput:       {wordsPerSecond:N0} words/sec");
        TestContext?.WriteLine($"Avg Latency:      {avgLatencyUs:F3} µs ({avgLatencyUs / 1000.0:F6} ms)");
        TestContext?.WriteLine($"P50 Latency:      {p50Us:F3} µs");
        TestContext?.WriteLine($"P95 Latency:      {p95Us:F3} µs");
        TestContext?.WriteLine($"P99 Latency:      {p99Us:F3} µs");
        TestContext?.WriteLine($"Max Latency:      {maxUs:F3} µs");

        // Per requirement: Per-word conversion latency must be far below 1.0 ms (1000 µs).
        // We assert that the average latency is below 0.05 ms (50 µs) and P99 is below 0.2 ms (200 µs).
        Assert.IsTrue(avgLatencyUs < 50.0, $"Average latency {avgLatencyUs:F3} µs exceeded 50 µs threshold!");
        Assert.IsTrue(p99Us < 200.0, $"P99 latency {p99Us:F3} µs exceeded 200 µs threshold!");
    }

    public TestContext? TestContext { get; set; }

    // ══════════════════════════════════════════════════════════════════
    // 4. Boundary Extremes and Unicode Edge Cases
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void EdgeCases_NullAndEmpty_HandledSafely()
    {
        Assert.IsNull(UzbekPhonetics.Convert(null!));
        Assert.AreEqual(string.Empty, UzbekPhonetics.Convert(string.Empty));
        Assert.IsNull(UzbekPhonetics.NormalizeApostrophes(null!));
        Assert.AreEqual(string.Empty, UzbekPhonetics.NormalizeApostrophes(string.Empty));
    }

    [TestMethod]
    public void EdgeCases_SingleCharacters_HandledSafely()
    {
        string[] singles = ["a", "b", "c", "s", "h", "o", "g", "'", "`", "1", " ", "\n"];
        foreach (string s in singles)
        {
            string expected = s == "`" ? "'" : s;
            Assert.AreEqual(expected, UzbekPhonetics.Convert(s));
        }
    }

    [TestMethod]
    public void EdgeCases_ConsecutiveIdenticalPatterns_TransliteratedCorrectly()
    {
        Assert.AreEqual("şşş", UzbekPhonetics.Convert("shshsh"));
        Assert.AreEqual("ççç", UzbekPhonetics.Convert("chchch"));
        Assert.AreEqual("ööö", UzbekPhonetics.Convert("o'o'o'"));
        Assert.AreEqual("ğğğ", UzbekPhonetics.Convert("g'g'g'"));
        Assert.AreEqual("ŞŞŞ", UzbekPhonetics.Convert("SHSHSH"));
        Assert.AreEqual("ÇÇÇ", UzbekPhonetics.Convert("CHCHCH"));
        Assert.AreEqual("ÖÖÖ", UzbekPhonetics.Convert("O'O'O'"));
        Assert.AreEqual("ĞĞĞ", UzbekPhonetics.Convert("G'G'G'"));
    }

    [TestMethod]
    public void EdgeCases_SurrogatePairsAndEmojis_PreservedWithoutException()
    {
        string[] emojiSamples =
        [
            "Salom dunyo! 🇺🇿 🚀 ✨",
            "shahar 🏙️ choyxona ☕ o'rtoq 🤝",
            "😀😁😂🤣😃😄😅😆😉😊😋😎😍"
        ];

        string[] expected =
        [
            "Salom dunyo! 🇺🇿 🚀 ✨",
            "şahar 🏙️ çoyxona ☕ örtoq 🤝",
            "😀😁😂🤣😃😄😅😆😉😊😋😎😍"
        ];

        for (int i = 0; i < emojiSamples.Length; i++)
        {
            string actual = UzbekPhonetics.Convert(emojiSamples[i]);
            Assert.AreEqual(expected[i], actual);
        }
    }

    [TestMethod]
    public void EdgeCases_LargeText_LinearScalingWithoutStackOverflow()
    {
        // 50,000 character synthetic string with mixed patterns
        string chunk = "shahar choyxona o'rtoq g'alaba Is'hoq mus'haf mo''jiza ";
        int repeatCount = 1000;
        var sb = new System.Text.StringBuilder(chunk.Length * repeatCount);
        for (int i = 0; i < repeatCount; i++)
        {
            sb.Append(chunk);
        }

        string largeInput = sb.ToString();

        var sw = Stopwatch.StartNew();
        string converted = UzbekPhonetics.Convert(largeInput);
        sw.Stop();

        Assert.IsNotNull(converted);
        Assert.IsTrue(converted.Length > 0);
        Assert.IsTrue(sw.ElapsedMilliseconds < 500, $"Large text conversion took too long: {sw.ElapsedMilliseconds} ms");
    }
}
