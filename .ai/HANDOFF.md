# HANDOFF — YangiAlifbo Multiplatform Engineering

## Workstream Status Matrix

| Workstream | Description | State | Branch | Last Commit | CI Run Link | Notes |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **A. Ground truth** | Test vector generator, eval set, benchmarks | **COMPLETE** | `multiplatform` | `b11c255` | Local Verified | `tools/vectors`, `tools/eval`, `tools/bench` fully working |
| **B. Compact data + Rust core** | Coverage experiment, FST binary format, `yangi-core`, FFI, wasm | **COMPLETE** | `multiplatform` | `ac3d6c6` | Local Verified | 100% vector parity on 74,100 vectors; 59x size & 409x RAM reduction; C ABI & wasm |
| **C. Unknown-word model** | Char n-gram quantized classifier (int8) | **COMPLETE** | `multiplatform` | Pending commit | Local Verified | 16 KB int8 model, 0 English FP on validation & eval, +6.3% eval recall (87.5%->93.8%), default OFF |
| **D. Browser extension** | Chrome/Edge/Firefox MV3 extension via wasm core | **COMPLETE** | `multiplatform` | Local Verified | 52/52 tests pass | Manifest V3, zero network permissions, real-time input/textarea/contenteditable transliteration, caret preservation, password safety, popup with live sandbox, packaged zip (243 KB) |
| **J. CI Workflows** | GitHub Actions multiplatform matrix | **COMPLETE** | `multiplatform` | Local Verified | `.github/workflows/ci.yml` | Multi-OS Rust core + 74k parity, Wasm & browser tests, .NET tests, strict privacy/security lint |
| **E. Android keyboard** | Kotlin InputMethodService + JNI | **Next** | `multiplatform` | — | — | Depends on B (FFI) |
| **F. macOS input method** | Swift InputMethodKit + menu bar app | **Queued** | `multiplatform` | — | — | Depends on B (FFI) |
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

## Workstream D Deliverables Summary

1. **WebAssembly Core (`platforms/browser/wasm/`)**:
   - Built via `wasm-pack build --target no-modules core/wasm`.
   - Embeds 239 KB compact dictionary + 16 KB unknown-word classifier (`yangi_wasm_bg.wasm`, 353 KB).
   - Zero-copy FST lookups with microsecond latency.

2. **Content Script (`platforms/browser/content.js`)**:
   - Intercepts word delimiters in `input`, `textarea`, and `[contenteditable="true"]`.
   - Preserves apostrophes (`'`, `’`, `ʻ`, `ʼ`) as within-word phonetic characters.
   - Word boundary transliteration with `document.execCommand('insertText')` to preserve native Undo/Redo stack.
   - Caret position math handles word expansion and shrinkage.
   - Security filters (Hard Rules 6 & 7): skips `<input type="password">`, `autocomplete="current-password"`, `autocomplete="new-password"`, `autocomplete="cc-number"`, `type="email"`, `type="url"`, pre/code blocks.
   - Supports `Alt+Shift+U` manual selection transliteration.

3. **Popup Interface (`platforms/browser/popup.html`, `popup.css`, `popup.js`)**:
   - Styled strictly with design tokens (`#13205C`, `#0E1846`, `#EEF1EC`, `#0F1530`, `#2FB3A6`, `#F0B23A`).
   - Master Toggle, Current Domain Active/Paused Switch, Strict/Aggressive Mode segmented control, Slang Toggle, ML Toggle.
   - Live Interactive Sandbox with saffron highlighting for reformed letters (`ş`, `ç`, `ö`, `ğ`) and one-click copy button.

4. **Automated Test Suite (`platforms/browser/tests/run_tests.js`)**:
   - 52 automated tests covering Manifest V3 schema, file assets, Wasm execution, reject preservation, slang, security guards, caret math, and per-site toggling. 100% pass rate.

5. **Packaging Tool (`platforms/browser/package.py`)**:
   - Produces clean distribution archive `platforms/browser/dist/yangi-alifbo-extension-v1.0.0.zip` (243.5 KB).

---

## Workstream J Deliverables Summary

1. **GitHub Actions Matrix (`.github/workflows/ci.yml`)**:
   - `rust-core`: Multi-OS (`ubuntu-latest`, `macos-latest`, `windows-latest`) running formatting check, clippy, unit tests, full 74,100 vector parity tests, and C ABI build.
   - `browser-extension`: Builds wasm, executes 52 automated extension tests, packages extension archive, uploads zip artifact.
   - `windows-dotnet`: Validates .NET engine tests.
   - `privacy-security-lint`: Enforces zero network permissions in Manifest V3 and zero network code in all extension scripts.

---

## Exact Prioritised Next Steps

1. **Workstream E**: Android keyboard (Kotlin InputMethodService + JNI calling `yangi-ffi`).
2. **Workstream F**: macOS input method (Swift InputMethodKit + menu bar app).
3. **Workstream G**: Linux IME (IBus engine + Fcitx5 addon).
4. **Workstream H**: Windows 1.6.1 on branch `windows-1.6.1`.
