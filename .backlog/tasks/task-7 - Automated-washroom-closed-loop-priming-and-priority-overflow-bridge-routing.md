---
id: TASK-7
title: Automated washroom closed-loop priming and priority overflow bridge routing
status: To Do
assignee: []
created_date: '2026-08-23 12:12'
labels:
  - mcp-server
  - ergonomics
  - infrastructure
  - plumbing
  - washroom
dependencies:
  - TASK-4
priority: high
ordinal: 7000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
### Problem Context
When a plumbed washroom (consisting of `FlushToilet`, `WashSink`, and `WaterPurifier`/Water Sieve) is constructed via OniMcp (using room templates or building placement), the toilets and sinks display the warnings:
- `• Empty Pipe`
- `⬇ No Liquid Intake • Water`

### Why this is a Tooling & Autonomous Gameplay Gap
1. **The Chicken-and-Egg Loop Deadlock**:
   - A closed-loop washroom is self-sustaining only *after* it is pressurized with clean water.
   - Newly built Flush Toilets and Sinks start with empty internal buffers (5 kg/use).
   - The Water Sieve cannot produce clean water because no polluted water has been flushed into it yet.
   - Duplicants cannot use the toilets/sinks because there is no clean water intake.
2. **Missing Automated Priming / Water Intake Bootstrap in MCP**:
   - Currently, an autonomous agent or player must manually locate a clean water pool, build a `LiquidPump`, route temporary pipes into the loop until all buffers fill, and then cut the line.
   - MCP has no one-call action (e.g. `prime_loop`, `auto_prime_from_source`, or temporary pitcher pump/bottle emptier delivery order) to automatically bootstrap liquid into newly laid closed circuits.
3. **Net-Positive Water Overflow Deadlock**:
   - Flush Toilets generate **+6.7 kg net excess Polluted Water per flush** (consuming 5 kg clean water, producing 11.7 kg polluted water).
   - Without an automated **Liquid Conduit Bridge (Priority Overflow)** where the bridge input takes priority and the continuation line diverts excess water to a reservoir or Thimble Reed hydroponic tile, the closed loop will inevitably back up, halting all toilets and causing bladder accidents.

### Proposed Improvement
1. **Washroom Template Enhancement with Priority Bridge**:
   - Update `building_control domain=planning action=room_template kind=washroom` to automatically place a `LiquidConduitBridge` configured for loop priority and overflow diversion.
2. **Automated Loop Priming Tool (`prime_loop` / `bootstrap_utility`)**:
   - Add an action in `building_control domain=planning action=prime_loop` or `orders_control domain=plumbing action=prime`:
     - Searches the active world for the nearest accessible clean water body (`Water` / `CleanWater`).
     - Plans a temporary `LiquidPump` connection or places a temporary `BottleEmptier` / `PitcherPump` errand into the washroom supply line.
     - Automatically pauses or signals when the closed loop reaches operating pressure (e.g. 50 kg total in circulation).
3. **Diagnostics & Status Integration**:
   - Report `loopStatus: "unprimed" | "pressurized" | "backed_up"` in `/active/infrastructure/liquid_conduits.md` and `unconnected_ports` read actions.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Washroom room template automatically includes priority overflow LiquidConduitBridge placement
- [ ] #2 MCP provides a loop priming action to bootstrap water into closed circuits from nearby reservoirs
- [ ] #3 Infrastructure diagnostics identify unprimed closed loops and guide agent on required priming mass
<!-- AC:END -->
