---
id: TASK-1
title: Support programmatic seed assignment for Planter Boxes and Farm Tiles
status: Done
assignee: []
created_date: '2026-08-22 16:59'
labels:
  - bug
  - mcp-server
  - ergonomics
  - farming
dependencies: []
priority: medium
ordinal: 1000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
### Problem
When Planter Boxes or Farm Tiles are constructed via OniMcp (using world_editor or building_control), they default to an unconfigured state in Oxygen Not Included and display the warning 'No Seed Selected'.

### Is this considered a bug?
- **In Vanilla Game Mechanics**: No, vanilla ONI requires the player to designate a crop for every newly built receptacle.
- **In ONI MCP Automation / Tooling**: Yes, this is an agent ergonomics / tooling limitation (DX bug). The agent currently cannot programmatically assign seeds to newly built planter boxes/farm tiles without requiring manual player Copy Settings in the UI.

### Proposed Fix
1. Expose a seed/plant configuration endpoint in `building_control domain=receptacle`, `colony_control domain=bio bioDomain=farming action=set`, or `building_control domain=config`.
2. Support editable `Plant.Seed: <Crop/SeedID>` in virtual building instance files (e.g. `/active/buildings/instances/PlanterBox-<id>.md`).
3. Support friendly crop names (e.g. `Mealwood`, `BristleBlossom`, `Mushroom`, `米虱木`) automatically resolving to proper `PlantableSeed` tags.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Agent can programmatically select crop/seed on PlanterBox and FarmTile instances
- [x] #2 Seed delivery chores are automatically created for duplicants upon selection
<!-- AC:END -->
