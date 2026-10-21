// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — UzbekPhonetics.cs
// Transliteration rules: 1995 Uzbek Latin → 2024 Reformed Latin.
// Handles case preservation, apostrophe-boundary exceptions, and
// all 9 apostrophe/quote character variants.
// ──────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Text;

namespace YangiAlifbo.Core;

/// <summary>
/// Implements the official 2024 Uzbek Latin alphabet reform rules:
///   sh → ş,  ch → ç,  oʻ → ö,  gʻ → ğ
/// with full case preservation, 9-variant apostrophe normalization,
/// and apostrophe-boundary / syllable-break protection.
/// </summary>
public sealed class UzbekPhonetics
{
    /// <summary>
    /// All 9 characters recognized as apostrophe/tutuq belgisi variants
    /// in Uzbek text. Normalized to standard ASCII <c>'</c> (U+0027).
    /// </summary>
    private static readonly HashSet<char> ApostropheVariants = new()
    {
        '\'',     // 1. U+0027  APOSTROPHE (standard keyboard)
        '\u2018', // 2. U+2018  LEFT SINGLE QUOTATION MARK
        '\u2019', // 3. U+2019  RIGHT SINGLE QUOTATION MARK
        '\u02BB', // 4. U+02BB  MODIFIER LETTER TURNED COMMA (official 1995 Uzbek)
        '\u02BC', // 5. U+02BC  MODIFIER LETTER APOSTROPHE (official tutuq belgisi)
        '`',      // 6. U+0060  GRAVE ACCENT (common typo substitute)
        '\u00B4', // 7. U+00B4  ACUTE ACCENT
        '\u201B', // 8. U+201B  SINGLE HIGH-REVERSED-9 QUOTATION MARK
        '\u2032', // 9. U+2032  PRIME
    };

