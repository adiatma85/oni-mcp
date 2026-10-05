using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OniMcp.Core;
using OniMcp.Support;

namespace OniMcp.Tools
{
    public static partial class BuildPlanningTools
    {
        public static McpTool RoomTemplatePlan()
        {
            return new McpTool
            {
                Name = "build_room_template",
                Group = "buildings",
                Mode = "execute",
                Risk = "dangerous",
                Hidden = true,
                Description = "Compatibility entrypoint: use building_control domain=planning action=room_template. kind=spom builds a self-powered oxygen module chamber. kind=starter/toilet_lab is the one-call starter setup: dig interiors and build room shells, doors, outhouse, wash basin, and research station. execute=true confirm=true runs the full plan.",
                Parameters = new Dictionary<string, McpToolParameter>
                {
                    ["kind"] = new McpToolParameter { Type = "string", Description = "Template kind: toilet/restroom/lab/research/starter/toilet_lab/spom.", Required = false },
                    ["template"] = new McpToolParameter { Type = "string", Description = "Alias for kind.", Required = false },
                    ["plan"] = new McpToolParameter { Type = "string", Description = "Natural template phrase, e.g. 完整厕所, 实验室, 厕所加实验室, 制氧模块, SPOM.", Required = false },
                    ["areaId"] = new McpToolParameter { Type = "string", Description = "Preferred room candidate area handle.", Required = false },
                    ["query"] = new McpToolParameter { Type = "string", Description = "Search anchor when areaId/x/y are omitted, e.g. printing pod or oxygen pocket.", Required = false },
                    ["target"] = new McpToolParameter { Type = "string", Description = "Alias for query.", Required = false },
                    ["search"] = new McpToolParameter { Type = "string", Description = "Alias for query.", Required = false },
                    ["x"] = new McpToolParameter { Type = "integer", Description = "Room lower-left X. Prefer areaId when available.", Required = false },
                    ["y"] = new McpToolParameter { Type = "integer", Description = "Room lower-left Y. Prefer areaId when available.", Required = false },
                    ["x1"] = new McpToolParameter { Type = "integer", Description = "Room candidate rect lower X.", Required = false },
                    ["y1"] = new McpToolParameter { Type = "integer", Description = "Room candidate rect lower Y.", Required = false },
                    ["x2"] = new McpToolParameter { Type = "integer", Description = "Room candidate rect upper X.", Required = false },
                    ["y2"] = new McpToolParameter { Type = "integer", Description = "Room candidate rect upper Y.", Required = false },
                    ["width"] = new McpToolParameter { Type = "integer", Description = "Single room width. Default 8; starter defaults to two rooms.", Required = false },
                    ["height"] = new McpToolParameter { Type = "integer", Description = "Room height. Default 4; minimum 4.", Required = false },
                    ["material"] = new McpToolParameter { Type = "string", Description = "Build material. Default auto.", Required = false },
                    ["priority"] = new McpToolParameter { Type = "integer", Description = "Dig/build priority 1..9. Default 7.", Required = false },
                    ["topPriority"] = new McpToolParameter { Type = "boolean", Description = "Mark generated work top priority when supported.", Required = false },
                    ["worldId"] = new McpToolParameter { Type = "integer", Description = "Target world id. Defaults active world or area world.", Required = false },
                    ["execute"] = new McpToolParameter { Type = "boolean", Description = "When true, execute generated dig/build calls in this one tool call.", Required = false },
                    ["confirm"] = new McpToolParameter { Type = "boolean", Description = "Required with execute=true unless dryRun=true.", Required = false },
                    ["autoLayout"] = new McpToolParameter { Type = "boolean", Description = "When true and no area/x/query is supplied, select the best layout candidate automatically. Starter templates auto-layout by default.", Required = false },
                    ["dryRun"] = new McpToolParameter { Type = "boolean", Description = "Preview without writing orders/blueprints.", Required = false }
                },
                Handler = HandleRoomTemplate
            };
        }

