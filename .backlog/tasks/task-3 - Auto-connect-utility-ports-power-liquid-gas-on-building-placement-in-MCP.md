---
id: TASK-3
title: 'Auto-connect utility ports (power, liquid, gas) on building placement in MCP'
status: Done
assignee: []
created_date: '2026-08-22 17:16'
labels:
  - mcp-server
  - ergonomics
  - infrastructure
  - utilities
dependencies: []
priority: medium
ordinal: 3000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
### Problem Context
When buildings with utility requirements (e.g. Water Sieve consuming 120W power, Flush Toilets needing liquid pipes) are placed via `world_editor` or `building_control`, the MCP server places only the building footprint. It does not automatically hook up or suggest connection stubs to adjacent live utility networks (such as a power line running 1 tile below the floor).

### Why this is an MCP Tooling Gap
- Finding exact port cells currently requires inspecting individual `/active/map/cell_X_Y.md` files or reading infrastructure layers.
- An autonomous agent or player using MCP must manually issue separate edits to `/active/infrastructure/power.md`, `/active/infrastructure/liquid_conduits.md`, and `/active/infrastructure/gas_conduits.md` just to link a 1-tile port stub to a passing main line.

### Proposed Improvement
1. Add `building_control domain=planning action=auto_connect` (and aliases `route_conduit`, `route_pipe`, `route_wire`) supporting power wires, liquid conduits, and gas conduits with multi-point paths, coordinate aliases (`x1`/`y1`/`x2`/`y2`), and auto-nearest conduit searching.
2. In `read_control domain=infrastructure action=unconnected_ports` / `ports unconnectedOnly=true`, highlight unconnected ports and provide exact suggested stub network cells and distances.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 MCP provides a tool/flag to automatically connect building utility ports to adjacent networks
- [x] #2 Building placement previews highlight unconnected ports and suggested stub coordinates
<!-- AC:END -->
