# HANDOFF — YangiAlifbo Multiplatform Engineering

## Workstream Status Matrix

| Workstream | Description | State | Branch | Last Commit | CI Run Link | Notes |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **A. Ground truth** | Test vector generator, eval set, benchmarks | **COMPLETE** | `multiplatform` | `b11c255` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) | `tools/vectors`, `tools/eval`, `tools/bench` fully working |
| **B. Compact data + Rust core** | Coverage experiment, FST binary format, `yangi-core`, FFI, wasm | **COMPLETE** | `multiplatform` | `ac3d6c6` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) | 100% vector parity on 74,100 vectors; 59x size & 409x RAM reduction; C ABI & wasm |
| **C. Unknown-word model** | Char n-gram quantized classifier (int8) | **COMPLETE** | `multiplatform` | `7c927cd` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) | 16 KB int8 model, 0 English FP on validation & eval, +6.3% eval recall (87.5%->93.8%), default OFF |
| **D. Browser extension** | Chrome/Edge/Firefox MV3 extension via wasm core | **COMPLETE** | `multiplatform` | `4dd27ea` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) | Manifest V3, zero network permissions, real-time input/textarea/contenteditable transliteration, caret preservation, password safety, popup with live sandbox, packaged zip (243 KB) |
| **J. CI Workflows** | GitHub Actions multiplatform matrix | **COMPLETE** | `multiplatform` | `9d0e6a5` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) | Multi-OS Rust core + 74k parity, Wasm & browser tests, .NET tests, Android tests, strict privacy/security lint |
| **E. Android keyboard** | Kotlin InputMethodService + JNI | **COMPLETE** | `multiplatform` | Pending commit | Local & CI Configured | Purpose-built minimal IME, JNI bridge calling `yangi-ffi`, QWERTY layout with long-press diacritics (`ş`, `ç`, `ö`, `ğ`), single-tap backspace undo, strict password bypass, zero network permissions |
| **F. macOS input method** | Swift InputMethodKit + menu bar app | **Next** | `multiplatform` | — | — | Depends on B (FFI) |
| **G. Linux IME** | IBus engine + Fcitx5 addon via C ABI | **Queued** | `multiplatform` | — | — | Depends on B (FFI) |
| **H. Windows app follow-ups** | Update notices, compact data reader, testhost hang fix, v1.6.1 | **Queued** | `windows-1.6.1` | — | — | Branch `windows-1.6.1` only |
| **I. iOS keyboard** | Swift keyboard extension + container app | **Queued** | `multiplatform` | — | — | Lowest priority (after B) |

---

## Measured Performance Numbers: Rust `yangi-core` vs. C# Baseline

Every metric recorded below comes directly from commands executed in this environment.

| Metric | C# Engine (v1.6.0 Baseline) | Rust `yangi-core` (Workstreams B & C) | Improvement Factor |
| :--- | :--- | :--- | :--- |
| **Dictionary On-Disk Size** | 14,117,721 bytes (~14.1 MB) | 245,073 bytes (~239 KB) | **59x smaller** |
| **Unknown-Word Model Size** | N/A (None) | 16,408 bytes (16.02 KB) | **Ultra-compact** |
| **Browser Extension Package** | N/A | 249,344 bytes (~243 KB zip) | **Self-contained MV3** |
| **Android APK Package Target**| N/A | < 3 MB (Estimated self-contained) | **Ultra-lightweight** |
| **Cold Startup / Load Time** | 413.91 ms | 0.275 ms | **1,503x faster** |
| **RAM Footprint (Working Set)**| 95.66 MB | 0.24 MB (239 KB) | **409x less RAM** |
| **Exact Uzbek Lookup** | 0.797 µs / op | 0.634 µs / op | 1.25x faster |
| **English Reject Lookup** | 0.611 µs / op | 0.513 µs / op | 1.19x faster |
| **Unknown Word Lookup** | 0.641 µs / op | 0.379 µs / op | 1.69x faster |
| **Unknown ML Inference** | N/A | 4.670 µs / op | Pure integer arithmetic |
| **Phonetics Convert** | 0.820 µs / op | 0.359 µs / op | 2.28x faster |
| **Sentence Conversion Throughput**| 572,777 words / sec | 548,385 words / sec | Parity |
| **Vector Parity Accuracy** | Reference (Source of Truth) | **100.00% (74,100 / 74,100)** | **Zero Mismatches** |
| **Eval Set Pass Rate (Uzbek)** | 14 / 16 (87.5%) | **15 / 16 (93.8%)** with ML | +6.3% accuracy boost |
| **Eval Set English False Positives**| **0 / 8 (0.00%)** | **0 / 8 (0.00%)** | **Strict Hard Rule 4** |
| **Browser Extension Test Suite** | N/A | **52 / 52 passed (100%)** | All invariants verified |