        private static CallToolResult HandleRoomTemplate(JObject args)
        {
            args = args ?? new JObject();
            string kind = ResolveRoomTemplateKind(args);
            if (string.IsNullOrEmpty(kind))
                return CallToolResult.Error("kind/template/plan must mention toilet/restroom/lab/research/starter/toilet_lab/spom.");

            string anchorError;
            RoomTemplateAnchor anchor = ResolveRoomTemplateAnchor(args, kind, out anchorError);
            if (anchor == null)
                return CallToolResult.Error(anchorError);

            string material = string.IsNullOrWhiteSpace(args["material"]?.ToString()) ? "auto" : args["material"].ToString();
            int priority = Math.Max(1, Math.Min(ToolUtil.GetInt(args, "priority") ?? 7, 9));
            bool topPriority = ToolUtil.GetBool(args, "topPriority", false);
            bool execute = ToolUtil.GetBool(args, "execute", false);
            bool dryRun = ToolUtil.GetBool(args, "dryRun", false);
            if (execute && !dryRun && !ToolUtil.GetBool(args, "confirm", false))
                return CallToolResult.Error("confirm=true required with execute=true dryRun=false.");

            List<RoomTemplateCall> calls = BuildRoomTemplateCalls(kind, anchor, material, priority, topPriority, execute, dryRun);
            RoomTemplateDefinition definition = GetRoomTemplateDefinition(kind);
            List<string> locked = LockedTemplatePrefabs(definition);
            var response = new JObject
            {
                ["ok"] = true,
                ["template"] = kind,
                ["anchor"] = new JObject { ["x"] = anchor.X, ["y"] = anchor.Y },
                ["areaId"] = anchor.AreaId,
                ["size"] = new JObject { ["width"] = anchor.Width, ["height"] = anchor.Height },
                ["rooms"] = BuildRoomTemplateRoomSummary(kind, anchor),
                ["techGate"] = new JObject
                {
                    ["buildable"] = locked.Count == 0,
                    ["lockedPrefabs"] = new JArray(locked.ToArray()),
                    ["note"] = locked.Count == 0
                        ? (JToken)"All buildings in this template are researched."
                        : "Not researched yet: " + string.Join(", ", locked.ToArray())
                          + ". The room, shell and any unlocked machines will still be placed; these will fail until the tech is researched."
                },
                ["note"] = definition != null && !string.IsNullOrEmpty(definition.Note)
                    ? (JToken)definition.Note
                    : null,
                ["priorityAction"] = new JObject { ["priority"] = priority, ["topPriority"] = topPriority },
                ["executionPlan"] = BuildRoomTemplateExecutionPlan(kind, anchor, priority),
                ["verificationPlan"] = BuildRoomTemplateVerificationPlan(kind, anchor, priority),
                ["tokenHint"] = "For one-call starter setup call kind=starter execute=true confirm=true; if no anchor is supplied the tool auto-selects a layout. Read rooms, results[].summary/diagnostic, then nextActions.",
                ["nextActions"] = BuildRoomTemplateNextActions(kind, anchor, priority),
                ["calls"] = new JArray(calls.Select(c => c.Call))
            };

            response[execute ? "results" : "next"] = execute
                ? (JToken)ExecuteRoomTemplateCalls(args, calls)
                : "Re-run with execute=true confirm=true, or send calls through server_control batch.";
            return CallToolResult.Text(JsonConvert.SerializeObject(response, McpJsonUtil.Settings));
        }

        private static RoomTemplateAnchor ResolveRoomTemplateAnchor(JObject args, string kind, out string error)
        {
            error = null;
            int worldId = ToolUtil.ResolveWorldId(args);
            int x;
            int y;

            if (TryGetInt(args, "x", out x) && TryGetInt(args, "y", out y))
                return BuildRoomTemplateAnchor(args, kind, x, y, worldId, null);

            if (HasRoomRect(args))
            {
                Dictionary<string, int> rect = WorldEditor.ResolveRect(args);
                return BuildRoomTemplateAnchor(args, kind, rect["x1"], rect["y1"], worldId, rect);
            }

            if (ToolUtil.TryResolveSearchCell(args, out x, out y, out error))
                return BuildRoomTemplateAnchor(args, kind, x, y, worldId, null);

            RoomTemplateAnchor autoAnchor = TryAutoRoomTemplateAnchor(args, kind, worldId, out error);
            if (autoAnchor != null)
                return autoAnchor;

            error = "room_template needs areaId, x/y, x1/y1/x2/y2, query/target/search anchor, or kind=starter auto layout. For one-call starter setup use kind=starter execute=true confirm=true.";
            return null;
        }

