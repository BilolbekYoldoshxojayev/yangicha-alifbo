---
description: Check and fix CI
---
1. gh run list --branch multiplatform --limit 10
2. For each failed run: gh run view <id> --log-failed, fix the cause, push.
3. Repeat until green or blocked (max ~3 attempts per failure); record run URLs in .ai/HANDOFF.md.
