// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — Exports.cs
// C entry points of libyangialifbo.so, called by the Linux input method
// (linux/engine/yangialifbo_engine.py, through ctypes). Strings are UTF-8.
// ──────────────────────────────────────────────────────────────────────

using System.Runtime.InteropServices;
using System.Text;
using YangiAlifbo.Core;
using YangiAlifbo.Win32;

namespace YangiAlifbo.Native;

public static unsafe class Exports
{
    private static LanguageDetector? _detector;
    private static readonly object Gate = new();
    private static byte* _version;

    /// <summary>Loads the dictionaries and the neural model. Returns 1 on success.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ya_init")]
    public static int Init()
    {
        try
        {
            lock (Gate) _detector ??= new LanguageDetector();
            return 1;
        }
        catch (Exception ex)
        {
            AppLog.Write("ya_init: " + ex);
            return 0;
        }
    }

    /// <summary>
    /// Converts one finished word. <paramref name="mode"/>: 0 strict, 1 aggressive.
    /// Returns a UTF-8 string to release with ya_free, or null when the word stays as typed.
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "ya_convert_word")]
    public static byte* ConvertWord(byte* word, int mode, int slang, int neural)
    {
        try
        {
            var d = _detector;
            if (d == null || word == null) return null;
            string w = Marshal.PtrToStringUTF8((IntPtr)word) ?? "";
            d.NeuralEnabled = neural != 0;
            string? result = WordConverter.Convert(d, w,
                mode == 1 ? TransliterationMode.Aggressive : TransliterationMode.Strict, slang != 0);
            if (result == null || result == w) return null;

            int n = Encoding.UTF8.GetByteCount(result);
            byte* p = (byte*)NativeMemory.Alloc((nuint)n + 1);
            fixed (char* c = result) Encoding.UTF8.GetBytes(c, result.Length, p, n);
            p[n] = 0;
            return p;
        }
        catch (Exception ex)
        {
            AppLog.Write("ya_convert_word: " + ex);
            return null;
        }
    }

    /// <summary>Frees a string returned by ya_convert_word.</summary>
    [UnmanagedCallersOnly(EntryPoint = "ya_free")]
    public static void Free(byte* p) => NativeMemory.Free(p);

    /// <summary>Library version, e.g. "2.0.0" (static, do not free).</summary>
    [UnmanagedCallersOnly(EntryPoint = "ya_version")]
    public static byte* GetVersion()
    {
        if (_version == null)
        {
            byte[] v = Encoding.UTF8.GetBytes(typeof(Exports).Assembly.GetName().Version?.ToString(3) ?? "0.0.0");
            byte* p = (byte*)NativeMemory.Alloc((nuint)v.Length + 1);
            v.CopyTo(new Span<byte>(p, v.Length));
            p[v.Length] = 0;
            _version = p;   // kept for the life of the process
        }
        return _version;
    }
}
