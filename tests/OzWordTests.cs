using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;
using YangiAlifbo.Win32;

namespace YangiAlifbo.Tests;

[TestClass]
public class OzWordTests
{
    private static readonly LanguageDetector D = new();

    [DataTestMethod]
    [DataRow("o'z", "öz")]
    [DataRow("O'z", "Öz")]
    [DataRow("O'Z", "ÖZ")]
    [DataRow("o‘z", "öz")]
    [DataRow("oʻz", "öz")]
    [DataRow("o’z", "öz")]
    [DataRow("o`z", "öz")]
    [DataRow("o´z", "öz")]
    [DataRow("o‛z", "öz")]
    [DataRow("o′z", "öz")]
    [DataRow("o'zim", "özim")]
    [DataRow("o'z-o'zidan", "öz-özidan")]
    [DataRow("'o'z'", "'öz'")]
    [DataRow("o'z'", "öz'")]
    [DataRow("'o'zim", "'özim")]
    [DataRow("tog'", "toğ")]
    [DataRow("bog'", "boğ")]
    [DataRow("abro'", "abrö")]
    public void WordConverter_Oz(string input, string expected)
    {
        Assert.AreEqual(expected, WordConverter.Convert(D, input, TransliterationMode.Strict, false));
        Assert.AreEqual(expected, WordConverter.Convert(D, input, TransliterationMode.Strict, true));
    }

    [DataTestMethod]
    [DataRow("Men o'z uyimda.", "Men öz uyimda.")]
    [DataRow("'o'z' so'z", "'öz' söz")]
    [DataRow("\"o'z\", o'z!", "\"öz\", öz!")]
    [DataRow("o'z-o'zimga, O'z", "öz-özimga, Öz")]
    public void TextConverter_Oz(string input, string expected)
    {
        Assert.AreEqual(expected, TextConverter.Convert(input, D, TransliterationMode.Strict, true, out _));
    }

    [TestMethod]
    public void LoneApostrophes_Untouched()
    {
        Assert.IsNull(WordConverter.Convert(D, "''", TransliterationMode.Strict, false));
        Assert.IsNull(WordConverter.Convert(D, "'o'", TransliterationMode.Strict, false));
        Assert.IsNull(WordConverter.Convert(D, "'tog'", TransliterationMode.Strict, false)); // quoted tog, not tog'
    }
}