        private static RoomTemplateAnchor BuildRoomTemplateAnchor(JObject args, string kind, int x, int y, int worldId, Dictionary<string, int> rect)
        {
            int inferredWidth = rect == null ? DefaultRoomTemplateWidth(kind) : rect["x2"] - rect["x1"] + 1;
            int inferredHeight = rect == null ? DefaultRoomTemplateHeight(kind) : rect["y2"] - rect["y1"] + 1;
            return new RoomTemplateAnchor
            {
                X = x,
                Y = y,
                Width = Math.Max(DefaultRoomTemplateWidth(kind), ToolUtil.GetInt(args, "width") ?? inferredWidth),
                // Height is a floor, not a default: a template whose machines do not fit in four
                // rows (a SPOM needs nine) must not be silently squashed into an unbuildable room.
                Height = Math.Max(DefaultRoomTemplateHeight(kind), ToolUtil.GetInt(args, "height") ?? inferredHeight),
                WorldId = worldId,
                AreaId = args["areaId"]?.ToString()
            };
        }

        private static bool HasRoomRect(JObject args)
        {
            if (!string.IsNullOrWhiteSpace(args["areaId"]?.ToString()))
                return true;
            return args["x1"] != null && args["y1"] != null;
        }

        private static List<RoomTemplateCall> BuildRoomTemplateCalls(string kind, RoomTemplateAnchor anchor, string material, int priority, bool topPriority, bool execute, bool dryRun)
        {
            if (kind == "starter")
                return BuildStarterTemplateCalls(anchor, material, priority, topPriority, execute, dryRun);

            return BuildSingleRoomCalls(kind, anchor, material, priority, topPriority, execute, dryRun);
        }

        private static List<RoomTemplateCall> BuildStarterTemplateCalls(RoomTemplateAnchor anchor, string material, int priority, bool topPriority, bool execute, bool dryRun)
        {
            int roomWidth = Math.Max(7, (anchor.Width - 1) / 2);
            var toilet = anchor.With(anchor.X, anchor.Y, roomWidth, anchor.Height);
            var lab = anchor.With(anchor.X + roomWidth + 1, anchor.Y, roomWidth, anchor.Height);
            var calls = new List<RoomTemplateCall>();
            calls.AddRange(BuildSingleRoomCalls("toilet", toilet, material, priority, topPriority, execute, dryRun, true, false, true));
            calls.AddRange(BuildSingleRoomCalls("lab", lab, material, priority, topPriority, execute, dryRun, false, true));
 calls.Add(BuildCall("Tile", material, VerticalWallAnchors(anchor.X + roomWidth, anchor.Y, anchor.Height), priority, topPriority, anchor.WorldId, execute, dryRun));
            return calls;
        }

        private static List<RoomTemplateCall> BuildSingleRoomCalls(string kind, RoomTemplateAnchor anchor, string material, int priority, bool topPriority, bool execute, bool dryRun, bool doorOnLeft = false, bool omitLeftWall = false, bool omitRightWall = false)
        {
            int doorX = doorOnLeft ? anchor.X : anchor.X + anchor.Width - 1;
            var calls = new List<RoomTemplateCall>
            {
                OrdersCall("dig", anchor.X + 1, anchor.Y + 1, anchor.X + anchor.Width - 2, anchor.Y + anchor.Height - 2, priority, topPriority, anchor.WorldId, execute, dryRun),
                BuildCall("Tile", material, RoomShellAnchors(anchor, doorOnLeft, omitLeftWall, omitRightWall), priority, topPriority, anchor.WorldId, execute, dryRun),
                BuildCall("Door", material, OneAnchor(doorX, anchor.Y + 1), priority, topPriority, anchor.WorldId, execute, dryRun)
            };

            // Contents come from the catalog. An unknown kind yields no fixtures rather than
            // silently falling through to a research station, which is what the previous
            // if/else did.
            RoomTemplateDefinition definition = GetRoomTemplateDefinition(kind);
            if (definition != null)
            {
                foreach (RoomTemplatePlacement placement in definition.Placements)
                {
                    int px, py;
                    ResolvePlacementCell(anchor, placement, out px, out py);
                    calls.Add(BuildCall(placement.PrefabId, material, OneAnchor(px, py), priority, topPriority, anchor.WorldId, execute, dryRun));
                }

                if (definition.WireLaneDy.HasValue)
                {
                    JArray lane = HorizontalRunAnchors(anchor.X + 1, anchor.Y + definition.WireLaneDy.Value, anchor.Width - 2);
                    calls.Add(BuildCall("Wire", material, lane, priority, topPriority, anchor.WorldId, execute, dryRun));
                }
            }

            return calls;
        }

