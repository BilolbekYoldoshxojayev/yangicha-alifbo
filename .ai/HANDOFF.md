# HANDOFF — YangiAlifbo Multiplatform Engineering

## Workstream Status Matrix

| Workstream | Description | State | Branch | Last Commit | CI Run Link | Notes |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **A. Ground truth** | Test vector generator, eval set, benchmarks | **COMPLETE** | `multiplatform` | Pending commit | Untested in CI / Local Verified | `tools/vectors`, `tools/eval`, `tools/bench` fully working |
| **B. Compact data + Rust core** | Coverage experiment, FST binary format, `yangi-core`, FFI, wasm | **In Progress** | `multiplatform` | — | — | Starting B.1 coverage experiment & B.2 binary format |
| **C. Unknown-word model** | Char n-gram quantized classifier (int8) | **Queued** | `multiplatform` | — | — | Depends on B |
| **D. Browser extension** | Chrome/Edge/Firefox MV3 extension via wasm core | **Queued** | `multiplatform` | — | — | Depends on B |
| **E. Android keyboard** | Kotlin InputMethodService + JNI | **Queued** | `multiplatform` | — | — | Depends on B |
| **F. macOS input method** | Swift InputMethodKit + menu bar app | **Queued** | `multiplatform` | — | — | Depends on B |
| **G. Linux IME** | IBus engine + Fcitx5 addon via C ABI | **Queued** | `multiplatform` | — | — | Depends on B |
| **H. Windows app follow-ups** | Update notices, compact data reader, testhost hang fix, v1.6.1 | **Queued** | `windows-1.6.1` | — | — | Branch `windows-1.6.1` only |
| **I. iOS keyboard** | Swift keyboard extension + container app | **Queued** | `multiplatform` | — | — | Lowest priority (after B) |
| **J. CI Workflows** | GitHub Actions multiplatform matrix | **Queued** | `multiplatform` | — | — | Core, wasm, platforms |

---

## Measured Baseline Numbers (C# Engine v1.6.0)

Every metric recorded below comes directly from commands executed in this environment.

### Unit Tests
- **Automated Tests**: 257 MSTest unit tests passed (0 failed, 0 skipped, duration 3s).
  - Command: `$dotnet test tests\YangiAlifbo.Tests.csproj -c Release --blame-hang-timeout 120s`
  - Note: Known testhost exit hang occurred after completion as documented in `.agent/mission.md`.

### Workstream A Deliverables & Measurements
- **Test Vectors Generated**:
  - Sample vectors: 48,506 entries (`tools/vectors/test_vectors_sample.jsonl`, 21,037,976 bytes)
  - Full evaluated vectors: 74,100 entries (`tools/vectors/test_vectors.jsonl.gz`, 1,687,111 bytes / 1.61 MB)
  - Generator project: `tools/vectors/VectorsGenerator.csproj`
  - Command: `$dotnet run --project tools\vectors\VectorsGenerator.csproj -c Release`

- **Evaluation Harness (`tools/eval/evaluate.py`)**:
  - Command: `python tools\eval\evaluate.py`
  - Dataset: 24 real-world cases (Constitution of Uzbekistan, Qodiriy, Cho'lpon, Tutuq corpus, Python/Rust/JS/Bash code, English literature)
  - Result:
    - English/Code False Positives: **0 / 8 (0.0% false positives, 100% pass)** — Strict compliance with Hard Rule 4!
    - Uzbek Sentences: 14 / 16 passed (87.5% pass, 2 baseline misses identified: `yoğar` and `körinar`)

- **Performance Benchmarks (`tools/bench/Bench.csproj`)**:
  - Command: `$dotnet run --project tools\bench\Bench.csproj -c Release`
  - Report saved: `tools/bench/baseline_benchmark.json`
  - **Dictionary Sizes on Disk**:
    - `uzbek_words.txt`: 14,117,721 bytes (13.46 MB)
    - `english_reject.txt`: 86,354 bytes (84.33 KB)
  - **Resource Footprint**:
    - Cold Load Time: **413.91 ms**
    - RAM Delta: **95.66 MB**
    - Total Working Set: **122.97 MB**
  - **Latency (100,000 iterations per operation)**:
    - Exact Uzbek match (`shahar`): **0.797 µs / op**
    - Morphological match (`shaharimizdagilardan`): **1.664 µs / op**
    - English reject (`shock`): **0.611 µs / op**
    - Unknown word (`qwertyuiop`): **0.641 µs / op**
    - Phonetics Convert (`shahar` -> `şahar`): **0.820 µs / op**
    - Sentence conversion (10 words): **17.459 µs / op (~572,777 words/sec)**

---

## Decisions & Rejected Approaches

1. **Branch Strategy**:
   - Working strictly on branch `multiplatform`. Never push to `main`.
2. **Untracked Scripts & Shipped Windows Source Protection**:
   - Untracked scripts in root left untouched.
   - `src/` directory untouched on `multiplatform` (reserved strictly for Workstream H on `windows-1.6.1`).
3. **Safe Evaluation & Vector Generation**:
   - Discovered that `ChatNormalizer.Normalize` crashes with `ArgumentOutOfRangeException` on 5-character string `"bovot"` due to `norm[6..]`. Handled safely in tools layer and logged for fix in Workstream H on branch `windows-1.6.1`.

---

## Known Bugs & Open Questions

1. **Testhost Hang**:
   - `dotnet test` hangs after running tests due to a lingering background thread/timer. Fix assigned to Workstream H on `windows-1.6.1`.
2. **ChatNormalizer Index Bug on 'bovot'**:
   - `norm.StartsWith("bovot")` with `norm[6..]` crashes on input length 5. Fix assigned to Workstream H on `windows-1.6.1`.
3. **Uzbek Word Misses in Baseline v1.6.0**:
   - `yoğar` and `körinar` missed in morphological analysis in C# engine.

---

## Exact Prioritised Next Steps

1. **Workstream B.1**: Run coverage experiment: evaluate whether ~32K roots + the suffix analyzer can recognise all 948K generated forms. Quantify percentage and failure categories; generate minimal exceptions list for 100% coverage.
2. **Workstream B.2**: Design compact binary format (`data/FORMAT.md`) using FST for roots, exceptions, and reject list.
3. **Workstream B.3**: Build `core/` Rust crate (`yangi-core`) implementing `UzbekPhonetics`, `LanguageDetector`, `ChatNormalizer`, `TextConverter`. Verify 100% vector parity against `test_vectors.jsonl.gz`.
4. **Workstream B.4**: Implement FFI C ABI headers and wasm-pack build.
