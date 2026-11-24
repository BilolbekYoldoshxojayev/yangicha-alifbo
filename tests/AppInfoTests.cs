// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — AppInfoTests.cs
// Feedback links, issue forms and the desktop shortcut.
// ──────────────────────────────────────────────────────────────────────

using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;
using YangiAlifbo.Win32;

namespace YangiAlifbo.Tests;

[TestClass]
public class AppInfoTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "YangiAlifbo.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }

    [DataTestMethod]
    [DataRow("bug", "bug.yml")]
    [DataRow("word", "wrong-conversion.yml")]
    [DataRow("idea", "idea.yml")]
    [DataRow("unknown", "bug.yml")]
    public void FeedbackUrl_OpensTheMatchingIssueForm(string kind, string template)
    {
        string url = AppInfo.FeedbackUrl(kind, "2.3.0", "Microsoft Windows 10.0.26100");
        StringAssert.StartsWith(url, AppInfo.RepoUrl + "/issues/new?template=" + template + "&");
        StringAssert.Contains(url, "&version=2.3.0");
        StringAssert.Contains(url, "&os=Microsoft%20Windows%2010.0.26100");
    }

    [TestMethod]
    public void FeedbackUrl_EscapesQueryCharacters()
    {
        string url = AppInfo.FeedbackUrl("bug", "1.0&x=1", "a#b");
        StringAssert.Contains(url, "version=1.0%26x%3D1");
        StringAssert.Contains(url, "os=a%23b");
    }

    [TestMethod]
    public void EveryFeedbackTemplate_ExistsWithVersionAndOsFields()
    {
        string dir = Path.Combine(RepoRoot(), ".github", "ISSUE_TEMPLATE");
        foreach (string file in AppInfo.FeedbackTemplates.Values)
        {
            string path = Path.Combine(dir, file);
            Assert.IsTrue(File.Exists(path), file + " is missing");
            string yml = File.ReadAllText(path);
            StringAssert.Contains(yml, "id: version", file);
            StringAssert.Contains(yml, "id: os", file);
        }
    }

    [TestMethod]
    public void DonationCard_Has16Digits()
    {
        Assert.AreEqual(16, AppInfo.DonationCard.Count(char.IsDigit));
    }

    [TestMethod]
    public void DesktopShortcut_CreateThenRemove()
    {
        string dir = Path.Combine(Path.GetTempPath(), "ya-lnk-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string lnk = DesktopShortcut.PathFor(dir);
            DesktopShortcut.Create(Environment.ProcessPath!, lnk);
            Assert.IsTrue(File.Exists(lnk));
            Assert.IsTrue(new FileInfo(lnk).Length > 0);
            DesktopShortcut.Remove(lnk);
            Assert.IsFalse(File.Exists(lnk));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
