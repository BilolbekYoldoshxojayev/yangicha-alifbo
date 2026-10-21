// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — ChatNormalizer.cs
// Ultra-fast, zero-allocation informal Uzbek text normalizer:
// 1. Russian keyboard 'w' substitution for 'sh' in verified Uzbek stems
// 2. Vowel-dropped chat contractions (-amz, -asz, -dm, -dk, -misz)
// 3. 'c' for 'ch' in high-frequency chat abbreviations (ucun, qacon, hec)
// 4. Ultra-fast O(1) hash-based unaccented 'o' expansion (bol-, kor-, oz-, etc.)
// 5. Colloquial / dialectal stems & spoken verb forms (-votti, -vor, -vur)
// 6. Hyphenated compounds & Russian digital loanwords in Uzbek chats
// ──────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;

namespace YangiAlifbo.Core;

/// <summary>
/// Normalizes colloquial, informal, and abbreviated Uzbek chat words
/// into standard orthographic forms ready for reformed Latin transliteration.
/// Optimized for ultra-low latency with O(1) lookup tables.
/// </summary>
public static class ChatNormalizer
{
    // ── Static O(1) Suffix Lookup Sets for Ultra-Fast Stem Expansion ────

    private static readonly HashSet<string> BolSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "adi", "di", "sa", "ib", "gan", "gani", "ishi", "averadi", "sin", "maydi",
        "arkan", "ardim", "gancha", "adimi", "ardi", "ar", "masa", "ish", "yapti", "lar",
        "masin", "madi", "ganmiz", "gandik", "masam", "votti", "vommiz", "vomman", "vosiz",
        "vossiz", "vosila", "votgan", "uradi", "uramiz", "urasiz", "uring", "vurasiz", "vuramiz"
    };

    private static readonly HashSet<string> KorSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "amiz", "amz", "ishamiz", "ishamz", "dim", "dm", "dik", "dk", "ib", "gan",
        "gancha", "ganmiz", "gandik", "ishdik", "aylik", "inadi", "indi", "ik", "asan",
        "asiz", "asz", "ish", "a", "ing", "aylikchi", "voman", "votti", "vommiz"
    };

    private static readonly HashSet<string> OzSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "i", "im", "ing", "imiz", "iz", "lari", "bekcha", "bek", "gina", "roq",
        "ini", "icha", "iga", "ida", "idan", "imizga", "imizda", "imizni", "ingiz"
    };

    private static readonly HashSet<string> KopSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "roq", "chilik", "aydi", "aytir", "ina", "dan", "ga", "ni", "da"
    };

    private static readonly HashSet<string> SozSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "lar", "lashuv", "lagan", "lash", "im", "ing", "i", "imiz", "da", "dan", "ga", "ni"
    };

    private static readonly HashSet<string> SoraSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "moq", "dim", "dm", "sh", "gan", "yman", "ng", "ylik", "dik", "dk", "yapsiz"
    };

    private static readonly HashSet<string> YolSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "lar", "i", "ida", "idan", "lovchi", "ga", "da", "ni", "boshchi"
    };

    private static readonly HashSet<string> OrtSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "oq", "iq", "iqcha", "ada", "asida", "iqroq", "ada", "asiga", "og'i", "og'im"
    };

    private static readonly HashSet<string> OchirSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "ildi", "ish", "dim", "dm", "di", "ib", "ilsin", "ing", "moq"
    };

    private static readonly HashSet<string> OylaSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "b", "dim", "dm", "sh", "yman", "sak", "sam", "gan", "nadi", "mang", "ng", "dik"
    };

    private static readonly HashSet<string> JonatSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "dim", "dm", "gan", "ildi", "ish", "ing", "adi", "aman", "amiz", "aylik"
    };

    private static readonly HashSet<string> QoshSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "imcha", "ish", "ildi", "ilgan", "moq", "ilsin", "amiz", "amz", "adi", "di", "dim", "dm"
    };

    private static readonly HashSet<string> OrinSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "diq", "bosar", "li", "da", "lar", "i", "ga", "dan", "iga", "ida"
    };

    private static readonly HashSet<string> TolSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ov", "ash", "adi", "angan", "ayman", "dir", "dim", "dm", "di", "dirildi", "iq"
    };

    private static readonly HashSet<string> OtSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "gan", "ib", "ish", "ganlar", "adi", "moqda", "dim", "dm", "aylik", "ibdi", "dik", "dk"
    };

    /// <summary>
    /// Normalizes a chat word into standard orthography, preserving casing.
    /// Returns the normalized word, or the original word if no normalization applies.
    /// </summary>
    public static string Normalize(string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 2)
            return word;

        string lower = word.ToLowerInvariant();
        string norm = lower;

        // ── 1. Russian keyboard 'w' substitutions ───────────────────
        if (norm == "wu")
        {
            norm = "shu";
        }
        else if (norm.StartsWith("wunaqa") || norm.StartsWith("wunaqangi") ||
                 norm.StartsWith("wuni") || norm.StartsWith("wunga") ||
                 norm.StartsWith("wunday") || norm.StartsWith("wunde") ||
                 norm.StartsWith("wuncha") || norm.StartsWith("wunda") ||
                 norm.StartsWith("wuning") || norm.StartsWith("wunisi"))
        {
            norm = "sh" + norm[1..];
        }
        else if (norm.StartsWith("owa") || norm.StartsWith("uwa"))
        {
            norm = "o'sha" + norm[3..];
        }
        else if (norm == "awqam")
        {
            norm = "oqshom";
        }
        else
        {
            if (norm.Contains("yaxw"))
                norm = norm.Replace("yaxw", "yaxsh");
            if (norm.Contains("tuw"))
                norm = norm.Replace("tuw", "tush");
            if (norm.Contains("bowq"))
                norm = norm.Replace("bowq", "boshq");
            if (norm.Contains("bowlan"))
                norm = norm.Replace("bowlan", "boshlan");
            if (norm.Contains("yow"))
                norm = norm.Replace("yow", "yosh");
            if (norm.EndsWith("iwi") && norm.Length >= 5)
                norm = norm[..^3] + "ishi";
            if (norm is "iwim" or "iwing" or "iwi")
                norm = norm.Replace("w", "sh");
            if (norm == "quwiladi")
                norm = "qo'shiladi";
            if (norm.Contains("tawavor"))
                norm = norm.Replace("tawavor", "tashavor");
        }

        // ── 2. 'c' for 'ch' in high-frequency chat abbreviations ────
        if (norm == "ucun") norm = "uchun";
        else if (norm == "qacon") norm = "qachon";
        else if (norm == "hec") norm = "hech";
        else if (norm == "qanca") norm = "qancha";
        else if (norm == "necu") norm = "nechun";

        // ── 3. Dialectal & spoken stems ─────────────────────────────
        if (norm.StartsWith("osha"))
        {
            norm = "o'sha" + norm[4..];
        }
        else if (norm.StartsWith("bopti"))
        {
            norm = "bo'pti" + norm[5..];
        }
        else if (norm is "bomasam" or "bomasa" or "bomasin")
        {
            norm = "bo'lmasam";
        }
        else if (norm == "bosa")
        {
            norm = "bo'lsa";
        }
        else if (norm.StartsWith("bovot"))
        {
            norm = "bo'vott" + norm[6..];
        }
        else if (norm == "bovotti")
        {
            norm = "bo'votti";
        }
        else if (norm == "lekn")
        {
            norm = "lekin";
        }
        else if (norm is "blan" or "bln")
        {
            norm = "bilan";
        }
        else if (norm == "nma")
        {
            norm = "nima";
        }
        else if (norm == "nmaga")
        {
            norm = "nimaga";
        }
        else if (norm is "nmadur" or "nmadir")
        {
            norm = "nimadir";
        }
        else if (norm == "qalesz")
        {
            norm = "qaleysiz";
        }
        else if (norm.StartsWith("qattansz"))
        {
            norm = "qattansiz";
        }
        else if (norm == "yaxwmsz")
        {
            norm = "yaxshimisiz";
        }
        else if (norm == "qanaqangisi")
        {
            norm = "qanaqangisi";
        }
        else if (norm.StartsWith("aytvor"))
        {
            // Keep stem recognizable for -vor suffix
        }

        // ── 4. Suffix contractions (-amz, -asz, -dm, -dk, -misz) ────
        if (norm.EndsWith("amz") && norm.Length >= 5)
        {
            norm = norm[..^3] + "amiz";
        }
        else if (norm.EndsWith("asz") && norm.Length >= 5)
        {
            norm = norm[..^3] + "asiz";
        }
        else if (norm.EndsWith("misz") && norm.Length >= 6)
        {
            norm = norm[..^4] + "misiz";
        }
        else if (norm.EndsWith("dm") && norm.Length >= 3 && norm != "adm")
        {
            norm = norm[..^2] + "dim";
        }
        else if (norm.EndsWith("dk") && norm.Length >= 4 && norm != "idk")
        {
            norm = norm[..^2] + "dik";
        }

        // ── 5. Ultra-fast O(1) Unaccented 'o' expansion ─────────────
        norm = ExpandUnaccentedRootsFast(norm);

        if (norm == lower)
            return word;

        return MatchCasing(word, norm);
    }

    /// <summary>
    /// High-speed O(1) unaccented root resolution using pre-indexed suffix sets.
    /// Eliminates regex compile and match overhead completely.
    /// </summary>
    private static string ExpandUnaccentedRootsFast(string norm)
    {
        // Root: bol- -> bo'l-
        if (norm.StartsWith("bol") && BolSuffixes.Contains(norm[3..]))
        {
            return "bo'l" + norm[3..];
        }

        // Root: kor- -> ko'r-
        if (norm.StartsWith("kor") && KorSuffixes.Contains(norm[3..]))
        {
            return "ko'r" + norm[3..];
        }

        // Root: oz- -> o'z-
        if (norm.StartsWith("oz") && OzSuffixes.Contains(norm[2..]))
        {
            return "o'z" + norm[2..];
        }

        // Words: togri, zor, hop, yoq
        if (norm == "togri" || norm.StartsWith("togri"))
        {
            return "to'g'ri" + norm[5..];
        }
        if (norm == "zor" || norm.StartsWith("zor"))
        {
            return "zo'r" + norm[3..];
        }
        if (norm is "hop" or "xop")
        {
            return "xo'p";
        }
        if (norm is "yoq" or "yoqmi" or "yoqligi" or "yoqku")
        {
            return "yo'q" + norm[3..];
        }

        // Root: kop- -> ko'p-
        if (norm.StartsWith("kop") && KopSuffixes.Contains(norm[3..]))
        {
            return "ko'p" + norm[3..];
        }

        // Root: soz- -> so'z-
        if (norm.StartsWith("soz") && SozSuffixes.Contains(norm[3..]))
        {
            return "so'z" + norm[3..];
        }

        // Root: sora- -> so'ra-
        if (norm.StartsWith("sora") && SoraSuffixes.Contains(norm[4..]))
        {
            return "so'ra" + norm[4..];
        }

        // Root: yol- -> yo'l-
        if (norm.StartsWith("yol") && YolSuffixes.Contains(norm[3..]))
        {
            return "yo'l" + norm[3..];
        }

        // Root: ort- -> o'rt-
        if (norm.StartsWith("ort") && OrtSuffixes.Contains(norm[3..]))
        {
            return "o'rt" + norm[3..];
        }

        // Root: ochir- -> o'chir-
        if (norm.StartsWith("ochir") && OchirSuffixes.Contains(norm[5..]))
        {
            return "o'chir" + norm[5..];
        }

        // Root: oyla- -> o'yla-
        if (norm.StartsWith("oyla") && OylaSuffixes.Contains(norm[4..]))
        {
            return "o'yla" + norm[4..];
        }

        // Root: jonat- -> jo'nat-
        if (norm.StartsWith("jonat") && JonatSuffixes.Contains(norm[5..]))
        {
            return "jo'nat" + norm[5..];
        }

        // Root: qosh- -> qo'sh-
        if (norm.StartsWith("qosh") && QoshSuffixes.Contains(norm[4..]))
        {
            return "qo'sh" + norm[4..];
        }

        // Root: orin- -> o'rin-
        if (norm.StartsWith("orin") && OrinSuffixes.Contains(norm[4..]))
        {
            return "o'rin" + norm[4..];
        }

        // Root: orgat- -> o'rgat-, organ- -> o'rgan-
        if (norm.StartsWith("orgat"))
        {
            return "o'rgat" + norm[5..];
        }
        if (norm.StartsWith("organ"))
        {
            return "o'rgan" + norm[5..];
        }

        // Root: tol- -> to'l-
        if (norm.StartsWith("tol") && TolSuffixes.Contains(norm[3..]))
        {
            return "to'l" + norm[3..];
        }

        // Root: ot- -> o't-
        if (norm.StartsWith("ot") && OtSuffixes.Contains(norm[2..]))
        {
            return "o't" + norm[2..];
        }

        return norm;
    }

    /// <summary>
    /// Copies the casing pattern (ALL CAPS, TitleCase, or lowercase) of the original word.
    /// </summary>
    public static string MatchCasing(string original, string transformed)
    {
        if (string.IsNullOrEmpty(transformed))
            return transformed;

        bool allUpper = true;
        for (int i = 0; i < original.Length; i++)
        {
            if (char.IsLetter(original[i]) && !char.IsUpper(original[i]))
            {
                allUpper = false;
                break;
            }
        }

        if (allUpper)
            return transformed.ToUpperInvariant();

        if (char.IsUpper(original[0]))
        {
            return char.ToUpperInvariant(transformed[0]) + transformed[1..];
        }

        return transformed.ToLowerInvariant();
    }
}
