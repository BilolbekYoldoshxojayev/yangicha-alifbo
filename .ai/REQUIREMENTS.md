# REQUIREMENTS — YangiAlifbo

## Functional Requirements
- **FR1**: Real-time keystroke monitoring globally across Windows using `WH_KEYBOARD_LL`.
- **FR2**: Transliterate 1995 Latin to 2024 Reformed Latin upon word boundary delimiters (space, enter, tab, punctuation).
  - `o'`/`oʻ` -> `ö`, `g'`/`gʻ` -> `ğ`, `sh` -> `ş`, `ch` -> `ç`.
- **FR3**: Strict case preservation:
  - lower -> lower (`shahar` -> `şahar`)
  - Title -> Title (`Shahar` -> `Şahar`)
  - UPPER -> UPPER (`SHAHAR` -> `ŞAHAR`)
- **FR4**: Syllable-break and morpheme-boundary exceptions:
  - Words with tutuq/apostrophe separating `s` and `h` (`Is'hoq`, `mus'haf`, `as-hob`, `mas'ul`) must NOT be merged into `ş` or `ç`.
- **FR5**: 3-Tier Linguistic Discrimination:
  - Tier 1: Negative reject filter (English and programming keywords).
  - Tier 2: Uzbek lexicon (33K+ words).
  - Tier 3: Agglutinative morphology heuristic (suffix stripping).
  - User Whitelist/Blacklist in `%AppData%/YangiAlifbo/`.
- **FR6**: System Tray Integration:
  - Active/Disabled icon.
  - Context menu with Strict/Aggressive modes, Whitelist/Blacklist editing, Startup toggle, About, Exit.
  - Global hotkey `Ctrl + Shift + U` to toggle.

## Non-Functional Requirements
- **NFR1**: Low latency: Keystroke processing latency < 5ms.
- **NFR2**: Zero recursion: Synthetic keystrokes (`SendInput`) tagged with `dwExtraInfo = 0x59414E47` to avoid infinite re-entrancy.
- **NFR3**: Reliability: Single instance enforced by named Mutex (`Global\YangiAlifbo_SingleInstance_Mutex`).
- **NFR4**: Process blacklist: Do not capture or corrupt terminal/IDE inputs (`cmd.exe`, `powershell.exe`, `code.exe`, etc.).
