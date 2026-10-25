// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — Updater.cs
// Self-update from GitHub Releases:
//   1. Ask the releases API for the latest release.
//   2. Download its YangiAlifbo.exe and check the SHA-256 GitHub reports.
//   3. Start the download as an installer (--install) and let the caller exit.
//      The installer waits for the old process, copies itself over the old
//      exe and starts it with --updated-from <old version>.
// The new process deletes leftovers and shows the release notes it was given.
// ──────────────────────────────────────────────────────────────────────

using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace YangiAlifbo.Core;

/// <summary>A published release as the updater sees it.</summary>
public sealed record ReleaseInfo(Version Version, string Tag, string Name, string Notes, string HtmlUrl,
                                 string AssetUrl, long AssetSize, string? Sha256);

/// <summary>Release notes saved by the old process for the new one to show.</summary>
public sealed record WhatsNew(string Version, string From, string Name, string Notes, string HtmlUrl);

public sealed class Updater : IDisposable
{
    public const string GitHubReleasesApi = "https://api.github.com/repos/BilolbekYoldoshxojayev/yangicha-alifbo/releases/latest";

    /// <summary>GitHub's latest-release URL; YANGIALIFBO_RELEASES_API replaces it for update tests.</summary>
    public static string ReleasesApi =>
        Environment.GetEnvironmentVariable("YANGIALIFBO_RELEASES_API") is { Length: > 0 } url ? url : GitHubReleasesApi;
    public const string AssetName = "YangiAlifbo.exe";

    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YangiAlifbo", "updates");
    private static readonly string WhatsNewPath = Path.Combine(Dir, "whatsnew.json");

    private readonly HttpClient _http;

    public Updater()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd($"YangiAlifbo-Updater/{CurrentVersion}");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    /// <summary>Version of the running build (major.minor.build).</summary>
    public static Version CurrentVersion
    {
        get
        {
            var v = typeof(Updater).Assembly.GetName().Version ?? new Version(0, 0, 0);
            return new Version(v.Major, v.Minor, Math.Max(v.Build, 0));
        }
    }

    /// <summary>
    /// Only the published single-file exe replaces itself; a build run from bin/ or
    /// through `dotnet` never does.
    /// </summary>
    public static bool CanSelfUpdate =>
        string.IsNullOrEmpty(typeof(Updater).Assembly.Location) &&
        string.Equals(Path.GetFileName(Environment.ProcessPath), AssetName, StringComparison.OrdinalIgnoreCase);

    // ── Parsing helpers (public for tests) ─────────────────────────

