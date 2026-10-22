---
description: Resume the multiplatform mission from the handoff file
---
1. Read .agent/mission.md (the full mission), then .ai/HANDOFF.md, .ai/DESIGN-REQUESTS.md and .ai/REVIEW-QUEUE.md if they exist.
2. git fetch, check out `multiplatform` (create it from main if it does not exist), pull.
3. Pick the highest-priority unblocked workstream from "Next steps" in HANDOFF.md (or start with A and B if there is no handoff yet) and work on it autonomously, following the Hard rules and Operating mode in the mission.
4. Run /checkpoint after every milestone. Keep going to the next workstream without asking.
