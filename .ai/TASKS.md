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

## Milestone 5: Verification & Publishing (Windows 1.6.0) [COMPLETE]
- [x] T5.1: Create test suite `TransliterationTests.cs`.
- [x] T5.2: Create test suite `DiscriminationTests.cs`.
- [x] T5.3: Create automated build & publish scripts (`publish.ps1`, `build.bat`).
- [x] T5.4: Execute test suite and verify 100% pass rate.
- [x] T5.5: Publish single-file self-contained executable.

## Milestone 6: Multiplatform Ground Truth & Rust Core [COMPLETE]
- [x] T6.1: Generate 74,100 comprehensive test vectors (`tools/vectors/`).
- [x] T6.2: Build compact FST dictionary format (`data/yangi_data.bin`, 239 KB).
- [x] T6.3: Implement `yangi-core` crate with 100% vector parity on 74,100 vectors.
- [x] T6.4: Implement `yangi-ffi` C ABI library and headers.
- [x] T6.5: Implement `yangi-wasm` WebAssembly package.

## Milestone 7: Unknown-Word Model & Browser Extension [COMPLETE]
- [x] T7.1: Train and quantize int8 char n-gram classifier (`data/unknown_model.bin`, 16 KB).
- [x] T7.2: Build Manifest V3 browser extension with WebAssembly core in `platforms/browser/`.
- [x] T7.3: Implement input/textarea/contenteditable word transliteration with caret and undo preservation.
- [x] T7.4: Implement extension popup UI with brand design tokens and live sandbox.
- [x] T7.5: Build automated extension test runner (52/52 tests passing).

## Milestone 8: Android Keyboard & Multi-OS CI [COMPLETE]
- [x] T8.1: Implement JNI exports in `core/ffi` for `YangiNativeBridge`.
- [x] T8.2: Create purpose-built minimal `InputMethodService` in `platforms/android/`.
- [x] T8.3: Implement Latin QWERTY layout with long-press diacritics (`ş`, `ç`, `ö`, `ğ`).
- [x] T8.4: Implement single-tap backspace undo and strict password field bypass.
- [x] T8.5: Configure multi-OS GitHub Actions CI matrix with automated unit tests and strict privacy linter.

## Milestone 9: macOS Input Method (Workstream F) [PLANNING]
- [ ] T9.1: Architect Swift Package Manager and `InputMethodKit` structure in `platforms/macos/`.
- [ ] T9.2: Create C ABI bridge (`YangiEngineBridge.swift` / `CYangiCore`) connecting to `yangi-ffi`.
- [ ] T9.3: Implement `YangiInputController` (`IMKServerInput` protocol) with composing buffer, word transliteration, and backspace undo.
- [ ] T9.4: Implement password field detection via `IsSecureEventInputEnabled()` and immediate event bypass (Hard Rule 7).
- [ ] T9.5: Implement menu bar status item (`StatusBarController.swift`) with On/Off, Mode, Slang, ML, and Privacy dialog (D-03).
- [ ] T9.6: Implement unit tests (`YangiEngineTests.swift`, `UndoBufferTests.swift`, `SecurityTests.swift`).
- [ ] T9.7: Add macOS build and test step to GitHub Actions CI (`.github/workflows/ci.yml`) on `macos-latest`.
- [ ] T9.8: Verify zero network permissions, zero telemetry, and vector parity.

## Milestone 10: Linux IME (Workstream G) [QUEUED]
- [ ] T10.1: IBus C engine plugin consuming `yangi-ffi`.
- [ ] T10.2: Password field detection (`IBUS_INPUT_PURPOSE_PASSWORD`).
- [ ] T10.3: Packaging manifests (Flatpak / .deb).

## Milestone 11: Windows 1.6.1 Maintenance (Workstream H) [QUEUED]
- [ ] T11.1: Fix `ChatNormalizer.cs` bovot out-of-bounds bug on branch `windows-1.6.1`.
- [ ] T11.2: Implement 3-hour delayed update notifications in `Updater.cs`.
- [ ] T11.3: Fix testhost exit teardown in UI Automation tests.
- [ ] T11.4: Add optional compact data reader and bump version.

## Milestone 12: iOS Keyboard (Workstream I) [QUEUED]
- [ ] T12.1: Swift keyboard extension + container app using `yangi-ffi` static lib.
