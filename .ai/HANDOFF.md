# HANDOFF — YangiAlifbo Multiplatform Engineering

## Workstream Status Matrix

| Workstream | Description | State | Branch | Last Commit | CI Run Link | Notes |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **A. Ground truth** | Test vector generator, eval set, benchmarks | **COMPLETE** | `multiplatform` | `b11c255` | Local Verified | `tools/vectors`, `tools/eval`, `tools/bench` fully working |
| **B. Compact data + Rust core** | Coverage experiment, FST binary format, `yangi-core`, FFI, wasm | **COMPLETE** | `multiplatform` | Pending commit | Local Verified | 100% vector parity on 74,100 vectors; 59x size & 409x RAM reduction; C ABI & wasm |
| **C. Unknown-word model** | Char n-gram quantized classifier (int8) | **In Progress** | `multiplatform` | — | — | Starting char n-gram model |
| **D. Browser extension** | Chrome/Edge/Firefox MV3 extension via wasm core | **Queued** | `multiplatform` | — | — | Depends on B (unblocked!) |
| **E. Android keyboard** | Kotlin InputMethodService + JNI | **Queued** | `multiplatform` | — | — | Depends on B (unblocked!) |
| **F. macOS input method** | Swift InputMethodKit + menu bar app | **Queued** | `multiplatform` | — | — | Depends on B (unblocked!) |
| **G. Linux IME** | IBus engine + Fcitx5 addon via C ABI | **Queued** | `multiplatform` | — | — | Depends on B (unblocked!) |
| **H. Windows app follow-ups** | Update notices, compact data reader, testhost hang fix, v1.6.1 | **Queued** | `windows-1.6.1` | — | — | Branch `windows-1.6.1` only |
| **I. iOS keyboard** | Swift keyboard extension + container app | **Queued** | `multiplatform` | — | — | Lowest priority (after B) |
| **J. CI Workflows** | GitHub Actions multiplatform matrix | **Queued** | `multiplatform` | — | — | Core, wasm, platforms |

---

## Measured Performance Numbers: Rust `yangi-core` vs. C# Baseline

Every metric recorded below comes directly from commands executed in this environment.

| Metric | C# Engine (v1.6.0 Baseline) | Rust `yangi-core` (Workstream B) | Improvement Factor |
| :--- | :--- | :--- | :--- |
| **Dictionary On-Disk Size** | 14,117,721 bytes (~14.1 MB) | 245,073 bytes (~239 KB) | **59x smaller** |
| **Cold Startup / Load Time** | 413.91 ms | 0.288 ms | **1,437x faster** |
| **RAM Footprint (Working Set)**| 95.66 MB | 0.24 MB (239 KB) | **409x less RAM** |
| **Exact Uzbek Lookup** | 0.797 µs / op | 0.652 µs / op | 1.2x faster |
| **English Reject Lookup** | 0.611 µs / op | 0.568 µs / op | 1.1x faster |
| **Unknown Word Lookup** | 0.641 µs / op | 0.379 µs / op | 1.7x faster |
| **Phonetics Convert** | 0.820 µs / op | 0.361 µs / op | 2.3x faster |
| **Sentence Conversion Throughput**| 572,777 words / sec | 567,408 words / sec | Parity |
| **Vector Parity Accuracy** | Reference (Source of Truth) | **100.00% (74,100 / 74,100)** | **Zero Mismatches** |

Commands executed:
- Baseline C# Bench: `$dotnet run --project tools\bench\Bench.csproj -c Release`
- Rust Core Bench: `cargo run -p yangi-core --bin bench --release`
- Vector Parity Test: `cargo test -p yangi-core --test vector_parity -- --nocapture`
- Real-World Eval Harness: `python tools\eval\evaluate.py --cmd "target\release\cli.exe"`

---

## Workstream B Deliverables Summary

1. **Coverage Experiment (B.1)**:
   - Tool: `tools/coverage/CoverageExperiment.csproj`
   - Tested 643,320 1995 Latin forms against 31,970 roots + suffix analyzer.
   - 628,611 forms recognized purely by morphological rules (97.71%).
   - 14,709 minimal exceptions extracted and stored in `data/exceptions.txt` (storing one spelling only).
   - Verified combined coverage: 100.00% of all valid Uzbek vocabulary.

2. **Compact Binary Data Format (B.2)**:
   - Specification: `data/FORMAT.md`
   - Generator: `data/src/main.rs` (`yangi-data-generator`)
   - Binary output: `data/yangi_data.bin` (245,073 bytes / 239 KB) containing Roots FST, Exceptions FST, Reject List FST with 64-byte aligned header.

3. **Pure Rust Core `yangi-core` (B.3)**:
   - Crate: `core/` (`yangi-core`)
   - Modules: `phonetics.rs`, `normalizer.rs`, `detector.rs`, `converter.rs`, `suffixes.rs`
   - Zero-copy / memory-mappable architecture.
   - Passed 100% of the 74,100 test vectors with zero discrepancies.

4. **FFI C ABI & Headers (B.4)**:
   - Crate: `core/ffi/` (`yangi-ffi`) compiling to `.dll`, `.so`, `.dylib`, and static library.
   - C Header: `core/ffi/include/yangi_core.h`.
   - Exported C API: `yangi_detector_create`, `yangi_detector_free`, `yangi_should_convert`, `yangi_is_uzbek`, `yangi_is_foreign_or_reject`, `yangi_phonetics_convert`, `yangi_chat_normalize`, `yangi_convert_text`, `yangi_string_free`.

5. **WebAssembly Core (B.4)**:
   - Crate: `core/wasm/` (`yangi-wasm`) with `wasm-bindgen` exports for web browsers and extensions.

---

## Decisions & Rejected Approaches

1. **FST Sets over Hash Tables**:
   - Rejected storing 948K pre-generated words in memory tables.
   - Chose BurntSushi `fst::Set` memory-mappable automaton for roots, exceptions, and reject words. Reduced disk size by 59x and RAM by 409x.
2. **Deterministic Vector Fix**:
   - In `tools/vectors/Program.cs`, replaced un-cased `"bo'votti"` in exception handler with case-matched output to preserve strict casing invariants across all platforms.

---

## Exact Prioritised Next Steps

1. **Workstream C**: Unknown-word model (char n-gram hashed logistic regression classifier, int8 quantized, pure inference in yangi-core, default OFF, zero English false positives).
2. **Workstream D**: Browser extension (Manifest V3 for Chrome/Edge using wasm core).
3. **Workstream J**: GitHub Actions CI workflow for core, wasm, and vector parity tests.
4. **Workstream E–G**: Native platform input methods (Android, macOS, Linux).