    /// <summary>
    /// Quickly checks whether a word contains any convertible character sequences
    /// (sh, ch, o', g') in any casing or apostrophe variant.
    /// Syllable-break words like Is'hoq or mus'haf do NOT count as convertible
    /// unless they contain another convertible sequence.
    /// </summary>
    public static bool HasConvertiblePatterns(string word)
    {
        if (string.IsNullOrEmpty(word) || word.Length < 2)
            return false;

        string normalized = NormalizeApostrophes(word);

        // Mark s'h and c'h morpheme boundaries to avoid false positives
        var protectedIndices = new HashSet<int>();
        for (int i = 0; i < normalized.Length - 2; i++)
        {
            char lo = char.ToLowerInvariant(normalized[i]);
            char mid = normalized[i + 1];
            char next = char.ToLowerInvariant(normalized[i + 2]);

            if ((lo == 's' || lo == 'c') && mid == '\'' && next == 'h')
            {
                protectedIndices.Add(i);
                protectedIndices.Add(i + 1);
                protectedIndices.Add(i + 2);
            }
        }

        for (int i = 0; i < normalized.Length - 1; i++)
        {
            if (protectedIndices.Contains(i) || protectedIndices.Contains(i + 1))
                continue;

            char lo = char.ToLowerInvariant(normalized[i]);
            char next = normalized[i + 1];
            char loNext = char.ToLowerInvariant(next);

            // sh or ch
            if ((lo == 's' || lo == 'c') && loNext == 'h')
                return true;

            // o' or g'
            if ((lo == 'o' || lo == 'g') && next == '\'')
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if a word has convertible character sequences (o', g', sh, ch).
    /// Alias for <see cref="HasConvertiblePatterns(string)"/> specified by PROJECT.md contract.
    /// </summary>
    public static bool HasConvertibleSequences(string word) => HasConvertiblePatterns(word);

    /// <summary>
    /// Converts a word or text from 1995 Uzbek Latin orthography to the 2024 reformed
    /// Latin alphabet:
    ///   sh → ş,  ch → ç,  oʻ → ö,  gʻ → ğ
    /// Strictly preserves letter casing (lower, TitleCase, ALL CAPS, double-cap)
    /// and syllable/morpheme breaks with tutuq separating s-h or c-h.
    /// </summary>
    /// <param name="word">The word or phrase in 1995 Uzbek Latin spelling.</param>
    /// <returns>The text in 2024 reformed spelling.</returns>
    public static string Convert(string word)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        // Step 1: Normalize all 9 apostrophe variants to standard ASCII apostrophe (')
        string normalizedWord = NormalizeApostrophes(word);

        // Step 2: Build char list with case tracking
        var chars = new List<CharInfo>(normalizedWord.Length);
        for (int i = 0; i < normalizedWord.Length; i++)
        {
            chars.Add(new CharInfo(normalizedWord[i], char.IsUpper(normalizedWord[i])));
        }

        // Pass 1: Mark protected morpheme and syllable boundaries (s'h, c'h)
        var protectedPositions = new HashSet<int>();
        for (int i = 0; i < chars.Count - 2; i++)
        {
            char lo = char.ToLowerInvariant(chars[i].Char);
            char mid = chars[i + 1].Char;
            char next = char.ToLowerInvariant(chars[i + 2].Char);

            // s'h → protect: do NOT convert to ş (e.g. Is'hoq, mus'haf, as'hob)
            if (lo == 's' && mid == '\'' && next == 'h')
            {
                protectedPositions.Add(i);
                protectedPositions.Add(i + 1);
                protectedPositions.Add(i + 2);
            }
            // c'h → protect: do NOT convert to ç
            if (lo == 'c' && mid == '\'' && next == 'h')
            {
                protectedPositions.Add(i);
                protectedPositions.Add(i + 1);
                protectedPositions.Add(i + 2);
            }
        }

        // Pass 2: Apply transliteration rules
        var result = new List<char>(chars.Count);
        int idx = 0;

        while (idx < chars.Count)
        {
            // Skip protected positions (apostrophe-boundary sequences)
            if (protectedPositions.Contains(idx))
            {
                result.Add(chars[idx].Char);
                idx++;
                continue;
            }

            // Try two-character patterns
            if (idx + 1 < chars.Count && !protectedPositions.Contains(idx + 1))
            {
                char lo = char.ToLowerInvariant(chars[idx].Char);
                char nextChar = chars[idx + 1].Char;
                char loNext = char.ToLowerInvariant(nextChar);
                bool isUpper1 = chars[idx].IsUpper;

                // ── sh → ş ────────────────────────────────────────
                if (lo == 's' && loNext == 'h')
                {
                    char replacement = isUpper1 ? 'Ş' : 'ş';
                    result.Add(replacement);
                    idx += 2;
                    continue;
                }

                // ── ch → ç ────────────────────────────────────────
                if (lo == 'c' && loNext == 'h')
                {
                    char replacement = isUpper1 ? 'Ç' : 'ç';
                    result.Add(replacement);
                    idx += 2;
                    continue;
                }

                // ── o' → ö ────────────────────────────────────────
                if (lo == 'o' && nextChar == '\'')
                {
                    char replacement = isUpper1 ? 'Ö' : 'ö';
                    result.Add(replacement);
                    idx += 2;
                    continue;
                }

                // ── g' → ğ ────────────────────────────────────────
                if (lo == 'g' && nextChar == '\'')
                {
                    char replacement = isUpper1 ? 'Ğ' : 'ğ';
                    result.Add(replacement);
                    idx += 2;
                    continue;
                }
            }

            // No pattern matched — pass character through
            result.Add(chars[idx].Char);
            idx++;
        }

        return new string(result.ToArray());
    }

    /// <summary>
    /// Normalizes all 9 apostrophe variants in a string to the standard
    /// apostrophe character (U+0027) for consistent pattern matching and orthography.
    /// Recognized variants: ' (U+0027), ‘ (U+2018), ’ (U+2019), ʻ (U+02BB),
    /// ʼ (U+02BC), ` (U+0060), ´ (U+00B4), ‛ (U+201B), ′ (U+2032).
    /// </summary>
    public static string NormalizeApostrophes(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            sb.Append(ApostropheVariants.Contains(c) ? '\'' : c);
        }
        return sb.ToString();
    }

    /// <summary>Holds a character and its case information.</summary>
    private readonly record struct CharInfo(char Char, bool IsUpper);
}
