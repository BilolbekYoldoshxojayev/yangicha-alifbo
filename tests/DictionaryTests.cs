using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;

namespace YangiAlifbo.Tests;

/// <summary>The embedded dictionaries as shipped (v1.9: Wikipedia words, foreign list, pruned forms).</summary>
[TestClass]
public class DictionaryTests
{
    private static readonly Lazy<LanguageDetector> Detector = new(() => new LanguageDetector());

    [DataTestMethod]
    [DataRow("qozog'iston")] [DataRow("afg'onistonning")] [DataRow("shveysariya")] [DataRow("mushukchalarda")]
    [DataRow("chiqaruvchi")] [DataRow("qirg'iziston")] [DataRow("shotlandiya")]
    public void WikipediaWordsAreUzbek(string word) => Assert.IsTrue(Detector.Value.IsUzbek(word), word);

    [DataTestMethod]
    [DataRow("hardships")] [DataRow("chartered")] [DataRow("fishing")] [DataRow("chocolates")]
    [DataRow("marchand")] [DataRow("muchacho")]
    public void ForeignWordsAreRejected(string word)
    {
        Assert.IsTrue(Detector.Value.IsForeignOrReject(word), word);
        Assert.IsFalse(Detector.Value.IsUzbek(word), word);
    }

    [DataTestMethod]
    [DataRow("shahar")] [DataRow("choyxona")] [DataRow("o'rtoq")] [DataRow("g'alaba")] [DataRow("tashkent")]
    [DataRow("o'rtog'imiz")] [DataRow("boshlamoqchiman")] [DataRow("ishlatilmaydi")]
    public void UzbekWordsAreNotRejected(string word)
    {
        Assert.IsFalse(Detector.Value.IsForeignOrReject(word), word);
        Assert.IsTrue(Detector.Value.IsUzbek(word), word);
    }

    [TestMethod]
    public void CountsCoverAllSources()
    {
        Assert.IsTrue(Detector.Value.UzbekLexiconCount >= 955_000, $"forms {Detector.Value.UzbekLexiconCount}");
        Assert.IsTrue(Detector.Value.EnglishRejectCount >= 100_000, $"reject {Detector.Value.EnglishRejectCount}");
    }
}