        private static RoomTemplateCall OrdersCall(string action, int x1, int y1, int x2, int y2, int priority, bool topPriority, int worldId, bool execute, bool dryRun)
        {
            var args = new JObject
            {
                ["domain"] = "area",
                ["action"] = action,
                ["x1"] = x1,
                ["y1"] = y1,
                ["x2"] = x2,
                ["y2"] = y2,
                ["priority"] = priority,
                ["topPriority"] = topPriority,
                ["worldId"] = worldId,
                ["dryRun"] = dryRun
            };
            if (execute && !dryRun)
                args["confirm"] = true;
            return new RoomTemplateCall("orders_control", args);
        }

        private static RoomTemplateCall BuildCall(string prefabId, string material, JArray anchors, int priority, bool topPriority, int worldId, bool execute, bool dryRun)
        {
            var args = new JObject
            {
                ["domain"] = "planning",
                ["action"] = "build_area",
                ["prefabId"] = prefabId,
                ["material"] = material,
                ["anchors"] = anchors,
                ["priority"] = priority,
                ["topPriority"] = topPriority,
                ["worldId"] = worldId,
                ["dryRun"] = dryRun,
                ["allowPartial"] = true,
                ["autoDig"] = true,
                ["maxCommitAnchors"] = 32
            };
            if (execute && !dryRun)
                args["confirm"] = true;
            return new RoomTemplateCall("building_control", args);
        }

        /// <summary>
        /// Run the generated dig/build calls.
        ///
        /// These cannot go through OniToolRegistry.CallTool: that path requires a `task` on every
        /// sub-call, rejects raw coordinates for any tool other than coordinate_control/world_editor,
        /// and building_control refuses domain=planning action=build_area outside a virtual-file
        /// edit context. A template's calls are coordinates by construction, so all three fire and
        /// execute=true never gets past the first call.
        ///
        /// Dispatch to the handlers directly instead, the same way the world-editor path does, and
        /// propagate the caller's task so the player still sees what is happening. The coordinates
        /// here are derived from a resolved anchor by this tool, not supplied by a model, which is
        /// what the coordinate gate exists to prevent.
        /// </summary>
        private static JArray ExecuteRoomTemplateCalls(JObject parentArgs, List<RoomTemplateCall> calls)
        {
            var results = new JArray();
            string task = parentArgs?["task"]?.ToString();
            if (string.IsNullOrWhiteSpace(task))
                task = "room template: " + (parentArgs?["kind"]?.ToString() ?? "starter");

            foreach (RoomTemplateCall call in calls)
            {
                if (string.IsNullOrWhiteSpace(call.Args["task"]?.ToString()))
                    call.Args["task"] = task;
                ToolCallMiddleware.PresentTaskDescription(task);

                CallToolResult result = DispatchRoomTemplateCall(call);
                string text = result.Content?.FirstOrDefault()?.Text ?? string.Empty;
                results.Add(new JObject
                {
                    ["tool"] = call.Tool,
                    ["action"] = call.Args["action"]?.ToString(),
                    ["ok"] = !result.IsError,
                    ["summary"] = SummarizeRoomTemplateResult(text),
                    ["diagnostic"] = DiagnoseRoomTemplateResult(text, result.IsError),
                    ["error"] = result.IsError ? TrimRoomTemplateText(text, 1200) : null
                });
                if (result.IsError)
                    break;
            }

        return results;
        }

        private static CallToolResult DispatchRoomTemplateCall(RoomTemplateCall call)
        {
            if (string.Equals(call.Tool, "orders_control", StringComparison.OrdinalIgnoreCase))
                return OrdersTools.ControlOrders().Handler(call.Args);
            if (string.Equals(call.Tool, "building_control", StringComparison.OrdinalIgnoreCase))
                return BuildingControlTools.ControlBuildingFromVirtualFile(call.Args);
            return CallToolResult.Error("Unsupported room template call target: " + call.Tool);
        }

