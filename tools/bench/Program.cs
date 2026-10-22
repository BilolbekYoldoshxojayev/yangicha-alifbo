using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using YangiAlifbo.Core;
using YangiAlifbo.Win32;

namespace YangiAlifbo.Tools.Bench;

public sealed class BenchmarkReport
{
    public long LexiconFileSizeBytes { get; set; }
    public long RejectFileSizeBytes { get; set; }
    public double LoadTimeMilliseconds { get; set; }
    public double RamDeltaMegabytes { get; set; }
    public double TotalWorkingSetMegabytes { get; set; }
    public double ExactMatchLatencyMicros { get; set; }
    public double MorphologyMatchLatencyMicros { get; set; }
    public double EnglishRejectLatencyMicros { get; set; }
    public double UnknownWordLatencyMicros { get; set; }
    public double PhoneticsConvertLatencyMicros { get; set; }
    public double SentenceConvertLatencyMicros { get; set; }
    public double ThroughputWordsPerSecond { get; set; }
}

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("=== YangiAlifbo C# Baseline Performance Benchmark ===");

        string repoRoot = FindRepoRoot();
        string dictDir = Path.Combine(repoRoot, "src", "Core", "Dictionaries");
        string uzbekWordsPath = Path.Combine(dictDir, "uzbek_words.txt");
        string englishRejectPath = Path.Combine(dictDir, "english_reject.txt");

        long uzbekSize = File.Exists(uzbekWordsPath) ? new FileInfo(uzbekWordsPath).Length : 0;
        long rejectSize = File.Exists(englishRejectPath) ? new FileInfo(englishRejectPath).Length : 0;

        Console.WriteLine($"Dictionary on disk: uzbek_words.txt = {uzbekSize:N0} bytes ({(uzbekSize / 1024.0 / 1024.0):F2} MB)");
        Console.WriteLine($"Reject list on disk: english_reject.txt = {rejectSize:N0} bytes ({(rejectSize / 1024.0):F2} KB)");

        // Force GC before measuring initial memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long memBefore = Process.GetCurrentProcess().WorkingSet64;

        // Measure Load Time
        var sw = Stopwatch.StartNew();
        var detector = new LanguageDetector();
        sw.Stop();
        double loadMs = sw.Elapsed.TotalMilliseconds;

        // Force GC and measure memory after load
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long memAfter = Process.GetCurrentProcess().WorkingSet64;
        double ramDeltaMb = (memAfter - memBefore) / (1024.0 * 1024.0);
        double totalWsMb = memAfter / (1024.0 * 1024.0);

        Console.WriteLine($"\n--- Resource Footprint ---");
        Console.WriteLine($"Load Time: {loadMs:F2} ms");
        Console.WriteLine($"RAM Delta: {ramDeltaMb:F2} MB (Total Working Set: {totalWsMb:F2} MB)");

        // Warm up JIT
        detector.ShouldConvert("shahar", ConversionMode.Strict);
        detector.ShouldConvert("shock", ConversionMode.Strict);
        detector.ShouldConvert("maktabimizdagilardan", ConversionMode.Strict);
        UzbekPhonetics.Convert("shahar");
        TextConverter.Convert("Salom dunyo, bugun ob-havo yaxshi bo'ladi.", detector, TransliterationMode.Strict, false, out _);

        // Benchmark Word Lookups
        const int iterations = 100_000;
        Console.WriteLine($"\n--- Latency Benchmark ({iterations:N0} iterations each) ---");

        // 1. Exact Uzbek match: "shahar"
        double exactMicros = BenchmarkAction(() => detector.ShouldConvert("shahar", ConversionMode.Strict), iterations);
        Console.WriteLine($"Exact Uzbek match ('shahar'):         {exactMicros:F3} µs / op");

        // 2. Morphological match: "shaharimizdagilardan"
        double morphMicros = BenchmarkAction(() => detector.ShouldConvert("shaharimizdagilardan", ConversionMode.Strict), iterations);
        Console.WriteLine($"Morphological match ('shaharimizdagilardan'): {morphMicros:F3} µs / op");

        // 3. English reject: "shock"
        double rejectMicros = BenchmarkAction(() => detector.ShouldConvert("shock", ConversionMode.Strict), iterations);
        Console.WriteLine($"English reject ('shock'):            {rejectMicros:F3} µs / op");

        // 4. Unknown word: "qwertyuiop"
        double unknownMicros = BenchmarkAction(() => detector.ShouldConvert("qwertyuiop", ConversionMode.Strict), iterations);
        Console.WriteLine($"Unknown word ('qwertyuiop'):         {unknownMicros:F3} µs / op");

        // 5. Phonetics Convert: "shahar" -> "şahar"
        double phoneticsMicros = BenchmarkAction(() => UzbekPhonetics.Convert("shahar"), iterations);
        Console.WriteLine($"Phonetics Convert ('shahar' -> 'şahar'): {phoneticsMicros:F3} µs / op");

        // 6. Sentence conversion
        string sampleSentence = "Bugun 5-chi sinf o'quvchilari maktabga kelishdi va yangi dars boshlandi.";
        const int sentenceIter = 20_000;
        double sentenceMicros = BenchmarkAction(() => TextConverter.Convert(sampleSentence, detector, TransliterationMode.Strict, false, out _), sentenceIter);
        int wordsInSentence = sampleSentence.Split(' ').Length;
        double wordsPerSec = (wordsInSentence / (sentenceMicros / 1_000_000.0));
        Console.WriteLine($"Sentence conversion ({wordsInSentence} words):        {sentenceMicros:F3} µs / op ({wordsPerSec:N0} words/sec)");

        var report = new BenchmarkReport
        {
            LexiconFileSizeBytes = uzbekSize,
            RejectFileSizeBytes = rejectSize,
            LoadTimeMilliseconds = loadMs,
            RamDeltaMegabytes = ramDeltaMb,
            TotalWorkingSetMegabytes = totalWsMb,
            ExactMatchLatencyMicros = exactMicros,
            MorphologyMatchLatencyMicros = morphMicros,
            EnglishRejectLatencyMicros = rejectMicros,
            UnknownWordLatencyMicros = unknownMicros,
            PhoneticsConvertLatencyMicros = phoneticsMicros,
            SentenceConvertLatencyMicros = sentenceMicros,
            ThroughputWordsPerSecond = wordsPerSec
        };

        string outDir = Path.Combine(repoRoot, "tools", "bench");
        string reportJsonPath = Path.Combine(outDir, "baseline_benchmark.json");
        File.WriteAllText(reportJsonPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"\nBenchmark report saved to: {reportJsonPath}");

        detector.Dispose();
        return 0;
    }

    private static double BenchmarkAction(Action action, int iterations)
    {
        // 1000 warmup iterations
        for (int i = 0; i < 1000; i++) action();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            action();
        }
        sw.Stop();

        return (sw.Elapsed.TotalMilliseconds * 1000.0) / iterations;
    }

    private static string FindRepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "YangiAlifbo.sln")) || Directory.Exists(Path.Combine(dir, ".agent")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName ?? "";
        }
        return Directory.GetCurrentDirectory();
    }
}