    /// <summary>"v1.6.0" → 1.6.0; null when the tag is not a version.</summary>
    public static Version? ParseTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;
        string t = tag.Trim().TrimStart('v', 'V');
        int cut = t.IndexOfAny(new[] { '-', '+', ' ' });
        if (cut >= 0) t = t[..cut];
        if (!Version.TryParse(t, out var v)) return null;
        return new Version(v.Major, v.Minor, Math.Max(v.Build, 0));
    }

    /// <summary>"sha256:ABC…" → "abc…"; null for anything else.</summary>
    public static string? ParseDigest(string? digest)
    {
        if (digest == null || !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)) return null;
        string hex = digest[7..].Trim().ToLowerInvariant();
        return hex.Length == 64 && hex.All(Uri.IsHexDigit) ? hex : null;
    }

    /// <summary>Reads a releases-API JSON document; null when it has no exe asset or version tag.</summary>
    public static ReleaseInfo? ParseRelease(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        if (r.TryGetProperty("draft", out var d) && d.GetBoolean()) return null;
        if (r.TryGetProperty("prerelease", out var p) && p.GetBoolean()) return null;

        string tag = r.GetProperty("tag_name").GetString() ?? "";
        var version = ParseTag(tag);
        if (version == null) return null;

        foreach (var a in r.GetProperty("assets").EnumerateArray())
        {
            if (!string.Equals(a.GetProperty("name").GetString(), AssetName, StringComparison.OrdinalIgnoreCase)) continue;
            return new ReleaseInfo(
                version, tag,
                r.TryGetProperty("name", out var n) ? n.GetString() ?? tag : tag,
                r.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "",
                r.TryGetProperty("html_url", out var h) ? h.GetString() ?? "" : "",
                a.GetProperty("browser_download_url").GetString() ?? "",
                a.GetProperty("size").GetInt64(),
                ParseDigest(a.TryGetProperty("digest", out var dg) ? dg.GetString() : null));
        }
        return null;
    }

    // ── Network ─────────────────────────────────────────────────────

    /// <summary>The latest release, or null when it cannot be read.</summary>
    public async Task<ReleaseInfo?> GetLatestAsync(CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync(ReleasesApi, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) return null;
        return ParseRelease(await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
    }

    /// <summary>
    /// Downloads the release exe and returns its path. Throws when the size or the
    /// SHA-256 does not match what GitHub published, or when no checksum exists.
    /// </summary>
    public async Task<string> DownloadAsync(ReleaseInfo release, IProgress<int>? progress, CancellationToken ct = default)
    {
        if (release.Sha256 == null)
            throw new InvalidDataException("Relizda SHA-256 nazorat summasi yo‘q.");

        Directory.CreateDirectory(Dir);
        string target = Path.Combine(Dir, $"YangiAlifbo-{release.Version}.exe");
        string partial = target + ".part";

        using (var resp = await _http.GetAsync(release.AssetUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
        {
            resp.EnsureSuccessStatusCode();
            await using var src = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var dst = File.Create(partial);
            var buffer = new byte[81920];
            long done = 0;
            int read, last = -1;
            while ((read = await src.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                done += read;
                int pct = release.AssetSize > 0 ? (int)(done * 100 / release.AssetSize) : 0;
                if (pct != last) { progress?.Report(pct); last = pct; }
            }
        }

        var info = new FileInfo(partial);
        string hash;
        await using (var fs = File.OpenRead(partial))
            hash = Convert.ToHexString(await SHA256.HashDataAsync(fs, ct).ConfigureAwait(false)).ToLowerInvariant();

        if (info.Length != release.AssetSize || hash != release.Sha256)
        {
            File.Delete(partial);
            throw new InvalidDataException("Yuklangan fayl relizdagi SHA-256 bilan mos kelmadi.");
        }

        File.Move(partial, target, overwrite: true);
        return target;
    }

    // ── Install and hand-over ───────────────────────────────────────

    // The running exe is never renamed or touched: a single-file build loads parts of
    // .NET from its own file on demand, and after a rename those loads fail (this broke
    // the restart in v1.6–v1.7). Instead the downloaded exe is started as an installer;
    // it waits for this process to exit, copies itself over the old exe and starts it.

    public const string InstallArg = "--install";

    /// <summary>
    /// Saves the release notes and starts <paramref name="downloaded"/> as the installer.
    /// The caller exits right after a true result.
    /// </summary>
    public static bool TryInstall(string downloaded, ReleaseInfo release, out string error) =>
        TryInstall(downloaded, release, Environment.ProcessPath!, start: true, out error);

    /// <summary>Saves the notes; starts the installer for <paramref name="exe"/> when <paramref name="start"/> is set.</summary>
    public static bool TryInstall(string downloaded, ReleaseInfo release, string exe, bool start, out string error)
    {
        error = "";
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(WhatsNewPath, new JsonObject
            {
                ["Version"] = release.Version.ToString(), ["From"] = CurrentVersion.ToString(),
                ["Name"] = release.Name, ["Notes"] = release.Notes, ["HtmlUrl"] = release.HtmlUrl
            }.ToJsonString());

            if (start)
            {
                var psi = new ProcessStartInfo(downloaded) { UseShellExecute = false };
                psi.ArgumentList.Add(InstallArg);
                psi.ArgumentList.Add(exe);
                psi.ArgumentList.Add(Environment.ProcessId.ToString());
                psi.ArgumentList.Add(CurrentVersion.ToString());
                Process.Start(psi);
            }
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Write("TryInstall: " + ex);
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Installer mode (<c>--install &lt;exe&gt; &lt;pid&gt; &lt;from-version&gt;</c>), run by the new exe:
    /// waits for the old process, replaces its exe with this one and starts it.
    /// The old exe is started again if the replacement fails, so the app never ends up gone.
    /// </summary>
    public static int RunInstaller(string[] args)
    {
        if (args.Length < 4 || !int.TryParse(args[2], out int pid)) return 2;
        string target = args[1], from = args[3], self = Environment.ProcessPath!;
        AppLog.Write($"Installer: v{CurrentVersion} replacing {target} (old pid {pid})");

        try
        {
            using var old = Process.GetProcessById(pid);
            if (!old.WaitForExit(30_000)) { old.Kill(); old.WaitForExit(5_000); }
        }
        catch (ArgumentException) { }   // already gone

        bool replaced = false;
        for (int i = 0; i < 40 && !replaced; i++)   // the file can stay locked for a moment after exit
        {
            try { Replace(self, target); replaced = true; }
            catch (Exception ex) { if (i == 39) AppLog.Write("Installer: replace failed: " + ex.Message); else Thread.Sleep(250); }
        }

        try
        {
            var psi = new ProcessStartInfo(target) { UseShellExecute = false };
            if (replaced) { psi.ArgumentList.Add("--updated-from"); psi.ArgumentList.Add(from); }
            Process.Start(psi);
            AppLog.Write(replaced ? "Installer: done, new version started" : "Installer: old version started again");
        }
        catch (Exception ex)
        {
            AppLog.Write("Installer: could not start " + target + ": " + ex.Message);
            return 1;
        }
        return replaced ? 0 : 1;
    }

    /// <summary>Puts a copy of <paramref name="source"/> at <paramref name="target"/>, keeping the old file until the copy is complete.</summary>
    public static void Replace(string source, string target)
    {
        string old = target + ".old", fresh = target + ".new";
        File.Copy(source, fresh, overwrite: true);
        if (File.Exists(target)) File.Move(target, old, overwrite: true);
        try { File.Move(fresh, target); }
        catch { File.Move(old, target); throw; }
        try { File.Delete(old); } catch { }
    }

    /// <summary>
    /// Removes what an update leaves behind: the previous exe (*.old, from v1.6–v1.7 updates)
    /// and downloaded installers, retrying while the installer exits.
    /// </summary>
    public static void CleanupOldExe()
    {
        string? exe = Environment.ProcessPath;
        if (exe == null) return;
        _ = Task.Run(async () =>
        {
            for (int i = 0; i < 20; i++)
            {
                var left = new List<string>();
                if (File.Exists(exe + ".old")) left.Add(exe + ".old");
                if (Directory.Exists(Dir))
                    left.AddRange(Directory.GetFiles(Dir, "YangiAlifbo-*.exe").Where(f =>
                        !string.Equals(f, exe, StringComparison.OrdinalIgnoreCase)));
                if (left.Count == 0) return;
                foreach (var f in left) { try { File.Delete(f); } catch { } }
                await Task.Delay(500).ConfigureAwait(false);
            }
        });
    }

    /// <summary>Returns and clears the notes saved by the update that started this process.</summary>
    public static WhatsNew? TakeWhatsNew()
    {
        try
        {
            if (!File.Exists(WhatsNewPath)) return null;
            var o = JsonNode.Parse(File.ReadAllText(WhatsNewPath))!.AsObject();
            string Str(string k) => o[k]?.GetValue<string>() ?? "";
            var w = new WhatsNew(Str("Version"), Str("From"), Str("Name"), Str("Notes"), Str("HtmlUrl"));
            File.Delete(WhatsNewPath);
            return w;
        }
        catch { return null; }
    }

    public void Dispose() => _http.Dispose();
}
