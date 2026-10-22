# YangiAlifbo Unknown-Word Model Format (v1)

`data/MODEL_FORMAT.md` specifies the binary format for the lightweight, int8-quantized character n-gram logistic regression classifier (`data/unknown_model.bin`).

---

## 1. Purpose & Design Principles
- **Purpose**: Classify whether an out-of-dictionary word (a neologism, slang, or rare inflected form) with convertible sequences (`sh`, `ch`, `o'`, `g'`) is Uzbek or foreign.
- **Safety First (Hard Rule 4)**: The model is evaluated ONLY when:
  1. The feature flag / setting `use_unknown_word_model` is explicitly turned **ON** (default is **OFF**).
  2. The word is **NOT** in the reject list or user blacklist (rejects always win).
  3. The word is **NOT** already recognized in the lexicon or via standard morphology.
- **Zero ML Runtime**: The inference engine is written in ~30 lines of pure Rust using integer arithmetic. No ONNX, Torch, or external ML runtime.
- **Footprint**: 16.02 KB total binary size.

---

## 2. Feature Extraction & Hashing
1. **Input Normalization**: `w = format!("^{}$", word.to_lowercase())`.
2. **N-gram Range**: Character n-grams for $n \in \{1, 2, 3, 4, 5\}$.
3. **Hashing**: Each n-gram byte sequence is hashed using the 32-bit FNV-1a hash:
   $$\text{index} = \text{fnv1a\_hash}(\text{ngram}) \pmod{16384}$$
4. **Active Features**: List of feature indices $f_1, f_2, \dots, f_k \in [0, 16383]$.

---

## 3. Binary File Layout (Little-Endian)

```
+-----------------------------------------------------------+
| Header (24 bytes)                                         |
|   - Magic: 8 bytes (b"YANGIMOD")                          |
|   - Version: u32 (1)                                      |
|   - Num Features: u32 (16384)                             |
|   - Quantized Threshold: i32 (348)                        |
|   - Quantized Bias: i32 (-3)                              |
+-----------------------------------------------------------+
| Weights Array (16,384 bytes)                              |
|   - 16,384 contiguous i8 values                           |
+-----------------------------------------------------------+
Total Size: 16,408 bytes (~16.02 KB)
```

### Header Specification
| Offset | Field | Type | Description |
| :--- | :--- | :--- | :--- |
| `0x00` | `magic` | `[u8; 8]` | Magic marker: `b"YANGIMOD"` (`0x59, 0x41, 0x4E, 0x47, 0x49, 0x4D, 0x4F, 0x44`) |
| `0x08` | `version` | `u32` | Format version (`1`) |
| `0x0C` | `num_features` | `u32` | Number of features (`16384`) |
| `0x10` | `threshold` | `i32` | Calibrated integer decision threshold |
| `0x14` | `bias` | `i32` | Quantized model bias |
| `0x18` | `weights` | `[i8; 16384]` | Contiguous signed 8-bit quantized weights |

---

## 4. Inference Algorithm (Rust)
```rust
let mut score: i32 = model.bias;
for n in 1..=5 {
    for window in chars.windows(n) {
        let index = fnv1a_hash(window) % model.num_features;
        score += model.weights[index] as i32;
    }
}
let is_uzbek = score >= model.threshold;
```
Execution time: **< 100 nanoseconds per word**.
