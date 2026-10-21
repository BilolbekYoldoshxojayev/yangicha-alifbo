// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — IInputSynthesizer.cs
// Interface contract for keyboard input synthesis.
// ──────────────────────────────────────────────────────────────────────

namespace YangiAlifbo.Win32;

/// <summary>
/// Interface contract for synthesizing keystroke replacement.
/// </summary>
internal interface IInputSynthesizer
{
    /// <summary>Magic marker to tag synthetic events and prevent recursion.</summary>
    const uint MAGIC_EXTRA_INFO = 0x59414E47; // "YANG"

    /// <summary>
    /// Replaces the word at current caret location and types the delimiter.
    /// </summary>
    void ReplaceWord(string originalWord, string convertedWord, char delimiter);
}