        private static string ResolveRoomTemplateKind(JObject args)
        {
            string text = ((args["kind"] ?? args["template"] ?? args["plan"])?.ToString() ?? string.Empty).Trim().ToLowerInvariant();
            // Checked before toilet/lab: a SPOM phrase can mention oxygen research or a lab
            // wing, and the generic lab match would otherwise swallow it.
            if (text.Contains("spom") || text.Contains("制氧模块") || text.Contains("自供电制氧")
                || text.Contains("电解制氧") || (text.Contains("oxygen") && text.Contains("module")))
                return "spom";

            if (text.Contains("washroom") || text.Contains("plumbed") || text.Contains("抽水马桶") || text.Contains("水洗卫生间") || text.Contains("水洗厕所") || text.Contains("豪华厕所"))
                return "washroom";

            if (text.Contains("farm") || text.Contains("greenhouse") || text.Contains("planter") || text.Contains("农场") || text.Contains("种植室") || text.Contains("温室") || text.Contains("农业"))
                return "farm";

            bool wantsToilet = text.Contains("toilet") || text.Contains("restroom") || text.Contains("latrine") || text.Contains("厕所") || text.Contains("卫生间") || text.Contains("洗手");
            bool wantsLab = text.Contains("lab") || text.Contains("research") || text.Contains("实验") || text.Contains("研究");
            if (text.Contains("starter") || text.Contains("toilet_lab") || text.Contains("toilet+lab") || text.Contains("toilet lab") || text.Contains("厕所加实验室") || text.Contains("厕所和实验室") || text.Contains("厕所实验室") || (wantsToilet && wantsLab))
                return "starter";
            if (wantsToilet)
                return "toilet";
            if (wantsLab)
                return "lab";
            return string.Empty;
        }

        private static int DefaultRoomTemplateWidth(string kind)
        {
            if (kind == "starter")
                return 15;
            RoomTemplateDefinition definition = GetRoomTemplateDefinition(kind);
            return definition != null ? definition.DefaultWidth : 8;
        }

        private static int DefaultRoomTemplateHeight(string kind)
        {
            if (kind == "starter")
                return 4;
            RoomTemplateDefinition definition = GetRoomTemplateDefinition(kind);
            return definition != null ? definition.DefaultHeight : 4;
        }

        private static bool TryGetInt(JObject args, string key, out int value)
        {
            value = 0;
            return args[key] != null && int.TryParse(args[key].ToString(), out value);
        }

        private static string SummarizeRoomTemplateResult(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "ok";

            try
            {
                var obj = JObject.Parse(text);
                // "planned" is always 0 on a utility path during a dry run, because nothing is
                // committed; the real outcome lives in valid/success. Reporting planned alone
                // made a healthy wire preflight read as a failure, so prefer success and valid
                // whenever the payload carries them.
                string[] keys = { "success", "valid", "pathCells", "planned", "marked", "executedCells", "remainingCells", "failed", "prefabId", "dryRun" };
                var parts = keys.Where(k => obj[k] != null).Select(k => k + "=" + obj[k]).ToList();
                return parts.Count == 0 ? "ok" : string.Join(", ", parts.ToArray());
            }
            catch
            {
                return TrimRoomTemplateText(text, 400);
            }
        }

        private static string TrimRoomTemplateText(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max)
                return text ?? string.Empty;
            return text.Substring(0, max) + "...";
        }

        private sealed class RoomTemplateAnchor
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public int WorldId;
            public string AreaId;

            public RoomTemplateAnchor With(int x, int y, int width, int height)
            {
                return new RoomTemplateAnchor { X = x, Y = y, Width = width, Height = height, WorldId = WorldId, AreaId = AreaId };
            }
        }

        private sealed class RoomTemplateCall
        {
            public readonly string Tool;
            public readonly JObject Args;

            public JObject Call => new JObject { ["tool"] = Tool, ["arguments"] = (JObject)Args.DeepClone() };

            public RoomTemplateCall(string tool, JObject args)
            {
                Tool = tool;
                Args = args;
            }
        }
    }
}
