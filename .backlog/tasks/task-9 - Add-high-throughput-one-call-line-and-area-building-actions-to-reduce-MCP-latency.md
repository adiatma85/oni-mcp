---
id: TASK-9
title: Add high-throughput one-call line and area building actions to reduce MCP construction latency
status: To Do
assignee: []
created_date: '2026-08-23 20:19'
labels:
  - mcp-server
  - ergonomics
  - performance
  - building
dependencies:
  - TASK-8
priority: high
ordinal: 9000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
### Problem Context
Currently, placing a row of tiles, a ladder shaft, or an entire room through MCP involves significant turn latency:
1. Direct coordinate calls to `building_control` (`build_area` with anchors or points) are restricted with `"Direct building_control build_area planning is forbidden"`.
2. Map edits on `/active/map/viewport.md` require single-line SEARCH/REPLACE diff blocks which require reading the full viewport text, constructing regexes/exact lines, and executing sequentially.
3. If errors occur due to minor whitespace or formatting mismatches, the agent must re-read, re-parse, and re-try across multiple roundtrips, causing notable delay for the user.

### Proposed Improvement
1. **Provide Fast Construction Macros in `building_control`**:
   - Expose concise, dedicated actions:
     - `action=build_line prefabId=Tile|Ladder fromX=... fromY=... toX=... toY=... material=auto priority=7 autoDig=true`
     - `action=build_rect prefabId=Tile x1=... y1=... x2=... y2=... hollow=true|false`
2. **Batch Operation Gateway**:
   - Streamline `server_control domain=batch action=call_many` to allow bundling room shells, doors, and furniture in one atomic roundtrip with sub-second execution time.
3. **Latency Benchmarking**:
   - Add a test in `benchmark` tool to verify that a standard 12-tile floor line completes in < 50ms without multi-turn roundtrips.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Expose build_line and build_rect actions in building_control with autoDig=true support
- [ ] #2 Allow single-call placement of floor rows and vertical ladder shafts without editing ASCII map viewports
- [ ] #3 Reduces floor/room construction latency from 3-4 LLM roundtrips down to a single instantaneous call
<!-- AC:END -->
