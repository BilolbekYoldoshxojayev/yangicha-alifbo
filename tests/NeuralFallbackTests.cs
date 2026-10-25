using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;

namespace YangiAlifbo.Tests;

[TestClass]
public class NeuralFallbackTests
{
    private static readonly NeuralFallback Model = NeuralFallback.LoadEmbedded()
        ?? throw new InvalidOperationException("uzbek_neural.bin.br is not embedded");

    private static readonly Lazy<LanguageDetector> Full = new(() => new LanguageDetector());

    [TestMethod]
    public void EmbeddedModelLoadsWithStrictThreshold()
    {
        Assert.IsTrue(Model.Threshold is > 0.5 and < 1.0, $"threshold {Model.Threshold}");
    }

    [TestMethod]
    public void ScoresMatchPyTorch()
    {
        // tools/neural/train.py writes these scores with the trained PyTorch model.
        string path = Path.Combine(AppContext.BaseDirectory, "data", "neural_vectors.tsv");
        int n = 0;
        double worst = 0;
        foreach (string line in File.ReadLines(path))
        {
            var p = line.Split('\t');
            double expected = double.Parse(p[1], CultureInfo.InvariantCulture);
            worst = Math.Max(worst, Math.Abs(Model.Score(p[0]) - expected));
            n++;
        }
        Assert.IsTrue(n >= 1000, $"only {n} vectors");
        Assert.IsTrue(worst < 1e-4, $"largest difference {worst}");
    }

    [DataTestMethod]
    [DataRow("shahar", true)]
    [DataRow("o'zbekiston", true)]
    [DataRow("o'z-o'zidan", true)]
    [DataRow("osh", false)]               // shorter than 4
    [DataRow("swish", false)]             // w is not an Uzbek letter
    [DataRow("chocolate", false)]         // c outside ch
    [DataRow("push_button", false)]
    [DataRow("chat2", false)]
    [DataRow("'shahar", false)]
    [DataRow("kitob", false)]             // nothing to convert
    [DataRow("is'hoq", false)]            // s'h is not sh
    public void EligibilityRules(string word, bool eligible)
    {
        Assert.AreEqual(eligible, NeuralFallback.IsEligible(word), word);
    }

    [DataTestMethod]
    [DataRow("fresh")] [DataRow("shopping")] [DataRow("english")] [DataRow("bishop")]
    [DataRow("sheriff")] [DataRow("flash")] [DataRow("mushroom")] [DataRow("finish")]
    [DataRow("teacher")] [DataRow("search")] [DataRow("children")] [DataRow("approach")]
    [DataRow("photo's")] [DataRow("bishop's")] [DataRow("o'brien")] [DataRow("shapefile")]
    [DataRow("hashmap")] [DataRow("dashboard")] [DataRow("refresh")] [DataRow("kuzushiji")]
    public void ForeignWordsStayUnchangedThroughFullDetector(string word)
    {
        Assert.IsFalse(Full.Value.IsUzbekOrLikely(word), word);
    }

    [TestMethod]
    public void NeverRejectsWhatTheDictionaryAccepts()
    {
        var d = Full.Value;
        foreach (string w in new[] { "shahar", "choyxona", "o'rtoq", "g'alaba", "maktabimizdagilarning" })
        {
            Assert.IsTrue(d.IsUzbek(w), w);
            Assert.IsTrue(d.IsUzbekOrLikely(w), w);
        }
    }

    [TestMethod]
    public void DisabledOrMissingModelFallsBackToDictionary()
    {
        var d = new LanguageDetector(new HashSet<string>(), new HashSet<string> { "shahar" });
        d.Neural = Model;
        d.NeuralEnabled = false;
        Assert.IsTrue(d.IsUzbekOrLikely("shahar"));
        Assert.IsFalse(d.IsUzbekOrLikely("sho'rvaxonalarimizdagilar"));
        d.Neural = null;
        d.NeuralEnabled = true;
        Assert.IsFalse(d.IsUzbekOrLikely("sho'rvaxonalarimizdagilar"));
    }

    [TestMethod]
    public void RejectListAndBlacklistWinOverModel()
    {
        const string word = "sho'rvaxonalarimizdagilar";
        var free = new LanguageDetector(new HashSet<string>(), new HashSet<string>()) { Neural = Model };
        Assert.IsTrue(free.IsUzbekOrLikely(word), "model should know this unknown Uzbek word");

        var rejected = new LanguageDetector(new HashSet<string> { word }, new HashSet<string>()) { Neural = Model };
        Assert.IsFalse(rejected.IsUzbekOrLikely(word));

        var blacklisted = new LanguageDetector(new HashSet<string>(), new HashSet<string>(),
            userBlacklist: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { word }) { Neural = Model };
        Assert.IsFalse(blacklisted.IsUzbekOrLikely(word));
    }

    [TestMethod]
    public void SlangRewritesNeverReachTheModel()
    {
        // With an empty lexicon only the model could accept a slang rewrite
        // ("korishamz" → "ko'rishamiz"), and TextConverter must not ask it.
        var d = new LanguageDetector(new HashSet<string>(), new HashSet<string>()) { Neural = Model };
        int checkedRewrites = 0;
        foreach (string typed in new[] { "korishamz", "yaxwi", "bowladik", "gapiramz", "chiqamz", "boshlaymz", "qilyapmz", "tushunmadm" })
        {
            string rewritten = ChatNormalizer.Normalize(typed);
            if (rewritten == typed || !Model.Accepts(LanguageDetector.NormalizeForLookup(rewritten))) continue;
            checkedRewrites++;
            Assert.AreEqual(typed, TextConverter.Convert(typed, d, Win32.TransliterationMode.Strict, slang: true, out _), typed);
        }
        Assert.IsTrue(checkedRewrites > 0, "no slang rewrite the model accepts; pick other examples");
    }
}
