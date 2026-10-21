// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.Tests — EmpiricalChallengeM2PerformanceAndHotReloadTests.cs
// Adversarial Empirical Challenge Suite for Milestone 2 (M2):
// Dynamic Hot-Reloading, Multi-Threaded Concurrency, and Latency Benchmark.
//
// Verification Requirements:
// 1. Dynamic hot-reloading: Programmatically write/modify user_whitelist.txt
//    and user_blacklist.txt in %AppData%/YangiAlifbo/, verifying that
//    within 500ms ShouldConvert dynamically reflects overrides.
// 2. Multi-threaded concurrency: 100+ parallel threads calling ShouldConvert
//    with diverse inputs while concurrently triggering reloads/modifications;
//    verifying 0 exceptions, 0 deadlocks, zero race conditions.
// 3. Performance benchmark: Per-word discrimination latency across
//    100,000 queries; verifying average latency < 20 µs per word.
// 4. Edge cases & boundary stress: Homographs, deep affix chaining,
//    consonant alternations, and extreme boundary strings.
// ──────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;

namespace YangiAlifbo.Tests;

[TestClass]
public class EmpiricalChallengeM2PerformanceAndHotReloadTests
{
    private static readonly string AppDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YangiAlifbo");

    private static readonly string WhitelistPath = Path.Combine(AppDataDir, "user_whitelist.txt");
    private static readonly string BlacklistPath = Path.Combine(AppDataDir, "user_blacklist.txt");

    private static string? _originalWhitelist;
    private static string? _originalBlacklist;

    [ClassInitialize]
    public static void ClassSetup(TestContext context)
    {
        Directory.CreateDirectory(AppDataDir);
        if (File.Exists(WhitelistPath))
            _originalWhitelist = File.ReadAllText(WhitelistPath, Encoding.UTF8);
        if (File.Exists(BlacklistPath))
            _originalBlacklist = File.ReadAllText(BlacklistPath, Encoding.UTF8);
    }

    [ClassCleanup]
    public static void ClassTeardown()
    {
        try
        {
            if (_originalWhitelist != null)
                File.WriteAllText(WhitelistPath, _originalWhitelist, Encoding.UTF8);
            if (_originalBlacklist != null)
                File.WriteAllText(BlacklistPath, _originalBlacklist, Encoding.UTF8);
        }
        catch
        {
            // Best effort restoration
        }
    }

    [TestInitialize]
    public void TestSetup()
    {
        Directory.CreateDirectory(AppDataDir);
        File.WriteAllText(WhitelistPath, "# Clean Whitelist\n", Encoding.UTF8);
        File.WriteAllText(BlacklistPath, "# Clean Blacklist\n", Encoding.UTF8);
        Thread.Sleep(100); // Allow file system events to settle
    }

