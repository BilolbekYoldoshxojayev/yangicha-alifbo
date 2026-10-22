# PROJECT STATE — YangiAlifbo Multiplatform

## Status: IN PROGRESS (Milestone 9: macOS Input Method & Native IMEs)
- Branch: `multiplatform`
- Rust Core (`yangi-core`): 100% Vector Parity verified on 74,100 vectors (0 mismatches).
- C ABI (`yangi-ffi`): Static and dynamic C libraries compiled and verified, JNI bindings implemented for Android.
- WebAssembly (`yangi-wasm`): Optimized binary (353 KB) with embedded FST lexicon & int8 ML classifier.
- Browser Extension (`platforms/browser/`): 100% complete (52/52 automated tests pass, 243.5 KB distribution zip).
- Android Keyboard (`platforms/android/`): 100% complete (purpose-built minimal IME, JNI bridge, QWERTY layout with long-press diacritics, backspace undo, password safety, zero network permissions, CI run 39772066211 green).
- CI Matrix (`.github/workflows/ci.yml`): All 7 jobs passing green across Linux, macOS, and Windows.
- Active Workstream: Workstream F (macOS Input Method).

## Workstream Progress Summary
- **Workstream A (Ground Truth)**: COMPLETE (74,100 test vectors, eval set, benchmarks).
- **Workstream B (Rust Core + Compact Data)**: COMPLETE (239 KB dictionary, 59x size reduction, 409x RAM reduction, 100% parity).
- **Workstream C (Unknown-Word Model)**: COMPLETE (16 KB int8 char n-gram classifier, 0 English false positives, +6.3% eval recall).
- **Workstream D (Browser Extension)**: COMPLETE (MV3 extension, zero network permissions, real-time input/textarea/contenteditable transliteration, caret preservation, password safety, popup with live sandbox).
- **Workstream J (CI Workflows)**: COMPLETE (Multi-OS Rust core + 74k parity, Wasm & browser tests, .NET tests, Android tests, strict privacy lint).
- **Workstream E (Android Keyboard)**: COMPLETE (Kotlin InputMethodService, JNI bridge, Latin QWERTY layout, backspace undo, zero network permissions).
- **Workstream F (macOS Input Method)**: NEXT IN QUEUE.
- **Workstream G (Linux IME)**: QUEUED.
- **Workstream H (Windows 1.6.1 Maintenance)**: QUEUED on branch `windows-1.6.1`.
- **Workstream I (iOS Keyboard)**: QUEUED (lowest priority).

## Security & Privacy Invariants
- Zero typed text transmission over network on any platform.
- Zero disk logging of keystrokes.
- Password/secure fields strictly bypassed.
- No release publishing or tag pushes on `multiplatform`.
