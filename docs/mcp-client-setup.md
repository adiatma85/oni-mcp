# MCP Client Setup

How to connect any MCP client to a running ONI MCP Server, and how to make duplicants act.

Everything below was verified against a live colony. Where a detail is likely to trip up a
naive client, it is called out rather than left implicit.

## 1. Start the server

The server is the mod. It only listens while Oxygen Not Included is running with **ONI MCP
Server** enabled in the in-game Mods menu.

```bash
onim dev -m OniMcp     # build and install to the game's Dev folder
# then launch ONI via Steam, enable the mod, restart when prompted
```

ONI loads mod DLLs at startup, so **every rebuild needs a game restart**. There is no hot
reload. Confirm the server is up before pointing a client at it:

```bash
lsof -nP -iTCP:8788 -sTCP:LISTEN     # macOS / Linux
```

Nothing listening means the mod is installed but not enabled, or the game has not been
restarted since it was installed.

## 2. Client configuration

Endpoint: `http://localhost:8788/mcp/` · protocol version `2025-11-25` · auth **disabled by
default** (`AuthEnabled = false` in `OniMcpConfig.json`).

Claude Code and most MCP CLIs read a `.mcp.json` in the project root:

```json
{
  "mcpServers": {
    "oni": {
      "type": "http",
      "url": "http://localhost:8788/mcp/",
      "headers": {}
    }
  }
}
```

The `mcpServers` wrapper is required. A bare `{"oni": {...}}` object is silently ignored —
the server never appears and there is no error to tell you why.

Claude Desktop takes the same object in its own config file. Port is configurable in the
mod's options screen; the default is `8788`.

If you enable authentication in the mod options, send the token as `Authorization: Bearer
<token>` or `X-Oni-Mcp-Token: <token>`. Both headers are accepted.

## 3. Four things that break naive clients

**`task` is required on every single tool call.** Not optional, not only for writes.

```json
{"name": "server_control",
 "arguments": {"task": "check colony state", "domain": "diagnostics", "action": "status"}}
```

Omitting it returns `task is required: describe what you are doing before every tool call.`
The string is displayed to the player as a speech bubble near the cursor, so it is part of
the safety design rather than bookkeeping. Most clients will not supply it automatically;
the agent must pass it explicitly.

**Session headers on every non-`initialize` request.** `initialize` returns an
`Mcp-Session-Id` header; send it back along with `Mcp-Protocol-Version: 2025-11-25` on
everything afterwards.

**Only 7 tools are public.** `tools/list` returns `benchmark`, `building_control`,
`game_control`, `navigation_control`, `orders_control`, `server_control`, `world_editor`.

`colony_control`, `read_control`, `dupes_control`, `search_control` and `coordinate_control`
are **internal**. Calling them through `tools/call` returns `Tool not found` by design.
Reach them through `oni://` resources or through `world_editor` operation files (§5).

**Dangerous actions need `confirm: true`.** Pass `dryRun: true` to preview without acting.
Most write paths default to a dry run when neither is given.

## 4. Reading state

Resources are the read surface. A few worth knowing:

| URI | Contents |
|-----|----------|
| `oni://colony/status` | cycle, duplicant count, speed, paused |
| `oni://colony/advice` | ranked recommendations joining live state to the strategy corpus |
| `oni://colony/diagnostics` | oxygen, food, stress, alerts |
| `oni://dupes/status-check` | positions, errands, needs, suspected trapped state |
| `oni://resources/inventory`, `oni://resources/food` | stock and spoilage |
| `oni://power/summary`, `oni://power/ports` | grid load, battery charge, wiring |
| `oni://world/text-map` | text map of the asteroid |
| `oni://strategy/query{?query,category,dlc,detail,limit}` | strategy corpus, EN + 中文 |
| `oni://tools/manifest` | full runtime tool list, authoritative over this document |

Corpus entries separate **authored judgement** (`body`, `formulas`, `cautions`) from
**derived fact** (`derive`). Anything listed under `derive` should be read live from the game
rather than trusted from the entry, because it changes between patches and between DLCs.