    [TestCleanup]
    public void TestTeardown()
    {
        try
        {
            File.WriteAllText(WhitelistPath, "# Clean Whitelist\n", Encoding.UTF8);
            File.WriteAllText(BlacklistPath, "# Clean Blacklist\n", Encoding.UTF8);
            Thread.Sleep(100);
        }
        catch
        {
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 1. DYNAMIC HOT-RELOADING EMPIRICAL VERIFICATION (< 500ms)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task HotReload_WhitelistAddition_ReflectedWithin500ms()
    {
        using var detector = new LanguageDetector();
        await Task.Delay(100); // Ensure FileSystemWatcher is active

        string testWord = "crashpad";
        Assert.IsFalse(detector.ShouldConvert(testWord, ConversionMode.Strict),
            "Precondition failed: test word should be rejected initially.");

        var sw = Stopwatch.StartNew();
        // Modify user_whitelist.txt
        await File.AppendAllLinesAsync(WhitelistPath, new[] { testWord }, Encoding.UTF8);

        // Poll every 10ms to observe the exact moment hot-reload occurs
        bool converted = false;
        long elapsedMs = 0;
        while (sw.ElapsedMilliseconds < 1500)
        {
            if (detector.ShouldConvert(testWord, ConversionMode.Strict))
            {
                converted = true;
                elapsedMs = sw.ElapsedMilliseconds;
                break;
            }
            await Task.Delay(10);
        }

        Console.WriteLine($"[HotReload Whitelist] Reflected in {elapsedMs} ms");
        Assert.IsTrue(converted, "Hot-reload failed to reflect whitelist addition within timeout.");
        Assert.IsTrue(elapsedMs <= 500,
            $"Hot-reload must reflect within 500ms. Actual elapsed: {elapsedMs} ms");
    }

    [TestMethod]
    public async Task HotReload_BlacklistAddition_ReflectedWithin500ms()
    {
        using var detector = new LanguageDetector();
        await Task.Delay(100); // Ensure FileSystemWatcher is active

        string testWord = "shahar";
        Assert.IsTrue(detector.ShouldConvert(testWord, ConversionMode.Strict),
            "Precondition failed: 'shahar' should be accepted initially.");

        var sw = Stopwatch.StartNew();
        // Modify user_blacklist.txt
        await File.AppendAllLinesAsync(BlacklistPath, new[] { testWord }, Encoding.UTF8);

        // Poll every 10ms to observe hot-reload rejection
        bool rejected = false;
        long elapsedMs = 0;
        while (sw.ElapsedMilliseconds < 1500)
        {
            if (!detector.ShouldConvert(testWord, ConversionMode.Strict))
            {
                rejected = true;
                elapsedMs = sw.ElapsedMilliseconds;
                break;
            }
            await Task.Delay(10);
        }

        Console.WriteLine($"[HotReload Blacklist] Reflected in {elapsedMs} ms");
        Assert.IsTrue(rejected, "Hot-reload failed to reflect blacklist addition within timeout.");
        Assert.IsTrue(elapsedMs <= 500,
            $"Hot-reload must reflect within 500ms. Actual elapsed: {elapsedMs} ms");
    }

    [TestMethod]
    public async Task HotReload_BlacklistTakesPrecedenceOverWhitelist_Dynamically()
    {
        using var detector = new LanguageDetector();
        await Task.Delay(100);

        string testWord = "choyxona";
        Assert.IsTrue(detector.ShouldConvert(testWord, ConversionMode.Strict));

        // Add to whitelist
        await File.AppendAllLinesAsync(WhitelistPath, new[] { testWord }, Encoding.UTF8);
        await Task.Delay(400); // Wait for whitelist debounce to settle

        // Now add same word to blacklist
        var sw = Stopwatch.StartNew();
        await File.AppendAllLinesAsync(BlacklistPath, new[] { testWord }, Encoding.UTF8);

        bool blacklisted = false;
        long elapsedMs = 0;
        while (sw.ElapsedMilliseconds < 1500)
        {
            if (!detector.ShouldConvert(testWord, ConversionMode.Strict))
            {
                blacklisted = true;
                elapsedMs = sw.ElapsedMilliseconds;
                break;
            }
            await Task.Delay(10);
        }

        Console.WriteLine($"[HotReload Blacklist Precedence] Reflected in {elapsedMs} ms");
        Assert.IsTrue(blacklisted, "Blacklist did not take precedence over whitelist dynamically.");
        Assert.IsTrue(elapsedMs <= 500,
            $"Dynamic blacklist precedence must reflect within 500ms. Actual: {elapsedMs} ms");
    }

    [TestMethod]
    public async Task HotReload_RemovalReversion_ReflectedWithin500ms()
    {
        using var detector = new LanguageDetector();
        await Task.Delay(100);

        string testWord = "shahar";
        // 1. Blacklist shahar
        await File.AppendAllLinesAsync(BlacklistPath, new[] { testWord }, Encoding.UTF8);
        await Task.Delay(400);
        Assert.IsFalse(detector.ShouldConvert(testWord, ConversionMode.Strict),
            "Precondition: shahar must be blacklisted.");

        // 2. Clear blacklist
        var sw = Stopwatch.StartNew();
        await File.WriteAllTextAsync(BlacklistPath, "# Clean Blacklist\n", Encoding.UTF8);

        bool restored = false;
        long elapsedMs = 0;
        while (sw.ElapsedMilliseconds < 1500)
        {
            if (detector.ShouldConvert(testWord, ConversionMode.Strict))
            {
                restored = true;
                elapsedMs = sw.ElapsedMilliseconds;
                break;
            }
            await Task.Delay(10);
        }

        Console.WriteLine($"[HotReload Blacklist Removal] Restored in {elapsedMs} ms");
        Assert.IsTrue(restored, "Hot-reload failed to restore word after blacklist clearance.");
        Assert.IsTrue(elapsedMs <= 500,
            $"Hot-reload restoration must reflect within 500ms. Actual: {elapsedMs} ms");
    }

    [TestMethod]
    public async Task HotReload_CasingAndApostropheVariants_HandledInOverrides()
    {
        using var detector = new LanguageDetector();
        await Task.Delay(100);

        // Add custom word with 1995 standard apostrophe to whitelist
        string whitelisted = "qo'shiqchi";
        await File.AppendAllLinesAsync(WhitelistPath, new[] { whitelisted }, Encoding.UTF8);

        // Wait for hot-reload
        await Task.Delay(400);

        // Check various cases and apostrophe variants
        Assert.IsTrue(detector.ShouldConvert("qo'shiqchi", ConversionMode.Strict), "Exact match failed");
        Assert.IsTrue(detector.ShouldConvert("QO'SHIQCHI", ConversionMode.Strict), "Uppercase match failed");
        Assert.IsTrue(detector.ShouldConvert("Qo'shiqchi", ConversionMode.Strict), "TitleCase match failed");
        Assert.IsTrue(detector.ShouldConvert("qoʻshiqchi", ConversionMode.Strict), "Modifier letter apostrophe failed");
        Assert.IsTrue(detector.ShouldConvert("qo‘shiqchi", ConversionMode.Strict), "Left quotation mark apostrophe failed");
        Assert.IsTrue(detector.ShouldConvert("qo’shiqchi", ConversionMode.Strict), "Right quotation mark apostrophe failed");
    }

    // ══════════════════════════════════════════════════════════════════
    // 2. MULTI-THREADED CONCURRENCY & STRESS (100+ THREADS)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Concurrency_128Threads_UnderContinuousReload_ZeroErrors()
    {
        using var detector = new LanguageDetector();

        const int threadCount = 128;
        const int iterationsPerThread = 2000; // 128 * 2000 = 256,000 total operations
        var exceptions = new ConcurrentBag<Exception>();

        string[] testPool =
        [
            "shahar", "o'rtoq", "choyxona", "g'alaba", "mus'haf", "Is'hoq",
            "show", "crash", "switch", "match", "fetch", "catch", "machine",
            "shaharlarimizda", "o'rtog'imizga", "qishlog'i", "yuragim", "boshlayapti",
            "kitob", "qalam", "kompyuter", "dastur", "", "a", "sh", "ch", "o'", "g'",
            "O'RTOQ", "SHAHAR", "g'oliblarimiz", "eshitmaganlar", "customword_test"
        ];

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var token = cts.Token;

        // Background worker 1: Continuously triggers ReloadUserLists
        var reloadTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                detector.ReloadUserLists();
                await Task.Delay(5, token).ConfigureAwait(false);
            }
        }, token);

