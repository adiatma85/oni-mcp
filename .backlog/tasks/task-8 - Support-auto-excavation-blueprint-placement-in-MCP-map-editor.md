---
id: TASK-8
title: Support auto-excavation blueprint placement in MCP map editor (Solid to Building transition)
status: To Do
assignee: []
created_date: '2026-08-23 20:18'
labels:
  - mcp-server
  - ergonomics
  - world-editor
  - building
dependencies: []
priority: high
ordinal: 8000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
### Problem Context
In the vanilla Oxygen Not Included game UI, players can select **Tile** (or **Ladder**) and drag a line directly across natural solid terrain (`Sandstone`, `Dirt`, `Sand`, `Algae`). The game engine automatically places the Tile blueprint and generates an internal dig-and-build errand without requiring manual prior excavation.

Currently, in OniMcp's `/active/map/viewport.md` editor and virtual building parsers:
- Replacing solid element symbols (`子`, `沙`, `土`, `藻`) with building tokens (`砖`, `梯`, `门`) is rejected with:
  `"Unsupported map cell transitions. Use empty -> 建筑名:优先级 for build, or replace cells with 挖/拆/擦/扫/毒/杀/收/消/捕 plus optional :priority for orders."`
- This forces agents and players into a slow, multi-turn two-phase workflow:
  1. Manually mark `挖` on every solid cell.
  2. Wait for duplicants to finish digging out the void.
  3. Re-read the viewport map.
  4. Finally place `砖` (Tile) or building blueprints.

### Proposed Improvement
1. **Allow Solid-to-Building Transitions in Map Editor**:
   - Update `WorldEditorMapEditTokenParsing.cs` and `WorldEditorMapEditor.cs` to allow `SolidElement -> Building` transitions (e.g. `沙 -> 砖`, `子 -> 砖`, `土 -> 梯`).
   - When a solid tile is replaced with a building token, the server should place the building blueprint and automatically register the underlying cell excavation errand (mirroring vanilla game behavior).
2. **Accept `autoDig=true` on Building Blueprints**:
   - Enable `autoDig` when building footprints overlap natural solid tiles, allowing instantaneous room and floor layouts.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Map editor SEARCH/REPLACE in /active/map/viewport.md permits solid element tokens to be replaced by building tokens (e.g. 子 -> 砖, 沙 -> 梯)
- [ ] #2 Placing building blueprints over solid rock automatically issues the corresponding excavation orders
- [ ] #3 Eliminates the requirement to perform separate prior dig passes before placing floor tile and ladder blueprints
<!-- AC:END -->
