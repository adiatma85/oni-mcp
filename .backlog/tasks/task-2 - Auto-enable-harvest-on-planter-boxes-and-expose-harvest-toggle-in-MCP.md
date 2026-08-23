---
id: TASK-2
title: Auto-enable harvest on planter boxes and expose harvest toggle in MCP
status: To Do
assignee: []
created_date: '2026-08-22 17:05'
labels:
  - farming
  - mcp-server
  - ergonomics
dependencies: []
priority: low
ordinal: 2000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
### Problem Context
Some Planter Boxes or farm plants display the status/warning 'No Harvest Pending' when Auto-Harvest is disabled or when plants are designated without an active auto-harvest policy.

### Is this considered a bug?
- **In Vanilla Game Mechanics**: No. 'No Harvest Pending' is standard vanilla behavior when a plant has its Auto-Harvest toggle disabled (preventing duplicants from harvesting it automatically so it drops seeds/yield naturally).
- **In ONI MCP Tooling**: It is an ergonomics/automation gap. When an agent or player constructs farm rooms, all food crop planter boxes should default to Auto-Harvest enabled (`harvest=true`), and the status should be easily inspectable/toggleable via MCP tools.

### Proposed Improvement
1. Ensure farm planning actions automatically set `autoHarvest=true` on food crops.
2. Expose `orders_control domain=farming action=toggle_autoharvest` to easily toggle harvest policies across rectangular areas.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Farm planning blueprints default to auto-harvest enabled
- [ ] #2 MCP provides clear diagnostic info distinguishing 'Growing', 'Harvest Pending', and 'Auto-Harvest Disabled'
<!-- AC:END -->
