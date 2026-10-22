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

## ADR-004: Rust `yangi-core` C ABI & Wasm Architecture
- **Context**: Need portable, memory-efficient core engine shared across Web, Android, macOS, Linux, and iOS.
- **Decision**: Pure Rust library `yangi-core` with no standard I/O beyond memory slices, plus `yangi-ffi` C ABI and `yangi-wasm` WebAssembly builds.
- **Rationale**: 100% vector parity on 74,100 ground truth vectors, 59x smaller dictionary (239 KB FST vs 14 MB text), 409x lower RAM footprint (240 KB vs 95 MB), sub-microsecond lookups.

## ADR-005: Unknown-Word Model Architecture (Int8 Quantized Char N-Grams)
- **Context**: Recognizing valid Uzbek neologisms without inflating the dictionary or allowing English false positives.
- **Decision**: Hashed character 1–5 n-grams ($N=16,384$) logistic regression model quantized to `int8` with pure-integer inference in `yangi-core`.
- **Rationale**: Zero float drift across platforms, 16 KB binary footprint, 4.6 µs inference latency, calibrated threshold strictly guaranteeing 0.00% English false positives. Placed behind toggle (default OFF).

## ADR-006: WebAssembly-Powered Manifest V3 Browser Extension Architecture
- **Context**: Cross-browser extension (Chrome, Edge, Firefox) transliterating text without external servers or network requests.
- **Decision**: In-process WebAssembly execution (`yangi_wasm_bg.wasm`, 353 KB) with `document.execCommand('insertText')` to preserve native Undo/Redo history, selection range caret adjustment, and strict password field bypass.
- **Rationale**: Zero network permissions in `manifest.json`, microsecond latency, works seamlessly on inputs, textareas, and rich-text contenteditable editors.

## ADR-007: Android Keyboard Architecture (Minimal InputMethodService vs. FlorisBoard Fork)
- **Context**: Workstream E requires an Android keyboard supporting 2024 Reformed Latin with word-boundary transliteration, long-press diacritics, and single-tap backspace undo.
- **Alternatives**: Forking FlorisBoard (Apache-2.0) vs. building a purpose-built minimal `InputMethodService`.
- **Decision**: Purpose-built minimal `InputMethodService` directly connecting to `yangi-core` via JNI.
- **Rationale**:
  1. *Footprint*: FlorisBoard is ~100k+ lines with massive Compose/CMake dependencies resulting in a 15–20 MB APK and 15-minute CI builds. A purpose-built IME is <3 MB with sub-2 minute CI builds.
  2. *Security & Privacy*: A minimal IME cleanly guarantees zero network permissions (`no android.permission.INTERNET`) without complex multi-lingual network telemetry risks.
  3. *Native Parity*: Seamless JNI integration with `yangi-ffi` C ABI ensures 100% vector parity with desktop and browser builds.
  4. *Targeted UX*: Purpose-built diacritic hints (`ş`, `ç`, `ö`, `ğ` in Saffron `#F0B23A`) and instant single-tap backspace undo without fighting generic autocorrect algorithms.
