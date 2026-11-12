// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — WordConverter.cs
// Decides what one finished word becomes. Used by the Windows keyboard
// hook and the Linux input method (libyangialifbo), so both platforms
// convert exactly alike.
// ──────────────────────────────────────────────────────────────────────

using YangiAlifbo.Win32;

namespace YangiAlifbo.Core;

public static class WordConverter
{
    /// <summary>
    /// Converts a word typed in 1995 Uzbek Latin to the reformed alphabet.
    /// Returns the converted word, or null if no conversion should happen.
    /// </summary>
    public static string? Convert(LanguageDetector detector, string word, TransliterationMode mode, bool slang)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 2)
            return null;

        // Quote marks wrapped around a word ('o'z', o'z') are not part of it.
        string norm = UzbekPhonetics.NormalizeApostrophes(word);
        int start = 0, end = norm.Length;
        while (start < end && norm[start] == '\'') start++;
        // A trailing ' after o/g is the tutuq of the word itself (tog', bog'), unless a leading quote pairs with it.
        while (end > start && norm[end - 1] == '\'' &&
               (start > 0 || end - 2 < start || "ogOG".IndexOf(norm[end - 2]) < 0)) end--;
        if (start > 0 || end < norm.Length)
        {
            if (end - start < 2) return null;
            string? inner = Convert(detector, word[start..end], mode, slang);
            return inner == null ? null : word[..start] + inner + word[end..];
        }

        // Tier 1: Absolute protection for English and foreign words
        if (detector.IsForeignOrReject(word))
            return null;

        string workWord = slang ? ChatNormalizer.Normalize(word) : word;

        // Quick check: does the workWord contain convertible patterns?
        if (!UzbekPhonetics.HasConvertiblePatterns(workWord))
        {
            // A slang rewrite without sh/ch/o'/g' (bilmadm -> bilmadim, kirasz -> kirasiz)
            // is used only when the result is a known Uzbek word.
            if (workWord != word && detector.IsUzbek(workWord))
                return workWord;
            return null;
        }

        // In Aggressive mode, skip dictionary checks
        if (mode == TransliterationMode.Aggressive)
            return UzbekPhonetics.Convert(workWord);

        // Strict mode: verify the word is Uzbek before converting. The neural
        // fallback only judges words as typed, never slang rewrites.
        if (workWord == word ? detector.IsUzbekOrLikely(workWord) : detector.IsUzbek(workWord))
            return UzbekPhonetics.Convert(workWord);

        return null;
    }
}
