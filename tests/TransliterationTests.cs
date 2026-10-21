// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo.Tests — TransliterationTests.cs
// Unit tests for the Uzbek transliteration phonetic rules engine (R1).
// Covers 2024 Reformed Latin mappings, all 9 apostrophe variants,
// strict casing preservation, syllable break / morpheme protection,
// double apostrophe contraction, glottal stop preservation, and edge cases.
// ──────────────────────────────────────────────────────────────────────

using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;

namespace YangiAlifbo.Tests;

[TestClass]
public class TransliterationTests
{
    // ══════════════════════════════════════════════════════════════════
    // 1. Lowercase conversions
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Convert_Shahar_Lowercase()
    {
        Assert.AreEqual("şahar", UzbekPhonetics.Convert("shahar"));
    }

    [TestMethod]
    public void Convert_Choyxona_Lowercase()
    {
        Assert.AreEqual("çoyxona", UzbekPhonetics.Convert("choyxona"));
    }

    [TestMethod]
    public void Convert_Ortoq_Lowercase()
    {
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o'rtoq"));
    }

    [TestMethod]
    public void Convert_Galaba_Lowercase()
    {
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("g'alaba"));
    }

    [TestMethod]
    public void Convert_Boshliq_Lowercase()
    {
        Assert.AreEqual("boşliq", UzbekPhonetics.Convert("boshliq"));
    }

    [TestMethod]
    public void Convert_Ichki_Lowercase()
    {
        Assert.AreEqual("içki", UzbekPhonetics.Convert("ichki"));
    }

    [TestMethod]
    public void Convert_Tosh_Lowercase()
    {
        Assert.AreEqual("toş", UzbekPhonetics.Convert("tosh"));
    }

    [TestMethod]
    public void Convert_Qosh_Lowercase()
    {
        Assert.AreEqual("qoş", UzbekPhonetics.Convert("qosh"));
    }

    // ══════════════════════════════════════════════════════════════════
    // 2. TitleCase conversions
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Convert_Shahar_TitleCase()
    {
        Assert.AreEqual("Şahar", UzbekPhonetics.Convert("Shahar"));
    }

    [TestMethod]
    public void Convert_Choyxona_TitleCase()
    {
        Assert.AreEqual("Çoyxona", UzbekPhonetics.Convert("Choyxona"));
    }

    [TestMethod]
    public void Convert_Ortoq_TitleCase()
    {
        Assert.AreEqual("Örtoq", UzbekPhonetics.Convert("O'rtoq"));
    }

    [TestMethod]
    public void Convert_Galaba_TitleCase()
    {
        Assert.AreEqual("Ğalaba", UzbekPhonetics.Convert("G'alaba"));
    }

    [TestMethod]
    public void Convert_Boshliq_TitleCase()
    {
        Assert.AreEqual("Boşliq", UzbekPhonetics.Convert("Boshliq"));
    }

    [TestMethod]
    public void Convert_Ichki_TitleCase()
    {
        Assert.AreEqual("Içki", UzbekPhonetics.Convert("Ichki"));
    }

    [TestMethod]
    public void Convert_Qoshimcha_TitleCase()
    {
        Assert.AreEqual("Qöşimça", UzbekPhonetics.Convert("Qo'shimcha"));
    }

    // ══════════════════════════════════════════════════════════════════
    // 3. ALL CAPS conversions
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Convert_Shahar_AllCaps()
    {
        Assert.AreEqual("ŞAHAR", UzbekPhonetics.Convert("SHAHAR"));
    }

    [TestMethod]
    public void Convert_Choyxona_AllCaps()
    {
        Assert.AreEqual("ÇOYXONA", UzbekPhonetics.Convert("CHOYXONA"));
    }

    [TestMethod]
    public void Convert_Ortoq_AllCaps()
    {
        Assert.AreEqual("ÖRTOQ", UzbekPhonetics.Convert("O'RTOQ"));
    }

    [TestMethod]
    public void Convert_Galaba_AllCaps()
    {
        Assert.AreEqual("ĞALABA", UzbekPhonetics.Convert("G'ALABA"));
    }

    [TestMethod]
    public void Convert_Tosh_AllCaps()
    {
        Assert.AreEqual("TOŞ", UzbekPhonetics.Convert("TOSH"));
    }

    [TestMethod]
    public void Convert_Shoshilinch_AllCaps()
    {
        Assert.AreEqual("ŞOŞILINÇ", UzbekPhonetics.Convert("SHOSHILINCH"));
    }

    // ══════════════════════════════════════════════════════════════════
    // 4. Double-cap typo and mixed-case handling
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Convert_DoubleCap_SHahar()
    {
        // Accidental Shift hold: SHahar -> Şahar
        Assert.AreEqual("Şahar", UzbekPhonetics.Convert("SHahar"));
    }

    [TestMethod]
    public void Convert_DoubleCap_CHoyxona()
    {
        // Accidental Shift hold: CHoyxona -> Çoyxona
        Assert.AreEqual("Çoyxona", UzbekPhonetics.Convert("CHoyxona"));
    }

    [TestMethod]
    public void Convert_InvertedCase_sHahar()
    {
        // Inverted case: sHahar -> şahar
        Assert.AreEqual("şahar", UzbekPhonetics.Convert("sHahar"));
    }

    [TestMethod]
    public void Convert_InvertedCase_cHtrans()
    {
        // Inverted case: cHtrans -> çtrans
        Assert.AreEqual("çtrans", UzbekPhonetics.Convert("cHtrans"));
    }

    // ══════════════════════════════════════════════════════════════════
    // 5. All 9 apostrophe variants handled uniformly
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Convert_All9ApostropheVariants_Lowercase()
    {
        // 1. Standard single quote (U+0027)
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o'rtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("g'alaba"));

        // 2. Left single quotation mark (U+2018)
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o‘rtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("g‘alaba"));

        // 3. Right single quotation mark (U+2019)
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o’rtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("g’alaba"));

        // 4. Modifier letter turned comma (U+02BB)
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("oʻrtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("gʻalaba"));

        // 5. Modifier letter apostrophe (U+02BC)
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("oʼrtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("gʼalaba"));

        // 6. Grave accent (U+0060)
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o`rtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("g`alaba"));

        // 7. Acute accent (U+00B4)
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o´rtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("g´alaba"));

        // 8. Single high-reversed-9 quotation mark (U+201B)
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o‛rtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("g‛alaba"));

        // 9. Prime (U+2032)
        Assert.AreEqual("örtoq", UzbekPhonetics.Convert("o′rtoq"));
        Assert.AreEqual("ğalaba", UzbekPhonetics.Convert("g′alaba"));
    }

    [TestMethod]
    public void Convert_All9ApostropheVariants_TitleCase()
    {
        Assert.AreEqual("Örtoq", UzbekPhonetics.Convert("O'rtoq"));
        Assert.AreEqual("Örtoq", UzbekPhonetics.Convert("O‘rtoq"));
        Assert.AreEqual("Örtoq", UzbekPhonetics.Convert("O’rtoq"));
        Assert.AreEqual("Örtoq", UzbekPhonetics.Convert("Oʻrtoq"));
        Assert.AreEqual("Örtoq", UzbekPhonetics.Convert("Oʼrtoq"));
        Assert.AreEqual("Örtoq", UzbekPhonetics.Convert("O`rtoq"));
        Assert.AreEqual("Örtoq", UzbekPhonetics.Convert("O´rtoq"));
        Assert.AreEqual("Örtoq", UzbekPhonetics.Convert("O‛rtoq"));
        Assert.AreEqual("Örtoq", UzbekPhonetics.Convert("O′rtoq"));

        Assert.AreEqual("Ğalaba", UzbekPhonetics.Convert("G'alaba"));
        Assert.AreEqual("Ğalaba", UzbekPhonetics.Convert("G‘alaba"));
        Assert.AreEqual("Ğalaba", UzbekPhonetics.Convert("G’alaba"));
        Assert.AreEqual("Ğalaba", UzbekPhonetics.Convert("Gʻalaba"));
        Assert.AreEqual("Ğalaba", UzbekPhonetics.Convert("Gʼalaba"));
        Assert.AreEqual("Ğalaba", UzbekPhonetics.Convert("G`alaba"));
        Assert.AreEqual("Ğalaba", UzbekPhonetics.Convert("G´alaba"));
        Assert.AreEqual("Ğalaba", UzbekPhonetics.Convert("G‛alaba"));
        Assert.AreEqual("Ğalaba", UzbekPhonetics.Convert("G′alaba"));
    }

    [TestMethod]
    public void Convert_All9ApostropheVariants_AllCaps()
    {
        Assert.AreEqual("ÖRTOQ", UzbekPhonetics.Convert("O'RTOQ"));
        Assert.AreEqual("ÖRTOQ", UzbekPhonetics.Convert("O‘RTOQ"));
        Assert.AreEqual("ÖRTOQ", UzbekPhonetics.Convert("O’RTOQ"));
        Assert.AreEqual("ÖRTOQ", UzbekPhonetics.Convert("OʻRTOQ"));
        Assert.AreEqual("ÖRTOQ", UzbekPhonetics.Convert("OʼRTOQ"));
        Assert.AreEqual("ÖRTOQ", UzbekPhonetics.Convert("O`RTOQ"));
        Assert.AreEqual("ÖRTOQ", UzbekPhonetics.Convert("O´RTOQ"));
        Assert.AreEqual("ÖRTOQ", UzbekPhonetics.Convert("O‛RTOQ"));
        Assert.AreEqual("ÖRTOQ", UzbekPhonetics.Convert("O′RTOQ"));

        Assert.AreEqual("ĞALABA", UzbekPhonetics.Convert("G'ALABA"));
        Assert.AreEqual("ĞALABA", UzbekPhonetics.Convert("G‘ALABA"));
        Assert.AreEqual("ĞALABA", UzbekPhonetics.Convert("G’ALABA"));
        Assert.AreEqual("ĞALABA", UzbekPhonetics.Convert("GʻALABA"));
        Assert.AreEqual("ĞALABA", UzbekPhonetics.Convert("GʼALABA"));
        Assert.AreEqual("ĞALABA", UzbekPhonetics.Convert("G`ALABA"));
        Assert.AreEqual("ĞALABA", UzbekPhonetics.Convert("G´ALABA"));
        Assert.AreEqual("ĞALABA", UzbekPhonetics.Convert("G‛ALABA"));
        Assert.AreEqual("ĞALABA", UzbekPhonetics.Convert("G′ALABA"));
    }

    [TestMethod]
    public void Convert_MixedApostrophesInSentence()
    {
        string input = "g'arb g‘alaba gʻurur g`oza g´urur g‛urur g′urur";
        string expected = "ğarb ğalaba ğurur ğoza ğurur ğurur ğurur";
        Assert.AreEqual(expected, UzbekPhonetics.Convert(input));
    }

    // ══════════════════════════════════════════════════════════════════
    // 6. Syllable break & morpheme boundary protection (tutuq belgisi)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Convert_Ishoq_PreservesApostropheBoundary()
    {
        // Is'hoq must NEVER become Işoq or İşhoq
        Assert.AreEqual("Is'hoq", UzbekPhonetics.Convert("Is'hoq"));
        Assert.AreNotEqual("Işoq", UzbekPhonetics.Convert("Is'hoq"));
    }

    [TestMethod]
    public void Convert_Ishoq_AllCaps_PreservesApostropheBoundary()
    {
        Assert.AreEqual("IS'HOQ", UzbekPhonetics.Convert("IS'HOQ"));
    }

    [TestMethod]
    public void Convert_Mushaf_PreservesApostropheBoundary()
    {
        // mus'haf must NEVER become muşaf
        Assert.AreEqual("mus'haf", UzbekPhonetics.Convert("mus'haf"));
        Assert.AreNotEqual("muşaf", UzbekPhonetics.Convert("mus'haf"));
    }

    [TestMethod]
    public void Convert_Mushaf_AllCaps_PreservesApostropheBoundary()
    {
        Assert.AreEqual("MUS'HAF", UzbekPhonetics.Convert("MUS'HAF"));
    }

    [TestMethod]
    public void Convert_Ashob_PreservesApostropheBoundary()
    {
        // as'hob must NEVER become aşob
        Assert.AreEqual("as'hob", UzbekPhonetics.Convert("as'hob"));
        Assert.AreNotEqual("aşob", UzbekPhonetics.Convert("as'hob"));
    }

    [TestMethod]
    public void Convert_Ashob_AllCaps_PreservesApostropheBoundary()
    {
        Assert.AreEqual("AS'HOB", UzbekPhonetics.Convert("AS'HOB"));
    }

    [TestMethod]
    public void Convert_Ashob_PreservesHyphenBoundary()
    {
        Assert.AreEqual("as-hob", UzbekPhonetics.Convert("as-hob"));
    }

    [TestMethod]
    public void Convert_Masul_PreservesBoundary()
    {
        Assert.AreEqual("mas'ul", UzbekPhonetics.Convert("mas'ul"));
        Assert.AreEqual("MAS'UL", UzbekPhonetics.Convert("MAS'UL"));
        Assert.AreEqual("mas'uliyat", UzbekPhonetics.Convert("mas'uliyat"));
    }

    [TestMethod]
    public void Convert_ApostropheVariantInBoundaryWord_PreservedCleanly()
    {
        // All apostrophe variants in tutuq-break words normalize to standard apostrophe
        Assert.AreEqual("Is'hoq", UzbekPhonetics.Convert("Isʻhoq"));
        Assert.AreEqual("mus'haf", UzbekPhonetics.Convert("mus’haf"));
        Assert.AreEqual("as'hob", UzbekPhonetics.Convert("asʻhob"));
        Assert.AreEqual("mas'ul", UzbekPhonetics.Convert("masʻul"));
    }

    [TestMethod]
    public void Convert_BoundaryWordWithConvertibleSuffix()
    {
        // Is'hoqning o'g'li -> Is'hoqning öğli (both o' and g' convert, Is'h protected)
        Assert.AreEqual("Is'hoqning öğli", UzbekPhonetics.Convert("Is'hoqning o'g'li"));
    }

    // ══════════════════════════════════════════════════════════════════
    // 7. Double apostrophe (o' + tutuq) contraction: o'' -> ö'
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Convert_DoubleApostrophe_Mojiza()
    {
        // 1995: mo''jiza (Мўъжиза) -> 2024: mö'jiza
        Assert.AreEqual("mö'jiza", UzbekPhonetics.Convert("mo''jiza"));
        Assert.AreEqual("MÖ'JIZA", UzbekPhonetics.Convert("MO''JIZA"));
    }

    [TestMethod]
    public void Convert_DoubleApostrophe_Shola()
    {
        // 1995: sho''la (Шўъла) -> 2024: şö'la
        Assert.AreEqual("şö'la", UzbekPhonetics.Convert("sho''la"));
        Assert.AreEqual("ŞÖ'LA", UzbekPhonetics.Convert("SHO''LA"));
    }

    [TestMethod]
    public void Convert_DoubleApostrophe_MixedVariants()
    {
        // Mixed apostrophe variants: moʻ'jiza or moʻʼjiza -> mö'jiza
        Assert.AreEqual("mö'jiza", UzbekPhonetics.Convert("moʻ'jiza"));
        Assert.AreEqual("mö'jiza", UzbekPhonetics.Convert("moʻʼjiza"));
        Assert.AreEqual("şö'la", UzbekPhonetics.Convert("shoʻʼla"));
    }

    // ══════════════════════════════════════════════════════════════════
    // 8. Glottal stop words (tutuq belgisi preservation)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Convert_GlottalStopWords_Untouched()
    {
        Assert.AreEqual("san'at", UzbekPhonetics.Convert("san'at"));
        Assert.AreEqual("ta'lim", UzbekPhonetics.Convert("ta'lim"));
        Assert.AreEqual("ma'no", UzbekPhonetics.Convert("ma'no"));
        Assert.AreEqual("e'tibor", UzbekPhonetics.Convert("e'tibor"));
        Assert.AreEqual("sur'at", UzbekPhonetics.Convert("sur'at"));
        Assert.AreEqual("a'lo", UzbekPhonetics.Convert("a'lo"));
    }

    // ══════════════════════════════════════════════════════════════════
    // 9. Multiple conversions in single word / phrase
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Convert_MultiplePatternsInSingleWord()
    {
        // sh + ch in one word: shoshilinch -> şoşilinç (verifies defect fix)
        Assert.AreEqual("şoşilinç", UzbekPhonetics.Convert("shoshilinch"));

        // ch + o' + q: cho'qqi -> çöqqi
        Assert.AreEqual("çöqqi", UzbekPhonetics.Convert("cho'qqi"));

        // g' + o' + z: g'o'za -> ğöza
        Assert.AreEqual("ğöza", UzbekPhonetics.Convert("g'o'za"));

        // qo'shiq -> qöşiq in all casing forms
        Assert.AreEqual("qöşiq", UzbekPhonetics.Convert("qo'shiq"));
        Assert.AreEqual("Qöşiq", UzbekPhonetics.Convert("Qo'shiq"));
        Assert.AreEqual("QÖŞIQ", UzbekPhonetics.Convert("QO'SHIQ"));
    }

    [TestMethod]
    public void Convert_FullSentence()
    {
        string input = "o'rtoq shahar bo'ylab choyxona va g'alaba tomon bordi";
        string expected = "örtoq şahar böylab çoyxona va ğalaba tomon bordi";
        Assert.AreEqual(expected, UzbekPhonetics.Convert(input));
    }

    // ══════════════════════════════════════════════════════════════════
    // 10. NormalizeApostrophes method
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void NormalizeApostrophes_All9VariantsNormalizedToStandard()
    {
        string input = "' ‘ ’ ʻ ʼ ` ´ ‛ ′";
        string expected = "' ' ' ' ' ' ' ' '";
        Assert.AreEqual(expected, UzbekPhonetics.NormalizeApostrophes(input));
    }

    [TestMethod]
    public void NormalizeApostrophes_NullAndEmpty_Handled()
    {
        Assert.IsNull(UzbekPhonetics.NormalizeApostrophes(null!));
        Assert.AreEqual("", UzbekPhonetics.NormalizeApostrophes(""));
    }

    // ══════════════════════════════════════════════════════════════════
    // 11. HasConvertiblePatterns and HasConvertibleSequences
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void HasConvertiblePatterns_PositiveMatches()
    {
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("shahar"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("choyxona"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("o'rtoq"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("g'alaba"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("SHAHAR"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("O'RTOQ"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("mo''jiza"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("sho''la"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("oʻrtoq"));
        Assert.IsTrue(UzbekPhonetics.HasConvertiblePatterns("gʼalaba"));
    }

    [TestMethod]
    public void HasConvertiblePatterns_NegativeMatches()
    {
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("kitob"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("qalam"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("maktab"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("salom"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("dunyo"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("Is'hoq"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("mus'haf"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("as'hob"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("mas'ul"));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns(""));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns(null!));
        Assert.IsFalse(UzbekPhonetics.HasConvertiblePatterns("a"));
    }

    [TestMethod]
    public void HasConvertibleSequences_ProducesIdenticalResult()
    {
        // Verifies the interface contract from PROJECT.md
        Assert.IsTrue(UzbekPhonetics.HasConvertibleSequences("shahar"));
        Assert.IsTrue(UzbekPhonetics.HasConvertibleSequences("o'rtoq"));
        Assert.IsFalse(UzbekPhonetics.HasConvertibleSequences("kitob"));
        Assert.IsFalse(UzbekPhonetics.HasConvertibleSequences("Is'hoq"));
    }

    // ══════════════════════════════════════════════════════════════════
    // 12. Edge cases: null, empty, single-character, punctuation
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Convert_NullAndEmpty_Handled()
    {
        Assert.IsNull(UzbekPhonetics.Convert(null!));
        Assert.AreEqual("", UzbekPhonetics.Convert(""));
    }

    [TestMethod]
    public void Convert_NonConvertibleWords_Passthrough()
    {
        Assert.AreEqual("kitob", UzbekPhonetics.Convert("kitob"));
        Assert.AreEqual("qalam", UzbekPhonetics.Convert("qalam"));
        Assert.AreEqual("salom", UzbekPhonetics.Convert("salom"));
    }

    [TestMethod]
    public void Convert_PunctuationWrapping_Preserved()
    {
        Assert.AreEqual("(şahar)", UzbekPhonetics.Convert("(shahar)"));
        Assert.AreEqual("örtoq,", UzbekPhonetics.Convert("o'rtoq,"));
        Assert.AreEqual("çoyxona!", UzbekPhonetics.Convert("choyxona!"));
    }
}
