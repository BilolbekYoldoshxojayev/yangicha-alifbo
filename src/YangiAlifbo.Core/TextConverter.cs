// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — TextConverter.cs
// Converts a whole text the way the keyboard hook converts typed words.
// Shared by the control-center playgrounds.
// ──────────────────────────────────────────────────────────────────────

using System.Text;
using System.Text.RegularExpressions;
using YangiAlifbo.Win32;

namespace YangiAlifbo.Core;

public static class TextConverter
{
    // Same delimiters as KeyboardHook, except '-' which joins 5-chi, o'z-o'zidan.
    private static readonly Regex Tokens = new(@"(\s+|[.,!?:;""\(\)\[\]\{\}\/\\])", RegexOptions.Compiled);

    /// <summary>Converts <paramref name="input"/> and reports how many words changed.</summary>
    public static string Convert(string input, LanguageDetector detector, TransliterationMode mode, bool slang, out int changed)
    {
        changed = 0;
        if (string.IsNullOrEmpty(input)) return input;

        var sb = new StringBuilder(input.Length);
        foreach (string token in Tokens.Split(input))
        {
            if (token.Length == 0) continue;
            string result = ConvertWord(token, detector, mode, slang);
            if (!ReferenceEquals(result, token) && result != token) changed++;
            sb.Append(result);
        }
        return sb.ToString();
    }

    private static string ConvertWord(string token, LanguageDetector detector, TransliterationMode mode, bool slang)
    {
        if (token.Length < 2) return token;
        char first = UzbekPhonetics.NormalizeApostrophes(token[..1])[0];
        if (!(char.IsLetter(first) || char.IsDigit(first) || first == '\''))
            return token;

        // Same decision as the keyboard hook.
        return WordConverter.Convert(detector, token, mode, slang) ?? token;
    }
}
