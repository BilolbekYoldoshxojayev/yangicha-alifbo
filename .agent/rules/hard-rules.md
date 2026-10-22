---
trigger: always_on
---
# YangiAlifbo hard rules (always apply)
1. Work only on branch `multiplatform` or its sub-branches. NEVER push to main, NEVER create tags or GitHub releases, NEVER publish to any store, NEVER sign with real certificates. Every installed copy of the app auto-updates from the latest GitHub release.
2. Never fabricate results. Every number, "it works" or "tests pass" must come from a command or CI run you executed; record the command or run URL next to it. Untested means say "untested".
3. The shared test vectors are the source of truth; every engine on every platform must pass them before it is called done.
4. Converting an English word is worse than missing an Uzbek word. Never accept a change that raises English false positives on the eval set.
5. Do not touch the untracked scripts in the repo root (add_vocab.py, bump_version.py, patch_*.py, update_suffixes.py). Do not change the shipped Windows app (src/) except in Workstream H on branch `windows-1.6.1`.
6. Privacy: no build may send, log or store typed text. Only the Windows updater uses the network.
7. Password/secure fields must never be converted on any platform.
8. Keep .ai/HANDOFF.md current; log UI decisions in .ai/DESIGN-REQUESTS.md and security-sensitive items in .ai/REVIEW-QUEUE.md.
