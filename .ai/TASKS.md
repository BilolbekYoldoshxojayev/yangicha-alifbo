# TASKS — YangiAlifbo

## Milestone 1: Core Engine & Phonetic Rules [COMPLETE]
- [x] T1.1: Implement `UzbekPhonetics.cs` with `sh->ş`, `ch->ç`, `o'->ö`, `g'->ğ`.
- [x] T1.2: Implement case preservation (lowercase, TitleCase, ALL CAPS).
- [x] T1.3: Implement apostrophe/tutuq boundary protection (`Is'hoq`, `mus'haf`, `as-hob`, `mas'ul`).

## Milestone 2: Linguistic Discrimination Engine [COMPLETE]
- [x] T2.1: Compile raw Uzbek word corpus (~33K+ words) into `uzbek_words.txt`.
- [x] T2.2: Compile English & programming keyword reject corpus (~10K+ words) into `english_reject.txt`.
- [x] T2.3: Implement `LanguageDetector.cs` with 3-tier lookup + morphology stripping.
- [x] T2.4: Support user whitelist & blacklist files in `%AppData%\YangiAlifbo\`.

## Milestone 3: Win32 Hook & Input Synthesis [COMPLETE]
- [x] T3.1: Define Win32 P/Invoke signatures in `NativeMethods.cs`.
- [x] T3.2: Implement `InputSynthesizer.cs` with Backspace + Unicode SendInput and `0x59414E47` magic guard.
- [x] T3.3: Implement `KeyboardHook.cs` with `WH_KEYBOARD_LL`, rolling buffer, delimiter detection.
- [x] T3.4: Implement `FocusTracker.cs` for window focus change & terminal blacklist.

## Milestone 4: System Tray UI & Hotkey [COMPLETE]
- [x] T4.1: Implement `TrayApplicationContext.cs` with icon generation, menu, state management.
- [x] T4.2: Implement global hotkey `Ctrl + Shift + U`.
- [x] T4.3: Implement Windows Startup registry integration.
- [x] T4.4: Implement single-instance mutex in `Program.cs`.

## Milestone 5: Verification & Publishing [IN PROGRESS]
- [x] T5.1: Create test suite `TransliterationTests.cs`.
- [x] T5.2: Create test suite `DiscriminationTests.cs`.
- [x] T5.3: Create automated build & publish scripts (`publish.ps1`, `build.bat`).
- [ ] T5.4: Execute test suite and verify 100% pass rate.
- [ ] T5.5: Publish single-file self-contained executable.
