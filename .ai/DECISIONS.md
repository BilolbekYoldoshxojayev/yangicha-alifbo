# ARCHITECTURAL DECISIONS — YangiAlifbo

## ADR-001: Technology Stack Selection (.NET 10 + WinForms + Win32 Interop)
- **Context**: Need ultra-low latency, native system tray, low-level keyboard hooks, and quick self-contained publishing on Windows.
- **Alternatives**: Rust (`windows-rs`), C++ Win32, C# WPF, C# WinForms.
- **Decision**: C# / .NET 10 with WinForms and P/Invoke.
- **Rationale**: Built-in `NotifyIcon`, mature P/Invoke for `WH_KEYBOARD_LL` and `SendInput`, rapid development, modern C# 12/13 performance with O(1) HashSets, and self-contained single-file publishing.

## ADR-002: Anti-Recursion Strategy (Magic dwExtraInfo)
- **Context**: Calling `SendInput` causes Windows to invoke `WH_KEYBOARD_LL` again.
- **Decision**: Pass `0x59414E47` ("YANG") in `KEYBDINPUT.dwExtraInfo`.
- **Rationale**: Deterministic, zero overhead, completely eliminates infinite recursion risk.

## ADR-003: 3-Tier Linguistic Discrimination Engine
- **Context**: Replacing `sh`/`ch` globally would break English (`show`, `catch`) and programming (`switch`, `match`).
- **Decision**: Fast reject filter -> Uzbek lexicon match -> Morphological suffix stripper.
- **Rationale**: Prevents false positive transliterations while allowing neologisms and inflected Uzbek forms.
