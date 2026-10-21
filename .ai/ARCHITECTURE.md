# ARCHITECTURE — YangiAlifbo

## Subsystem Architecture

```
User Keystroke -> WH_KEYBOARD_LL (KeyboardHook.cs)
     |
     +-- [Check dwExtraInfo == MAGIC_EXTRA_INFO] -> Pass through (recursion prevention)
     +-- [Check Process Blacklist (FocusTracker.cs)] -> Pass through
     +-- [Char / Delimiter Check]
           |-- Delimiter (Space, Enter, etc.) -> Evaluate Word Buffer
           |        |
           |        +--> LanguageDetector.cs (3-Tier Check)
           |        |      Tier 1: English Reject (Fast HashSet) -> Skip if found
           |        |      Tier 2: Uzbek Lexicon (HashSet) -> Convert if found
           |        |      Tier 3: Uzbek Affix Morphology -> Convert if valid stem+suffix
           |        |
           |        +--> UzbekPhonetics.cs
           |               Case Preservation, Morpheme Boundary Exception Protection
           |
           |-- InputSynthesizer.cs
                 N Backspaces + Unicode SendInput with dwExtraInfo = 0x59414E47 ("YANG")
```

## Security & Anti-Recursion
- All synthetic keyboard inputs set `dwExtraInfo = 0x59414E47`.
- The hook ignores any events tagged with this identifier.
- Sensitive processes (`cmd.exe`, `powershell.exe`, `code.exe`, etc.) and password fields bypass the hook.
