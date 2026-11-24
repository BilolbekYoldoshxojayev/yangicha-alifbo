// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — AppInfo.cs
// Author, donation card and feedback links shown in the control center.
// ──────────────────────────────────────────────────────────────────────

namespace YangiAlifbo.Core;

public static class AppInfo
{
    public const string Author = "Bilolbek Yoldoshxojayev";
    public const string DonationCard = "5614 6838 6100 6096";
    public const string RepoUrl = "https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo";

    /// <summary>Feedback kinds and the GitHub issue form each one opens (.github/ISSUE_TEMPLATE).</summary>
    public static readonly IReadOnlyDictionary<string, string> FeedbackTemplates = new Dictionary<string, string>
    {
        ["bug"] = "bug.yml",
        ["word"] = "wrong-conversion.yml",
        ["idea"] = "idea.yml",
    };

    /// <summary>
    /// New-issue URL for a feedback kind, with only the app version and OS prefilled.
    /// Nothing the user typed goes into the URL.
    /// </summary>
    public static string FeedbackUrl(string kind, string version, string os)
    {
        string template = FeedbackTemplates.TryGetValue(kind, out var t) ? t : FeedbackTemplates["bug"];
        return $"{RepoUrl}/issues/new?template={template}" +
               $"&version={Uri.EscapeDataString(version)}&os={Uri.EscapeDataString(os)}";
    }
}
