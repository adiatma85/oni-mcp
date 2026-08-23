---
id: TASK-6
title: Fix prefab token mapping for WashSink vs WashBasin in MCP virtual file parser
status: Done
assignee: []
created_date: '2026-08-22 17:31'
labels:
  - bug
  - mcp-server
  - localization
  - building-planner
dependencies: []
priority: high
ordinal: 6000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
### Problem Description
When editing virtual map files (`/active/map/viewport.md`), using Chinese building name `洗手池` (or similar terms) erroneously resolved to `WashBasin` (the manual tier-1 Wash Basin with no pipe ports) instead of `WashSink` (the plumbed tier-2 Sink with liquid intake/output ports).

### Root Cause
In the MCP building token regex dictionary, `洗手池` fuzzy-matched `WashBasin` before `WashSink`, or `WashSink` was missing an unambiguous token mapping.

### Fix
1. Update MCP building alias dictionary:
   - `洗手池` / `水槽` / `水池` / `Sink` / `WashSink` -> `WashSink` (Plumbed Sink).
   - `洗手盆` / `洗手台` / `WashBasin` -> `WashBasin` (Manual Basin).
2. Prioritized exact alias dictionary and exact proper name lookup before fuzzy substring search in `TryResolveBuildPrefabFromToken`.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Virtual file tokens for '洗手池' and 'WashSink' resolve strictly to prefabId: WashSink
- [x] #2 Virtual file tokens for '洗手盆' and 'WashBasin' resolve strictly to prefabId: WashBasin
<!-- AC:END -->
