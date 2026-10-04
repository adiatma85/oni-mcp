---
id: TASK-4
title: Add conduit routing tool and plumbing network validation to OniMcp
status: Done
assignee: []
created_date: '2026-08-22 17:23'
labels:
  - bug
  - mcp-server
  - ergonomics
  - infrastructure
  - plumbing
dependencies: []
priority: high
ordinal: 4000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
### Problem Assessment from Plumbing Overlay
From the in-game Plumbing Overlay inspection:
1. **Disconnected Pipe Dots**: The liquid pipes at Y=204 appear as isolated circular nodes with no conduit connections between adjacent cells.
2. **Missing Port Connections**: Neither the Flush Toilet intake (white) / output (green) ports at Y=206/207 nor the Water Sieve intake/output at Y=203 are connected to any conduit lines. Red 'Unconnected Pipe' alerts are active.
3. **Missing Dual-Network Routing**: A functioning washroom requires two separate, non-colliding networks (Clean Water In vs. Polluted Water Out) plus an overflow priority bridge. Laying this via ASCII character grid replacement in `/active/infrastructure/liquid_conduits.md` is error-prone, hard to validate, and prone to broken conduit graph topologies.
4. **Prefab Ambiguity**: Manual `WashBasin` (non-plumbed) vs `WashSink` (plumbed) was misassigned.

### Is this an MCP Gap?
**Yes, this is a major MCP architecture & ergonomics gap.**
Managing graph-based networks (power, liquid, gas, logic, conveyors) tile-by-tile via flat text search-replace does not guarantee continuous conduit graph connectivity or automatic multi-layer intake/output network separation.

### Proposed Improvement
1. **Conduit Path Tool**: Added `building_control domain=planning action=route_conduit` / `route_pipe` / `auto_connect` accepting multi-point paths, `fromX`/`fromY`/`toX`/`toY` or `x1`/`y1`/`x2`/`y2`, `type=liquid|gas|wire|logic`, auto-digging obstructions, and native drag-placement.
2. **Plumbing Templates**: Added `washroom` and `farm` room templates with plumbing notes in `BuildPlanningRoomTemplateCatalog.cs`.
3. **Graph Connectivity Validator**: Added `read_control domain=infrastructure action=unconnected_ports` / `ports unconnectedOnly=true` in `InfrastructurePortReadTools.cs` returning unconnected port alerts, port roles, and nearest network cells with stub distances.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Conduit routing tool allows dragging continuous pipe/wire lines between start and end coordinates
- [x] #2 MCP provides clear diagnostic warnings when building ports are unconnected to valid networks
- [x] #3 Washroom closed-loop template automatically builds separate clean intake, polluted drain, and bridge overflow
<!-- AC:END -->
