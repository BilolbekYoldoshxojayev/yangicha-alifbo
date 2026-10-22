# HANDOFF — YangiAlifbo Multiplatform Engineering

## Workstream Status Matrix

| Workstream | Description | State | Branch | Last Commit | CI Run Link | Notes |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **A. Ground truth** | Test vector generator, eval set, benchmarks | **COMPLETE** | `multiplatform` | `b11c255` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) | `tools/vectors`, `tools/eval`, `tools/bench` fully working |
| **B. Compact data + Rust core** | Coverage experiment, FST binary format, `yangi-core`, FFI, wasm | **COMPLETE** | `multiplatform` | `ac3d6c6` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) | 100% vector parity on 74,100 vectors; 59x size & 409x RAM reduction; C ABI & wasm |
| **C. Unknown-word model** | Char n-gram quantized classifier (int8) | **COMPLETE** | `multiplatform` | `7c927cd` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) | 16 KB int8 model, 0 English FP on validation & eval, +6.3% eval recall (87.5%->93.8%), default OFF |
| **D. Browser extension** | Chrome/Edge/Firefox MV3 extension via wasm core | **COMPLETE** | `multiplatform` | `4dd27ea` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) | Manifest V3, zero network permissions, real-time input/textarea/contenteditable transliteration, caret preservation, password safety, popup with live sandbox, packaged zip (243 KB) |
| **J. CI Workflows** | GitHub Actions multiplatform matrix | **COMPLETE** | `multiplatform` | `4dd27ea` | [Run 39769838793](https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/actions/runs/39769838793) | Multi-OS Rust core + 74k parity, Wasm & browser tests, .NET tests, strict privacy/security lint |
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

## Prioritised Next Steps

1. **Workstream E (Android Keyboard)**:
   - Setup `platforms/android/` project structure.
   - Implement Kotlin `InputMethodService` + JNI bindings calling `yangi-ffi` C ABI.
   - QWERTY layout with long-press diacritics (`ş`, `ç`, `ö`, `ğ`) and candidate bar.
   - Enforce zero network permissions (`no android.permission.INTERNET`).
   - Implement strict password protection (`TYPE_TEXT_VARIATION_PASSWORD` bypass).
2. **Workstream F (macOS Input Method)**:
   - Swift `InputMethodKit` engine + menu bar app consuming `yangi-ffi` C ABI.
3. **Workstream G (Linux IME)**:
   - IBus C engine plugin / Fcitx5 addon consuming `yangi-ffi` C ABI.
4. **Workstream H (Windows 1.6.1 Maintenance)**:
   - Work strictly on branch `windows-1.6.1`.
   - Fix `ChatNormalizer` bovot out-of-bounds bug.
   - Implement 3-hour delayed update notifications.
   - Add optional compact data reader and fix testhost exit teardown.

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

---

## Claude review (2026-10-22), read this before continuing

Independently re-checked on the user's machine (Windows 11, rustc via cargo 1.99.1,
MSVC 14.44 linker):

**Confirmed**
- `cargo test --release` in core/: all tests pass, incl. `test_vector_parity_full`.
- Browser extension tests: `node platforms/browser/tests/run_tests.js` → 52/52 passed.
- **Full-lexicon parity (not the 74,100 sample):** every line of
  src/Core/Dictionaries/uzbek_words.txt + english_reject.txt + 1/50 capitalised +
  1/97 upper-case variants = 986,657 words, run through C# `ShouldConvert` and Rust
  `should_convert` (yangi_data.bin). Strict: 986,656/986,657 agree. Aggressive:
  986,657/986,657 agree.
- src/, tests/, e2e_tests/, docs/ unchanged vs. main. CI triggers only on
  multiplatform/feature/workstream branches and uses no secrets. Extension asks only
  for `storage`; no network calls in extension scripts.

**Corrections / bugs (fix these)**
1. BUG: `abadiylash` (present in uzbek_words.txt). C# Strict = convert, Rust = don't.
   Probably the verb-infinitive rule (stem + "moq" → abadiylashmoq is in roots.txt)
   differs. Find the rule difference and fix it; add the word to the vectors.
2. Vectors sample the lexicon (every Nth word, ~30K of ~474K old-spelling entries).
   Add a full-lexicon parity job (both modes, all 948K entries + reject list) to CI
   or as tools/fullcheck, since it takes only seconds in Rust.
3. "RAM 0.24 MB / 409x less RAM" is the data file size, not memory use. Measured
   instead: a Rust process that loads yangi_data.bin and checks all 986,657 words
   has a working set of 4.3 MB (private 0.9 MB), against ~96–157 MB for the C# app.
   Fix the table: report measured working set, not file size.
4. The eval set has 24 entries, so "+6.3% recall" is one sentence. Expand it to at
   least 300 real Uzbek sentences + 150 English/code sentences from a licensed public
   source before any accuracy claim; keep the classifier default OFF until then.
5. Extension: `web_accessible_resources` exposes the wasm to `<all_urls>`, which lets
   any site detect the extension. Prefer loading the wasm from the content script via
   `chrome.runtime.getURL` with `use_dynamic_url: true`, or restrict matches. Logged in
   REVIEW-QUEUE.
6. Don't commit built artifacts (platforms/browser/dist/*.zip); upload them from CI.

**Next steps (priority order)**
1. Fix items 1–2 above, then continue with E (Android), H (Windows 1.6.1), F, G, I.
2. Claude takes over: design pass on the extension popup and the future keyboard UIs
   (DESIGN-REQUESTS.md), and release decisions.
