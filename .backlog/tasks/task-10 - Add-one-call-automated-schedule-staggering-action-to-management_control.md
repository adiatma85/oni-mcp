---
id: TASK-10
title: Add one-call automated schedule staggering action to management_control
status: To Do
assignee: []
created_date: '2026-08-23 20:20'
labels:
  - mcp-server
  - ergonomics
  - management
  - schedule
dependencies: []
priority: medium
ordinal: 10000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
### Problem Context
Staggering duplicant schedules is one of the most critical early-game best practices in Oxygen Not Included (to prevent bathroom traffic jams, reduce toilet/washbasin count requirements, and balance oxygen/power usage).

Currently in OniMcp:
- `management_control domain=schedule` supports granular low-level actions (`create_schedule`, `set_block`, `assign_dupe`, `reorder`).
- However, creating a balanced 2-shift, 3-shift, or 4-shift staggered schedule requires an agent to:
  1. Calculate shifted hours for Downtime, Sleep, Bathtime, and Worktime across a 24-hour cycle.
  2. Issue multiple `set_block` commands per hour block.
  3. Manually map and assign individual duplicants.
- This creates unnecessary tool friction and roundtrips for a standard colony setup routine.

### Proposed Improvement
1. **Add `action=stagger_schedules` in `management_control domain=schedule`**:
   - Accepts parameters:
     - `shiftCount: 2 | 3 | 4` (default 2 for starting 3 dupes, 3 for 6 dupes, 4 for 8+ dupes).
     - `downtimeHours: 2` (default 2).
     - `sleepHours: 3` (default 3).
     - `autoAssign: true` (automatically distributes current duplicants evenly across the staggered shifts).
     - `respectNightOwls: true` (automatically places Night Owl dupes into night shifts and Early Birds into early morning shifts).
2. **Expose Quick Macro in `/active/management/schedule.md`**:
   - Add `# stagger_schedules shiftCount=2 autoAssign=true` to the editable commands template.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 management_control domain=schedule action=stagger_schedules creates balanced staggered schedules in 1 tool call
- [ ] #2 autoAssign distributes duplicants evenly across shifts and respects trait affinities (Night Owl, Early Bird)
- [ ] #3 Expose stagger_schedules macro command in /active/management/schedule.md
<!-- AC:END -->
