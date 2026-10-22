# YangiAlifbo Compact Binary Data Format (v1)

`data/FORMAT.md` specifies the binary format for the compact, read-only, memory-mappable dictionary file used by `yangi-core` and platform engines (`yangi_data.bin`).

---

## 1. Design Objectives
1. **Ultra-Low Memory (RAM)**: Memory-mappable (`mmap`) directly into the address space. Zero runtime heap allocation for dictionary structures.
2. **Instant Startup**: Near-zero load time (< 1 ms vs. baseline ~414 ms).
3. **Extreme Compactness**: Under 400 KB total binary size (compared to ~14.1 MB raw text, a ~35x reduction).
4. **Single Spelling Indexing**: Stores roots, exceptions, and reject words in a canonical spelling (1995 Uzbek Latin) with case normalization; 2024 orthography and casing variants are derived dynamically at runtime.
5. **Deterministic Cross-Platform Compatibility**: Byte-level consistency across x86_64, aarch64, Windows, macOS, Linux, Android, iOS, and WebAssembly (Wasm). Little-endian byte order.

---

## 2. File Layout & Header Structure

The binary file consists of a 64-byte aligned header followed by contiguous data sections:

```
+---------------------------------------------------------------+
|  Header (64 bytes)                                            |
|    - Magic: 8 bytes ("YANGIDAT")                              |
|    - Format Version: u32 (1)                                  |
|    - Flags / Reserved: u32                                    |
|    - Section 1 Offset (Roots FST): u64                        |
|    - Section 1 Length: u64                                    |
|    - Section 2 Offset (Exceptions FST): u64                   |
|    - Section 2 Length: u64                                    |
|    - Section 3 Offset (Reject List FST): u64                  |
|    - Section 3 Length: u64                                    |
|    - Reserved padding to 64 bytes                             |
+---------------------------------------------------------------+
|  Section 1: Roots FST (fst::Set)                              |
|    Automaton containing ~32,000 base stems                    |
+---------------------------------------------------------------+
|  Section 2: Exceptions FST (fst::Set)                         |
|    Automaton containing ~14,000 minimal inflection exceptions |
+---------------------------------------------------------------+
|  Section 3: Reject List FST (fst::Set)                        |
|    Automaton containing ~10,000 English/programming words     |
+---------------------------------------------------------------+
```

### Header Specification (64 bytes, Little Endian)
| Offset | Field | Type | Description |
| :--- | :--- | :--- | :--- |
| `0x00` | `magic` | `[u8; 8]` | Magic byte marker: `b"YANGIDAT"` (`0x59, 0x41, 0x4E, 0x47, 0x49, 0x44, 0x41, 0x54`) |
| `0x08` | `version` | `u32` | Format specification version (`1`) |
| `0x0C` | `flags` | `u32` | Reserved for feature flags (default `0`) |
| `0x10` | `roots_offset` | `u64` | Byte offset from file start to Roots FST |
| `0x18` | `roots_len` | `u64` | Byte length of Roots FST |
| `0x20` | `exceptions_offset` | `u64` | Byte offset from file start to Exceptions FST |
| `0x28` | `exceptions_len` | `u64` | Byte length of Exceptions FST |
| `0x30` | `reject_offset` | `u64` | Byte offset from file start to Reject List FST |
| `0x38` | `reject_len` | `u64` | Byte length of Reject List FST |
| `0x40` | `reserved` | `[u8; 8]` | Reserved for 64-bit alignment and future extensions |

---

## 3. Sections

### Section 1: Roots FST
- Format: Serialized `fst::Set` (BurntSushi finite state transducer).
- Contents: Sorted list of ~32,000 lowercase Uzbek root stems in 1995 Latin orthography.
- Key property: Supports `contains(&key)` in $O(\text{key\_len})$ time and prefix/automaton range queries.

### Section 2: Exceptions FST
- Format: Serialized `fst::Set`.
- Contents: Sorted list of ~14,000 minimal exceptional forms (inflected forms not captured by the greedy 3-level suffix stripper).
- Key property: Ensures 100% lexical parity with the legacy 948K dictionary.

### Section 3: Reject List FST
- Format: Serialized `fst::Set`.
- Contents: Sorted list of 10,057 English words, programming language keywords, and CLI tools that must never be converted (enforcing Hard Rule 4).

---

## 4. Building the Binary (`tools/builder` / `data/build.rs`)
The builder takes:
- `data/roots.txt` (sorted unique lowercase strings)
- `data/exceptions.txt` (sorted unique lowercase strings)
- `src/Core/Dictionaries/english_reject.txt` (sorted unique lowercase strings)

Constructs `fst::SetBuilder` for each, serializes them sequentially, and writes the 64-byte header.

---

## 5. Memory-Mapping & Consumption in `yangi-core`
In native platforms (Rust, C ABI, Windows, macOS, Linux, Android, iOS):
- Uses `memmap2::Mmap::map(&file)` or embeds bytes via `include_bytes!`.
- Creates `fst::Set::new(&bytes[offset..offset+len])` zero-copy.
- Zero heap allocations during lookup operations.
- Page faults only load the specific FST nodes touched during lookup.
