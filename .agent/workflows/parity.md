---
description: Verify every engine against the shared test vectors
---
1. Regenerate or load the test vectors from tools/ (Workstream A).
2. Run the parity check for every engine built so far (C#, Rust, wasm, JNI, Swift).
3. Report agreement % per engine in .ai/HANDOFF.md. Any mismatch is a bug: fix it before calling that engine done.