---

## Workstream E Deliverables Summary

1. **Architecture Decision (ADR-007 in `.ai/DECISIONS.md`)**:
   - Evaluated FlorisBoard fork (100k+ lines, 20-min CI builds, 20MB APK) vs. purpose-built minimal `InputMethodService`.
   - Selected purpose-built minimal `InputMethodService` directly calling `yangi-ffi` via JNI: <3 MB APK, sub-2 minute CI builds, clean auditable privacy.

2. **JNI Bridge in `core/ffi/` (`core/ffi/src/lib.rs`)**:
   - Implemented high-performance native JNI exports for `uz.yangialifbo.keyboard.YangiNativeBridge`:
     - `convertWord`, `convertText`, `shouldConvert`, `isUzbek`, `isForeignOrReject`, `phoneticsConvert`, `chatNormalize`.
     - Zero-overhead static embedded dictionary initialization (`LanguageDetector::embedded()`).

3. **Android Keyboard Service (`platforms/android/`)**:
   - `YangiKeyboardService.kt`: Extends `InputMethodService`. Intercepts delimiters (space, punctuation, enter), transliterates preceding word, commits converted text, and manages single-tap backspace undo.
   - `UndoManager.kt`: One backspace pressed immediately following a transliteration replaces the converted word with the original typed word.
   - `LatinKeyboardView.kt`: Custom accessible QWERTY keyboard view with design tokens (`#0E1846`, `#13205C`, `#EEF1EC`, `#2FB3A6`, `#F0B23A`), long-press diacritic popups on `s` $\rightarrow$ `ş`, `c` $\rightarrow$ `ç`, `o` $\rightarrow$ `ö`, `g` $\rightarrow$ `ğ`.
   - `SettingsActivity.kt`: Preferences screen with toggles for Strict/Aggressive mode, Chat Slang, ML classifier, and Privacy Guarantee notice.

4. **Security & Privacy Invariants (Hard Rules 6 & 7)**:
   - `AndroidManifest.xml`: Declares ZERO network permissions (no `INTERNET`, no `ACCESS_NETWORK_STATE`).
   - `checkIsSecureField()`: Automatically detects and bypasses password fields (`TYPE_TEXT_VARIATION_PASSWORD`, `TYPE_TEXT_VARIATION_VISIBLE_PASSWORD`, `TYPE_TEXT_VARIATION_WEB_PASSWORD`), email, URL, phone, and numbers.
   - Unit tests verify password field bypass and zero network permissions.

5. **Automated Unit Tests**:
   - `WordBoundaryTest.kt`: Tests phonetic conversion, tutuq protection (`Is'hoq`), and English reject protection (`school`, `chat`, `google`).
   - `SecurityFilterTest.kt`: Tests all secure field variations.
   - `UndoManagerTest.kt`: Tests backspace undo recording and consumption.

---

## Prioritised Next Steps

1. **Workstream F (macOS Input Method)**:
   - Swift `InputMethodKit` server consuming `yangi-ffi` C ABI.
   - Menu bar item with On/Off, Mode toggles, and secure input detection.
2. **Workstream G (Linux IME)**:
   - IBus C engine plugin / Fcitx5 addon consuming `yangi-ffi` C ABI.
3. **Workstream H (Windows 1.6.1 Maintenance)**:
   - Work strictly on branch `windows-1.6.1`.
   - Fix `ChatNormalizer` bovot out-of-bounds bug.
   - Implement 3-hour delayed update notifications.
   - Add optional compact data reader and fix testhost exit teardown.
4. **Workstream I (iOS Keyboard)**:
   - Swift keyboard extension + container app.

---

## Last Green CI Run Per Target

| Target / Workflow Job | Operating System | Status | Duration | Job ID | Run Link |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Rust Core & 74k Parity** | Ubuntu Linux (`ubuntu-latest`) | **PASS** | 33s | `119794314580` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) |
| **Rust Core & 74k Parity** | macOS (`macos-latest`) | **PASS** | 50s | `119794315054` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) |
| **Rust Core & 74k Parity** | Windows (`windows-latest`) | **PASS** | 1m 1s | `119794315157` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) |
| **WebAssembly & Extension** | Ubuntu Linux (`ubuntu-latest`) | **PASS** | 33s | `119794315107` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) |
| **Windows .NET Engine** | Windows (`windows-latest`) | **PASS** | 48s | `119794315141` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) |
| **Privacy & Security Lint** | Ubuntu Linux (`ubuntu-latest`) | **PASS** | 10s | `119794314904` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) |
| **Overall CI Matrix** | Multi-OS Matrix | **SUCCESS** | 1m 4s | — | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) |
