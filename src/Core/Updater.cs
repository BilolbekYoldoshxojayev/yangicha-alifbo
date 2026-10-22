// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — Updater.cs
// Self-update from GitHub Releases:
//   1. Ask the releases API for the latest release.
//   2. Download its YangiAlifbo.exe and check the SHA-256 GitHub reports.
//   3. Rename the running exe to *.old, put the new one in its place,
//      start it with --updated-from <old version>, and let the caller exit.
// The new process deletes *.old and shows the release notes it was given.
// ──────────────────────────────────────────────────────────────────────

using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace YangiAlifbo.Core;

/// <summary>A published release as the updater sees it.</summary>
public sealed record ReleaseInfo(Version Version, string Tag, string Name, string Notes, string HtmlUrl,
                                 string AssetUrl, long AssetSize, string? Sha256);

/// <summary>Release notes saved by the old process for the new one to show.</summary>
public sealed record WhatsNew(string Version, string From, string Name, string Notes, string HtmlUrl);

public sealed class Updater : IDisposable
{
    public const string ReleasesApi = "https://api.github.com/repos/BilolbekYoldoshxojayev/yangicha-alifbo/releases/latest";
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

    /// <summary>
    /// Puts <paramref name="downloaded"/> in place of the running exe and starts it.
    /// The caller exits right after a true result.
    /// </summary>
    public static bool TryInstall(string downloaded, ReleaseInfo release, out string error) =>
        TryInstall(downloaded, release, Environment.ProcessPath!, start: true, out error);

    /// <summary>Swaps <paramref name="exe"/> for the download; starts it when <paramref name="start"/> is set.</summary>
    public static bool TryInstall(string downloaded, ReleaseInfo release, string exe, bool start, out string error)
    {
        error = "";
        string old = exe + ".old";
        try
        {
            if (File.Exists(old)) File.Delete(old);
            File.Move(exe, old);                 // a running exe can be renamed, not overwritten
            try { File.Copy(downloaded, exe); }
            catch { File.Move(old, exe); throw; }

            Directory.CreateDirectory(Dir);
            File.WriteAllText(WhatsNewPath, JsonSerializer.Serialize(new WhatsNew(
                release.Version.ToString(), CurrentVersion.ToString(), release.Name, release.Notes, release.HtmlUrl)));

            if (start)
                Process.Start(new ProcessStartInfo(exe, $"--updated-from {CurrentVersion}") { UseShellExecute = false });
            try { File.Delete(downloaded); } catch { }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Removes the previous exe left by an update (retrying while it exits).</summary>
    public static void CleanupOldExe()
    {
        string? exe = Environment.ProcessPath;
        if (exe == null) return;
        string old = exe + ".old";
        _ = Task.Run(async () =>
        {
            for (int i = 0; i < 20 && File.Exists(old); i++)
            {
                try { File.Delete(old); } catch { await Task.Delay(500).ConfigureAwait(false); }
            }
        });
    }

    /// <summary>Returns and clears the notes saved by the update that started this process.</summary>
    public static WhatsNew? TakeWhatsNew()
    {
        try
        {
            if (!File.Exists(WhatsNewPath)) return null;
            var w = JsonSerializer.Deserialize<WhatsNew>(File.ReadAllText(WhatsNewPath));
            File.Delete(WhatsNewPath);
            return w;
        }
        catch { return null; }
    }

    public void Dispose() => _http.Dispose();
}
