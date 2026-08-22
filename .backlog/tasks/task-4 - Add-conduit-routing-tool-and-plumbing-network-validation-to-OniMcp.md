---
id: TASK-4
title: Add conduit routing tool and plumbing network validation to OniMcp
status: To Do
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
1. **Conduit Path Tool**: Add `orders_control domain=plumbing action=route_pipe path=[(x1,y1), (x2,y2), ...]` or `build_conduit from=(x1,y1) to=(x2,y2) conduitType=liquid material=Sandstone`.
2. **Plumbing Template / Room Macro**: Add automated plumbing templates (e.g. `washroom_loop`, `spom_plumbing`) in `building_control domain=planning`.
3. **Graph Connectivity Validator**: Expose conduit network diagnostics in `/active/infrastructure/liquid_conduits.md` (e.g. `connected: false`, `graph_id: null`, `endpoints: [unconnected_input, unconnected_output]`).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Conduit routing tool allows dragging continuous pipe/wire lines between start and end coordinates
- [ ] #2 MCP provides clear diagnostic warnings when building ports are unconnected to valid networks
- [ ] #3 Washroom closed-loop template automatically builds separate clean intake, polluted drain, and bridge overflow
<!-- AC:END -->
