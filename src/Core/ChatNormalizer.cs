// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — ChatNormalizer.cs
// High-performance normalizer for informal Uzbek digital communication:
// 1. Russian keyboard 'w' substitution for 'sh' in verified Uzbek stems
// 2. Vowel-dropped chat contractions (-amz, -asz, -dm, -dk, -misz)
// 3. 'c' for 'ch' in high-frequency chat abbreviations (ucun, qacon, hec)
// 4. Unaccented 'o' expansion in high-frequency Uzbek roots (bol-, kor-, oz-, etc.)
// 5. Colloquial / dialectal stems & spoken verb forms (-votti, -vor)
// ──────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace YangiAlifbo.Core;

/// <summary>
/// Normalizes colloquial, informal, and abbreviated Uzbek chat words
/// into standard orthographic forms ready for reformed Latin transliteration.
/// </summary>
public static class ChatNormalizer
{
    private static readonly HashSet<string> ExactColloquialMap = new(StringComparer.OrdinalIgnoreCase)
    {
        "ashi", "ashini", "ashining", "ashiga", "ashida", "ashidan",
        "ashani", "ashanga", "ashanda", "ashatta", "ashande",
        "qanaqa", "qanaqadir", "qanaqadur", "qanaqasiga", "qanaqadr", "qanaqaligini",
        "anaqa", "manaqa", "unaqa", "unaqada", "unaqamas", "shunaqa", "bunaqa",
        "qatta", "qattan", "qattasiz", "qattansiz",
        "chotki", "chotkiy", "chotka",
        "daxshat", "daxshatku",
        "gapyo", "gapyoq",
        "kemadi", "kemadimi", "kemadida", "kemadiku",
        "ketti", "kettizmi", "kettik",
        "obkel", "obkelin", "opkel", "opket", "obor", "oborish", "oboradi",
        "prosta", "vobshe", "voobshe"
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
        else if (norm is "bomasam" or "bomasa")
        {
            norm = "bo'lmasam";
        }
        else if (norm == "bosa")
        {
            norm = "bo'lsa";
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
        else if (norm == "qattansz")
        {
            norm = "qattansiz";
        }
        else if (norm == "yaxwmsz")
        {
            norm = "yaxshimisiz";
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

        // ── 5. Unaccented 'o' in high-frequency Uzbek roots ─────────
        norm = ExpandUnaccentedRoots(norm);

        if (norm == lower)
            return word;

        return MatchCasing(word, norm);
    }

    private static string ExpandUnaccentedRoots(string norm)
    {
        // Root: bol- -> bo'l-
        if (norm.StartsWith("bol") && (norm.Length == 3 ||
            Regex.IsMatch(norm, @"^bol(adi|di|sa|ib|gan|ishi|averadi|sin|maydi|arkan|ardim|gancha|adimi|ardi|ar|masa|ish|yapti|lar|masin|madi|ganmiz|gandik)$")))
        {
            return "bo'l" + norm[3..];
        }

        // Root: kor- -> ko'r-
        if (norm.StartsWith("kor") && (norm.Length == 3 ||
            Regex.IsMatch(norm, @"^kor(amiz|amz|ishamiz|ishamz|dim|dm|dik|dk|ib|gan|gancha|ganmiz|gandik|ishdik|aylik|inadi|indi|ik|asan|asiz|asz|ish|a|ing|aylikchi)$")))
        {
            return "ko'r" + norm[3..];
        }

        // Root: oz- -> o'z-
        if (norm.StartsWith("oz") && (norm.Length == 2 ||
            Regex.IsMatch(norm, @"^oz(i|im|ing|imiz|iz|lari|bekcha|bek|gina|roq|ini|icha|iga|ida|idan)$")))
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
        if (norm.StartsWith("kop") && (norm.Length == 3 ||
            Regex.IsMatch(norm, @"^kop(roq|chilik|aydi|aytir|ina|dan|ga)$")))
        {
            return "ko'p" + norm[3..];
        }

        // Root: soz- -> so'z-
        if (norm.StartsWith("soz") && (norm.Length == 3 ||
            Regex.IsMatch(norm, @"^soz(lar|lashuv|lagan|lash|im|ing|i|imiz)$")))
        {
            return "so'z" + norm[3..];
        }

        // Root: sora- -> so'ra-
        if (norm.StartsWith("sora") && (norm.Length == 4 ||
            Regex.IsMatch(norm, @"^sora(moq|dim|sh|gan|yman|ng|ylik)$")))
        {
            return "so'ra" + norm[4..];
        }

        // Root: yol- -> yo'l-
        if (norm.StartsWith("yol") && (norm.Length == 3 ||
            Regex.IsMatch(norm, @"^yol(lar|i|ida|idan|lovchi|ga|da)$")))
        {
            return "yo'l" + norm[3..];
        }

        // Root: ort- -> o'rt-
        if (norm.StartsWith("ort") &&
            Regex.IsMatch(norm, @"^ort(a|oq|iq|iqcha|ada|asida|iqroq)$"))
        {
            return "o'rt" + norm[3..];
        }

        // Root: ochir- -> o'chir-
        if (norm.StartsWith("ochir") &&
            Regex.IsMatch(norm, @"^ochir(ildi|ish|dim|di|ib|ilsin)?$"))
        {
            return "o'chir" + norm[5..];
        }

        // Root: oyla- -> o'yla-
        if (norm.StartsWith("oyla") &&
            Regex.IsMatch(norm, @"^oyla(b|dim|dm|sh|yman|sak|sam|gan|nadi)?$"))
        {
            return "o'yla" + norm[4..];
        }

        // Root: jonat- -> jo'nat-
        if (norm.StartsWith("jonat") &&
            Regex.IsMatch(norm, @"^jonat(dim|dm|gan|ildi|ish|ing|adi)?$"))
        {
            return "jo'nat" + norm[5..];
        }

        // Root: qosh- -> qo'sh-
        if (norm.StartsWith("qosh") && (norm.Length == 4 ||
            Regex.IsMatch(norm, @"^qosh(imcha|ish|ildi|ilgan|moq|ilsin|amiz|amz|adi|di|dim)?$")))
        {
            return "qo'sh" + norm[4..];
        }

        // Root: orin- -> o'rin-
        if (norm.StartsWith("orin") &&
            Regex.IsMatch(norm, @"^orin(diq|bosar|li|da|lar|i|ga)?$"))
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
        if (norm.StartsWith("tol") &&
            Regex.IsMatch(norm, @"^tol(ov|ash|adi|angan|ayman|dir|dim|dm|di)$"))
        {
            return "to'l" + norm[3..];
        }

        // Root: ot- -> o't- (specific common verbal forms)
        if (norm.StartsWith("ot") &&
            Regex.IsMatch(norm, @"^ot(gan|ib|ish|ganlar|adi|moqda|dim|aylik|ibdi)$"))
        {
            return "o't" + norm[2..];
        }

        return norm;
    }

    private static bool IsVowel(char c) =>
        c is 'a' or 'e' or 'i' or 'o' or 'u' or 'y' or '\'';

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
