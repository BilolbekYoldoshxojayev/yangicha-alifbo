// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — TransliterationMode.cs
// Shared by the Windows app and the Linux library. It keeps its original
// namespace so existing code needs no changes.
// ──────────────────────────────────────────────────────────────────────

namespace YangiAlifbo.Win32;

/// <summary>
/// Transliteration mode determines how strictly words are verified.
/// </summary>
public enum TransliterationMode
{
    /// <summary>
    /// Only convert words confirmed to be Uzbek by the dictionary
    /// and morphological analyzer. Default and safest mode.
    /// </summary>
    Strict,

    /// <summary>
    /// Convert all words containing sh/ch/o'/g' patterns without
    /// dictionary verification. Use at your own risk.
    /// </summary>
    Aggressive
}
