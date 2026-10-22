using System;
using System.IO;
using System.Text;
using YangiAlifbo.Core;
using YangiAlifbo.Win32;

namespace YangiAlifbo.Tools.Eval;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        var mode = args.Contains("--aggressive") ? TransliterationMode.Aggressive : TransliterationMode.Strict;
        bool slang = args.Contains("--slang");

        // Load standard dictionaries
        using var detector = new LanguageDetector();

        if (args.Length > 0 && !args[0].StartsWith("-") && File.Exists(args[0]))
        {
            string input = File.ReadAllText(args[0], Encoding.UTF8);
            string output = TextConverter.Convert(input, detector, mode, slang, out _);
            Console.Write(output);
            return 0;
        }

        // Read from stdin line by line or entire stream
        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            string output = TextConverter.Convert(line, detector, mode, slang, out _);
            Console.WriteLine(output);
        }

        return 0;
    }
}