        // Background worker 2: Continuously modifies Whitelist/Blacklist
        var mutateTask = Task.Run(async () =>
        {
            int counter = 0;
            while (!token.IsCancellationRequested)
            {
                string word = $"temp_word_{counter++ % 20}";
                if (counter % 2 == 0)
                    detector.AddToWhitelist(word);
                else
                    detector.AddToBlacklist(word);

                await Task.Delay(10, token).ConfigureAwait(false);
            }
        }, token);

        // 128 Parallel worker threads pounding ShouldConvert and IsUzbek
        var tasks = new Task[threadCount];
        for (int t = 0; t < threadCount; t++)
        {
            int threadId = t;
            tasks[t] = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < iterationsPerThread; i++)
                    {
                        string word = testPool[(threadId + i) % testPool.Length];
                        var mode = (i % 2 == 0) ? ConversionMode.Strict : ConversionMode.Aggressive;

                        bool conv = detector.ShouldConvert(word, mode);
                        bool isUzbek = detector.IsUzbek(word, mode);

                        // Invariant: If not convertible, ShouldConvert must always be false
                        if (!UzbekPhonetics.HasConvertiblePatterns(word))
                        {
                            if (conv)
                                throw new InvalidOperationException($"ShouldConvert returned true for non-convertible '{word}'!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }

        // Wait for all reader threads to complete
        Task.WaitAll(tasks);
        cts.Cancel(); // Stop background writers

        try
        {
            Task.WaitAll(reloadTask, mutateTask);
        }
        catch (AggregateException)
        {
            // Expected on cancellation
        }

        Console.WriteLine($"[Concurrency Stress] 128 threads executed {threadCount * iterationsPerThread:N0} calls. Exceptions: {exceptions.Count}");

        if (!exceptions.IsEmpty)
        {
            var first = exceptions.First();
            Assert.Fail($"Concurrency failure detected! Total exceptions: {exceptions.Count}. First error: {first.Message}\n{first.StackTrace}");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // 3. PERFORMANCE BENCHMARK (100,000 QUERIES, LATENCY < 20 µs/WORD)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Benchmark_100kQueries_LatencyStrictlyUnder20Microseconds()
    {
        using var detector = new LanguageDetector();

        // Representative diverse workload:
        // - Tier 2: Uzbek core words
        // - Tier 3: Inflected morphology
        // - Tier 1: English & Programming rejects
        // - Fast-path: Non-convertible words
        // - Unknown / foreign words with convertible patterns
        string[] benchmarkWords =
        [
            // Tier 2: Core Uzbek words
            "shahar", "o'rtoq", "choyxona", "g'alaba", "o'rmon", "qo'g'irchoq", "sho'rva", "chashma",
            "yashamoq", "oshxona", "chiroq", "og'ir", "bog'bon", "to'g'ri", "to'qqiz", "o'n",

            // Tier 3: Inflected morphology
            "shaharlarimizda", "o'rtoqlarimizdan", "qishlog'i", "qishlog'imizda", "yuragim", "eshigimiz",
            "boshlayapti", "boshlamoqchiman", "gapiryapti", "tushundi", "eshitmaganlar", "do'stlikka",

            // Tier 1: English & Programming rejects
            "show", "sheet", "crash", "push", "child", "church", "change", "machine",
            "switch", "match", "fetch", "catch", "shell", "shift", "char", "echo",

            // Fast-path: Non-convertible words
            "kitob", "qalam", "daftar", "maktab", "ustoz", "dastur", "kompyuter", "kod",

            // Unknown / foreign words with convertible patterns
            "washington", "michigan", "chicago", "screenshot", "powershell", "checkout", "pushdown", "blockchain"
        ];

        const int totalQueries = 100_000;
        var queries = new string[totalQueries];
        for (int i = 0; i < totalQueries; i++)
        {
            queries[i] = benchmarkWords[i % benchmarkWords.Length];
        }

        // Warm up JIT (1,000 queries)
        for (int i = 0; i < 1000; i++)
        {
            detector.ShouldConvert(queries[i], ConversionMode.Strict);
        }

        // Execute 100,000 queries and measure precise latency
        var individualLatenciesUs = new double[totalQueries];
        var swTotal = Stopwatch.StartNew();

        for (int i = 0; i < totalQueries; i++)
        {
            long startTicks = Stopwatch.GetTimestamp();
            detector.ShouldConvert(queries[i], ConversionMode.Strict);
            long endTicks = Stopwatch.GetTimestamp();

            individualLatenciesUs[i] = (endTicks - startTicks) * (1_000_000.0 / Stopwatch.Frequency);
        }

        swTotal.Stop();

        // Calculate statistics
        Array.Sort(individualLatenciesUs);
        double totalTimeMs = swTotal.Elapsed.TotalMilliseconds;
        double avgLatencyUs = (swTotal.Elapsed.TotalMicroseconds) / totalQueries;
        double p50Us = individualLatenciesUs[(int)(totalQueries * 0.50)];
        double p90Us = individualLatenciesUs[(int)(totalQueries * 0.90)];
        double p95Us = individualLatenciesUs[(int)(totalQueries * 0.95)];
        double p99Us = individualLatenciesUs[(int)(totalQueries * 0.99)];
        double maxLatencyUs = individualLatenciesUs[^1];

        Console.WriteLine("═════════════════════════════════════════════════════════════");
        Console.WriteLine($"[Benchmark Results: {totalQueries:N0} queries]");
        Console.WriteLine($"Total Execution Time : {totalTimeMs:F2} ms");
        Console.WriteLine($"Throughput           : {totalQueries / (totalTimeMs / 1000.0):N0} queries/sec");
        Console.WriteLine($"Mean Latency         : {avgLatencyUs:F3} µs/word (Target: < 20.000 µs)");
        Console.WriteLine($"Median (p50) Latency : {p50Us:F3} µs");
        Console.WriteLine($"90th Percentile (p90): {p90Us:F3} µs");
        Console.WriteLine($"95th Percentile (p95): {p95Us:F3} µs");
        Console.WriteLine($"99th Percentile (p99): {p99Us:F3} µs");
        Console.WriteLine($"Max Latency          : {maxLatencyUs:F3} µs");
        Console.WriteLine("═════════════════════════════════════════════════════════════");

        Assert.IsTrue(avgLatencyUs < 20.0,
            $"Average latency must be < 20 µs per word. Actual: {avgLatencyUs:F3} µs/word");
        Assert.IsTrue(p99Us < 50.0,
            $"99th percentile latency must be under 50 µs. Actual: {p99Us:F3} µs");
    }

    // ══════════════════════════════════════════════════════════════════
    // 4. ADVERSARIAL DISCRIMINATION & MORPHOLOGY CORNER CASES
    // ══════════════════════════════════════════════════════════════════

    [DataTestMethod]
    [DataRow("chin", false, "English reject: 'chin' should not convert in strict mode")]
    [DataRow("chindan", true, "Uzbek inflected form: 'chindan' is Uzbek and should convert")]
    [DataRow("chop", false, "English reject: 'chop' should not convert in strict mode")]
    [DataRow("chopmoq", true, "Uzbek verb: 'chopmoq' should convert")]
    [DataRow("ship", false, "English reject: 'ship' should not convert")]
    [DataRow("shirintoy", true, "Uzbek word: 'shirintoy' should convert")]
    public void HomographDiscrimination_BareVsInflected(string word, bool expectedConvert, string reason)
    {
        using var detector = new LanguageDetector();
        bool actual = detector.ShouldConvert(word, ConversionMode.Strict);
        Assert.AreEqual(expectedConvert, actual, reason);
    }

    [DataTestMethod]
    [DataRow("Is'hoq", false, "Syllable break word should not convert")]
    [DataRow("mus'haf", false, "Syllable break word should not convert")]
    [DataRow("as'hob", false, "Syllable break word should not convert")]
    [DataRow("mas'ul", false, "Syllable break word should not convert")]
    public void SyllableBreak_NonConversionGuaranteed(string word, bool expected, string reason)
    {
        using var detector = new LanguageDetector();
        bool actual = detector.ShouldConvert(word, ConversionMode.Strict);
        Assert.AreEqual(expected, actual, reason);
    }

    [TestMethod]
    public void ExtremeBoundaryWords_DoNotCrashOrHang()
    {
        using var detector = new LanguageDetector();

        // 10,000-character repetitive string
        string massiveString = new string('o', 5000) + "'" + new string('q', 5000);
        bool res1 = detector.ShouldConvert(massiveString, ConversionMode.Strict);
        Assert.IsFalse(res1);

        // Many apostrophes
        string apostropheMadness = "o''''''''''''''''''g'''''''''''''''''';;;;;;;;shshshshchchch";
        bool res2 = detector.ShouldConvert(apostropheMadness, ConversionMode.Strict);
        Assert.IsFalse(res2);

        // Control characters and zero-width spaces
        string zeroWidth = "sh\u200Bah\u200Bar";
        bool res3 = detector.ShouldConvert(zeroWidth, ConversionMode.Strict);
        Assert.IsNotNull(res3);
    }
}
