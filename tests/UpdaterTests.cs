// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — UpdaterTests.cs
// Release parsing, checksum handling and the exe swap used by auto-update.
// ──────────────────────────────────────────────────────────────────────

using Microsoft.VisualStudio.TestTools.UnitTesting;
using YangiAlifbo.Core;

namespace YangiAlifbo.Tests;

[TestClass]
public class UpdaterTests
{
    private const string Hash = "e965925f612673330a09ff261a46da7af0d875e1f5c8f355a23b0f0ad0b01efb";

    private static string ReleaseJson(string tag, string assetName = "YangiAlifbo.exe", string? digest = "sha256:" + Hash,
                                      bool prerelease = false) =>
        $$"""
        {
          "tag_name": "{{tag}}", "name": "{{tag}} — test", "body": "## Yangiliklar\n- 5-chi → 5-çi",
          "html_url": "https://github.com/x/y/releases/tag/{{tag}}", "draft": false, "prerelease": {{(prerelease ? "true" : "false")}},
          "assets": [
            { "name": "notes.txt", "size": 10, "browser_download_url": "https://example/notes.txt" },
            { "name": "{{assetName}}", "size": 54887693, "browser_download_url": "https://example/{{assetName}}"
              {{(digest == null ? "" : $", \"digest\": \"{digest}\"")}} }
          ]
        }
        """;

    [DataTestMethod]
    [DataRow("v1.6.0", "1.6.0")]
    [DataRow("1.6.0", "1.6.0")]
    [DataRow("V2.0", "2.0.0")]
    [DataRow("v1.6.0-beta", "1.6.0")]
    [DataRow("v10.11.12", "10.11.12")]
    public void ParseTag_ReadsVersions(string tag, string expected)
    {
        Assert.AreEqual(Version.Parse(expected), Updater.ParseTag(tag));
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("latest")]
    [DataRow("release-one")]
    public void ParseTag_RejectsNonVersions(string? tag)
    {
        Assert.IsNull(Updater.ParseTag(tag));
    }

    [TestMethod]
    public void ParseTag_OrdersNumerically()
    {
        Assert.IsTrue(Updater.ParseTag("v1.10.0") > Updater.ParseTag("v1.9.0"));
        Assert.IsTrue(Updater.ParseTag("v1.6.0") > Updater.ParseTag("v1.5.0"));
    }

    [TestMethod]
    public void ParseDigest_AcceptsOnlySha256Hex()
    {
        Assert.AreEqual(Hash, Updater.ParseDigest("sha256:" + Hash.ToUpperInvariant()));
        Assert.IsNull(Updater.ParseDigest("sha512:" + Hash));
        Assert.IsNull(Updater.ParseDigest("sha256:1234"));
        Assert.IsNull(Updater.ParseDigest("sha256:" + new string('z', 64)));
        Assert.IsNull(Updater.ParseDigest(null));
    }

    [TestMethod]
    public void ParseRelease_PicksTheExeAsset()
    {
        var r = Updater.ParseRelease(ReleaseJson("v1.6.0"));
        Assert.IsNotNull(r);
        Assert.AreEqual(new Version(1, 6, 0), r.Version);
        Assert.AreEqual("https://example/YangiAlifbo.exe", r.AssetUrl);
        Assert.AreEqual(54887693, r.AssetSize);
        Assert.AreEqual(Hash, r.Sha256);
        StringAssert.Contains(r.Notes, "5-çi");
    }

    [TestMethod]
    public void ParseRelease_SkipsPrereleasesAndMissingAssets()
    {
        Assert.IsNull(Updater.ParseRelease(ReleaseJson("v1.6.0", prerelease: true)));
        Assert.IsNull(Updater.ParseRelease(ReleaseJson("v1.6.0", assetName: "Other.exe")));
        Assert.IsNull(Updater.ParseRelease(ReleaseJson("nightly")));
    }

    [TestMethod]
    public void ParseRelease_KeepsMissingDigestAsNull()
    {
        var r = Updater.ParseRelease(ReleaseJson("v1.6.0", digest: null));
        Assert.IsNotNull(r);
        Assert.IsNull(r.Sha256);
    }

    [TestMethod]
    public async Task Download_RefusesReleaseWithoutChecksum()
    {
        var r = Updater.ParseRelease(ReleaseJson("v9.9.9", digest: null))!;
        using var u = new Updater();
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => u.DownloadAsync(r, null));
    }

    [TestMethod]
    public void TryInstall_SwapsExeKeepsOldAndSavesNotes()
    {
        string dir = Path.Combine(Path.GetTempPath(), "ya-update-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string exe = Path.Combine(dir, "YangiAlifbo.exe");
            string downloaded = Path.Combine(dir, "YangiAlifbo-1.6.0.exe");
            File.WriteAllText(exe, "old build");
            File.WriteAllText(downloaded, "new build");
            Updater.TakeWhatsNew(); // clear leftovers

            var r = Updater.ParseRelease(ReleaseJson("v1.6.0"))!;
            bool ok = Updater.TryInstall(downloaded, r, exe, start: false, out string error);

            Assert.IsTrue(ok, error);
            Assert.AreEqual("new build", File.ReadAllText(exe));
            Assert.AreEqual("old build", File.ReadAllText(exe + ".old"));
            Assert.IsFalse(File.Exists(downloaded));

            var notes = Updater.TakeWhatsNew();
            Assert.IsNotNull(notes);
            Assert.AreEqual("1.6.0", notes.Version);
            StringAssert.Contains(notes.Notes, "5-çi");
            Assert.IsNull(Updater.TakeWhatsNew(), "notes are shown once");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [TestMethod]
    public void TryInstall_RestoresOriginalWhenCopyFails()
    {
        string dir = Path.Combine(Path.GetTempPath(), "ya-update-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string exe = Path.Combine(dir, "YangiAlifbo.exe");
            File.WriteAllText(exe, "old build");
            var r = Updater.ParseRelease(ReleaseJson("v1.6.0"))!;

            bool ok = Updater.TryInstall(Path.Combine(dir, "missing.exe"), r, exe, start: false, out string error);

            Assert.IsFalse(ok);
            Assert.IsFalse(string.IsNullOrEmpty(error));
            Assert.AreEqual("old build", File.ReadAllText(exe));
            Assert.IsFalse(File.Exists(exe + ".old"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
