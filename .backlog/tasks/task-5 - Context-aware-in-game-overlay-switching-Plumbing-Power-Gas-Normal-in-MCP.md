---
id: TASK-5
title: 'Context-aware in-game overlay switching (Plumbing, Power, Gas, Normal) in MCP'
status: To Do
assignee: []
created_date: '2026-08-22 17:25'
labels:
  - mcp-server
  - ergonomics
  - camera
  - livestream
  - ux
dependencies: []
priority: medium
ordinal: 5000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
### Problem Context
The player/viewer observed that the in-game camera frequently stays on the Temperature overlay (`F1`) even when the agent is performing plumbing jobs, wiring power grids, or managing farm crops.

### Why this is an MCP Gap
1. **Lack of In-Game Overlay Synchronization**: When an agent reads or edits `/active/infrastructure/liquid_conduits.md` or `/active/infrastructure/power.md`, the MCP server queries internal data structures but does not automatically switch the player's active in-game overlay to match the task (e.g. switching to Plumbing Overlay `F6` for pipe work, Electric Overlay `F2` for wire work, or Normal/Default view `F1` toggle off for regular play).
2. **Livestream & Pair-Programming UX**: In live pair-programming or streaming mode, viewers and players see a mismatched overlay (e.g. Temperature overlay while the agent is placing pipes), creating confusion.

### Proposed Improvement
1. Add automatic context-aware in-game overlay switching in MCP:
   - When editing/zooming liquid conduits -> Switch in-game overlay to `LiquidConduits` (`F6`).
   - When editing/zooming power grids -> Switch in-game overlay to `Power` (`F2`).
   - When editing/zooming gas conduits -> Switch in-game overlay to `GasConduits` (`F7`).
   - When returning to general gameplay / orders -> Revert overlay to `None` / `Default` view.
2. Expose explicit overlay control via `navigation_control domain=camera action=set_overlay overlay="liquid|power|gas|temperature|none"`.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 MCP camera/overlay tool supports setting in-game overlay mode (liquid, power, gas, normal, etc.)
- [ ] #2 Infrastructure tool actions automatically sync the corresponding in-game overlay during active editing
- [ ] #3 Game reverts to Default/Normal overlay when regular simulation resumes
<!-- AC:END -->