## 5. Making duplicants act

Most duplicant activity comes from **orders**, not direct commands. You mark work; duplicants
pick it up as errands according to priority, schedule and reachability. Direct `move_to` is
for repositioning one specific duplicant.

Because `orders_control` and `dupes_control` reject raw coordinates through the public tool
gate, exact spatial work goes through `world_editor` operation files:

| File | Backing tool | Use for |
|------|--------------|---------|
| `/active/ops/orders.md` | `orders_control` | dig, sweep, mop, disinfect, harvest, capture, deconstruct, cancel |
| `/active/ops/dupes.md` | `dupes_control` | move a duplicant, priorities, skills, rename |
| `/active/ops/build.md` | `building_control` | building placement |
| `/active/ops/any.md` | any | generic, requires `tool=<name>` per line |

### Workflow

1. `world_editor command=read path=/active/ops/orders.md` — read it first; the file lists the
   semantic shortcuts and current examples.
2. Submit a SEARCH/REPLACE edit containing **exactly one** executable command. An empty
   SEARCH block runs a fresh command.
3. Read the result, then re-read state to verify.

The one-command-per-edit limit is deliberate. Batch several and the edit is rejected rather
than partially applied.

### Order syntax

Verbs accept Chinese or English. A `:N` suffix sets priority.

```text
挖 土@(83,146):7 dryRun=true          # dig, priority 7, preview only
扫 @(90,140):6 confirm=true            # sweep debris
擦 @(90,140):6 confirm=true            # mop liquid
毒 @(90,140):6 confirm=true            # disinfect
收 植物@(100,137):6                    # harvest
拆 建筑@(92,145):7 dryRun=true         # deconstruct
捕 小动物@(101,130):7                  # capture a critter
消 @(93,146)                           # cancel a designation
扫 x1=90 y1=140 x2=94 y2=142 priority=6 dryRun=true
擦 areaId=base_floor priority=6 dryRun=true
```

### Moving a duplicant

```text
移 人@Ada -> target confirm=true
```

Two constraints the file states outright: **critters cannot be moved** with duplicant
commands — use `捕` capture in `orders.md`; and **items cannot either** — use `扫` sweep or
the storage tools.

### Build sets

Whole rooms are one call, no coordinates needed:

```json
{"name": "building_control",
 "arguments": {"task": "build starter rooms", "domain": "planning",
               "action": "room_template", "kind": "starter",
               "execute": true, "dryRun": true, "autoLayout": true}}
```

`kind` accepts `toilet`, `lab`, `starter` and `spom`. The response carries a `techGate` block
listing anything not yet researched, so a template that cannot fully build says so up front
instead of silently placing half of itself.

## 6. Safety

Worth setting up before pointing an autonomous agent at a colony you care about.

- **Keep the game paused** while the agent reads and plans. Resume in short bursts to let
  work happen, then pause again to verify.
- **Dry-run liquid-adjacent digs.** A mistaken dig next to liquid is one of the few genuinely
  unrecoverable actions in the game.
- **Do not auto-select duplicants from the Printing Pod.** Each one is a permanent recurring
  food and oxygen cost; prefer care packages unless population is the actual bottleneck.
- **Avoid sandbox and debug spawning.** It invalidates everything the advisor reports.
- Test on a **throwaway save** first. Every example in this document was verified with
  `dryRun: true` against a live colony without mutating it.

See `AGENTS.md` for the full operating rules used when driving this mod autonomously.

## 7. Troubleshooting

| Symptom | Cause |
|---------|-------|
| Nothing on port 8788 | Mod not enabled in the Mods menu, or game not restarted since install |
| Server absent from the client entirely | `.mcp.json` missing the `mcpServers` wrapper, or wrong port |
| `task is required: ...` | Add `task` to the tool call arguments |
| `Tool not found: colony_control` | Internal tool; use an `oni://` resource or an ops file |
| `Resource not found: oni://...` | Check `oni://tools/manifest` for the live URI list |
| Changes to the mod have no effect | Rebuild with `onim dev`, then restart the game |
