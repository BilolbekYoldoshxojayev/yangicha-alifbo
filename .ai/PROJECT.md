# PROJECT MEMORY — YangiAlifbo

## Executive Summary
YangiAlifbo (UzbekScriptEngine) is a lightweight, real-time, low-latency Windows background system tray application designed to intercept user keystrokes globally and transliterate 1995 Uzbek Latin orthography to the official 2024 Reformed Latin alphabet (`ö`, `ğ`, `ş`, `ç`) across all Windows applications (Telegram, Word, Chrome, Notepad, etc.) without interrupting typing speed or corrupting non-Uzbek words.

## Tech Stack
- **Platform**: Windows 10/11 x64
- **Language & Runtime**: C# / .NET 10 (WinForms, Win32 P/Invoke)
- **Key Win32 APIs**: `WH_KEYBOARD_LL`, `SendInput`, `SetWinEventHook`, `RegisterHotKey`
- **Linguistic Engine**: 3-Tier Discrimination (Reject filter, Uzbek Lexicon ~33K+, Affix morphology)
