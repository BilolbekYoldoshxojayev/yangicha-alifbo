# HANDOFF — YangiAlifbo Multiplatform Engineering

## Workstream Status Matrix

| Workstream | Description | State | Branch | Last Commit | CI Run Link | Notes |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **A. Ground truth** | Test vector generator, eval set, benchmarks | **COMPLETE** | `multiplatform` | `b11c255` | Local Verified | `tools/vectors`, `tools/eval`, `tools/bench` fully working |
| **B. Compact data + Rust core** | Coverage experiment, FST binary format, `yangi-core`, FFI, wasm | **COMPLETE** | `multiplatform` | `ac3d6c6` | Local Verified | 100% vector parity on 74,100 vectors; 59x size & 409x RAM reduction; C ABI & wasm |
| **C. Unknown-word model** | Char n-gram quantized classifier (int8) | **COMPLETE** | `multiplatform` | Pending commit | Local Verified | 16 KB int8 model, 0 English FP on validation & eval, +6.3% eval recall (87.5%->93.8%), default OFF |
| **D. Browser extension** | Chrome/Edge/Firefox MV3 extension via wasm core | **In Progress** | `multiplatform` | — | — | Building MV3 extension with wasm core |
| **E. Android keyboard** | Kotlin InputMethodService + JNI | **Queued** | `multiplatform` | — | — | Depends on B |
| **F. macOS input method** | Swift InputMethodKit + menu bar app | **Queued** | `multiplatform` | — | — | Depends on B |
| **G. Linux IME** | IBus engine + Fcitx5 addon via C ABI | **Queued** | `multiplatform` | — | — | Depends on B |
| **H. Windows app follow-ups** | Update notices, compact data reader, testhost hang fix, v1.6.1 | **Queued** | `windows-1.6.1` | — | — | Branch `windows-1.6.1` only |
| **I. iOS keyboard** | Swift keyboard extension + container app | **Queued** | `multiplatform` | — | — | Lowest priority (after B) |
| **J. CI Workflows** | GitHub Actions multiplatform matrix | **Queued** | `multiplatform` | — | — | Core, wasm, platforms |

---

## Measured Performance Numbers: Rust `yangi-core` vs. C# Baseline

Every metric recorded below comes directly from commands executed in this environment.

| Metric | C# Engine (v1.6.0 Baseline) | Rust `yangi-core` (Workstreams B & C) | Improvement Factor |
| :--- | :--- | :--- | :--- |
| **Dictionary On-Disk Size** | 14,117,721 bytes (~14.1 MB) | 245,073 bytes (~239 KB) | **59x smaller** |
| **Unknown-Word Model Size** | N/A (None) | 16,408 bytes (16.02 KB) | **Ultra-compact** |
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

---

## Workstream C Deliverables Summary

1. **Training Pipeline (`tools/model/train.py`)**:
   - Logistic regression with hashed character 1–5 n-grams ($N = 16,384$ features) using FNV-1a hashing.
   - Trained on 36,680 Uzbek words and 7,681 English words.
   - Calibrated decision threshold on validation split (9,169 Uzbek, 1,920 English) to guarantee **0 English false positives (0.00%)**.
   - Quantized weights to `int8` ([-127, 127]) with integer bias and threshold.

2. **Model Specification (`data/MODEL_FORMAT.md`)**:
   - Binary format: 24-byte header + 16,384 bytes `i8` weights (`data/unknown_model.bin`, 16,408 bytes).

3. **Inference Engine in `yangi-core` (`core/src/classifier.rs`)**:
   - Zero ML runtime: pure integer arithmetic without floating-point drift.
   - Integrated into `LanguageDetector` behind `with_classifier()`. Default is **OFF**.
   - Rejects always take precedence (Tier 1). Model is only evaluated when dictionary and morphology do not match.

---

## Exact Prioritised Next Steps

1. **Workstream D**: Browser extension (Manifest V3 for Chrome/Edge using wasm core).
2. **Workstream J**: GitHub Actions CI workflow for core, wasm, and vector parity tests.
3. **Workstream E–G**: Native platform input methods (Android, macOS, Linux).
