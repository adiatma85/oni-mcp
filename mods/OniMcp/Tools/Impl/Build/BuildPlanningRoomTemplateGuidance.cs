using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using OniMcp.Core;

namespace OniMcp.Tools
{
    public static partial class BuildPlanningTools
    {
        private static JArray BuildRoomTemplateExecutionPlan(string kind, RoomTemplateAnchor anchor, int priority)
        {
            var steps = new JArray();
            if (kind == "starter")
            {
                int roomWidth = Math.Max(7, (anchor.Width - 1) / 2);
                steps.Add(RoomStep("dig_toilet_interior", "orders_control", "dig",
                    Rect(anchor.X + 1, anchor.Y + 1, anchor.X + roomWidth - 2, anchor.Y + anchor.Height - 2),
                    priority, "Clear natural solids inside toilet room before furniture errands."));
                steps.Add(RoomStep("dig_lab_interior", "orders_control", "dig",
                    Rect(anchor.X + roomWidth + 2, anchor.Y + 1, anchor.X + anchor.Width - 2, anchor.Y + anchor.Height - 2),
                    priority, "Clear natural solids inside lab before research station."));
                steps.Add(RoomStep("build_shells_and_divider", "building_control", "build_area",
                    Rect(anchor.X, anchor.Y, anchor.X + anchor.Width - 1, anchor.Y + anchor.Height - 1),
                    priority, "Build room floors, ceilings, walls, middle divider, doors."));
                steps.Add(RoomStep("place_core_buildings", "building_control", "build_area",
                    Rect(anchor.X + 2, anchor.Y + 1, anchor.X + anchor.Width - 3, anchor.Y + 1),
                    priority, "Place Outhouse, WashBasin, and ResearchCenter in same template call."));
            }
            else
            {
                steps.Add(RoomStep("dig_interior", "orders_control", "dig",
                    Rect(anchor.X + 1, anchor.Y + 1, anchor.X + anchor.Width - 2, anchor.Y + anchor.Height - 2),
                    priority, "Clear natural solids inside room before furniture errands."));
                steps.Add(RoomStep("build_shell_and_core", "building_control", "build_area",
                    Rect(anchor.X, anchor.Y, anchor.X + anchor.Width - 1, anchor.Y + anchor.Height - 1),
                    priority, "Build shell, door, core building."));
                steps.Add(RoomStep("optional_sweep_debris", "orders_control", "sweep",
                    Rect(anchor.X + 1, anchor.Y + 1, anchor.X + anchor.Width - 2, anchor.Y + anchor.Height - 2),
                    priority, "Sweep newly dug debris if pathing allows."));
            }

            return steps;
        }

        private static JArray BuildRoomTemplateVerificationPlan(string kind, RoomTemplateAnchor anchor, int priority)
        {
            string purpose = kind == "starter"
                ? "Confirm toilet, wash station, research station, interior digs, temperature, oxygen, power anchors in one compact view."
                : "Confirm room shell, door, core building, interior dig, temperature, oxygen.";
            var plan = new JArray
            {
                new JObject
                {
                    ["step"] = "inspect_compact_result",
                    ["why"] = "Use results[].summary/errors first; avoid broad map reads unless blocker appears."
                },
                ZoomRead("verify_local_zoom", anchor, purpose)
            };

            if (kind == "starter")
            {
                int roomWidth = Math.Max(7, (anchor.Width - 1) / 2);
                int washBasinX = anchor.X + Math.Max(4, roomWidth - 3);
                plan.Add(new JObject
                {
                    ["step"] = "expected_anchor_cells",
                    ["why"] = "Use these exact cells to verify the one-call starter template without counting columns.",
                    ["cells"] = new JArray
                    {
                        ExpectedCell("Outhouse", anchor.X + 2, anchor.Y + 1),
                        ExpectedCell("WashBasin", washBasinX, anchor.Y + 1),
                        ExpectedCell("ResearchCenter", anchor.X + roomWidth + 3, anchor.Y + 1)
                    }
                });
                AddCellRead(plan, "verify_outhouse_cell", anchor.X + 2, anchor.Y + 1,
                    "Verify outhouse blueprint/building, debris, temperature, element, Decision Hints.");
                AddCellRead(plan, "verify_wash_basin_cell", washBasinX, anchor.Y + 1,
                    "Verify wash basin blueprint/building, ports if any, debris, Decision Hints.");
                AddCellRead(plan, "verify_research_station_cell", anchor.X + roomWidth + 3, anchor.Y + 1,
                    "Verify lab research station blueprint/building without counting columns.");
            }
            else
            {
                AddCellRead(plan, "verify_core_cell", anchor.X + 2, anchor.Y + 1,
                    "Check building, pickup stacks, element, temperature, ports, Decision Hints, and obstruction first core cell.");
            }

            plan.Add(new JObject
            {
                ["step"] = "verify_reachability",
                ["tool"] = "world_editor",
                ["arguments"] = new JObject
                {
                    ["command"] = "read",
                    ["path"] = "/active/dupes/reachability.md",
                    ["radius"] = 12,
                    ["sampleLimit"] = 12
                },
                ["call"] = "world_editor command=read path=/active/dupes/reachability.md radius=12 sampleLimit=12",
                ["why"] = "Check reachable area before adding rescue ladders or extra digs."
            });
            plan.Add(new JObject
            {
                ["step"] = "debris_followup",
                ["tool"] = "orders_control",
                ["arguments"] = new JObject
                {
                    ["domain"] = "area",
                    ["action"] = "sweep",
                    ["x1"] = anchor.X,
                    ["y1"] = anchor.Y,
                    ["x2"] = anchor.X + anchor.Width - 1,
                    ["y2"] = anchor.Y + anchor.Height - 1,
                    ["priority"] = priority,
                    ["dryRun"] = true
                },
                ["call"] = "orders_control domain=area action=sweep x1=" + anchor.X
                    + " y1=" + anchor.Y
                    + " x2=" + (anchor.X + anchor.Width - 1)
                    + " y2=" + (anchor.Y + anchor.Height - 1)
                    + " priority=" + priority + " dryRun=true",
                ["why"] = "Preview sweeping newly dug debris before issuing cleanup."
            });
            plan.Add(new JObject
            {
                ["step"] = "if_failed",
                ["tool"] = "world_editor",
                ["arguments"] = new JObject
                {
                    ["command"] = "read",
                    ["path"] = "/active/diagnostics/logs.md",
                    ["logLimit"] = 220
                },
                ["call"] = "world_editor command=read path=/active/diagnostics/logs.md logLimit=220",
                ["why"] = "Only read logs when generated work fails, crashes, or returns unsafe diagnostics."
            });
            return plan;
        }

        private static RoomTemplateAnchor TryAutoRoomTemplateAnchor(JObject args, string kind, int worldId, out string error)
        {
            error = null;
            bool requestedAutoLayout = ToolUtil.GetBool(args, "autoLayout", false) || ToolUtil.GetBool(args, "auto", false);
            if (!requestedAutoLayout && kind != "starter")
                return null;

            var layoutArgs = new JObject
            {
                ["action"] = "layout_candidates",
                ["purpose"] = kind == "starter" ? "starter" : kind,
                ["width"] = ToolUtil.GetInt(args, "width") ?? DefaultRoomTemplateWidth(kind),
                ["height"] = ToolUtil.GetInt(args, "height") ?? 4,
                ["limit"] = 1,
                ["maxCells"] = ToolUtil.GetInt(args, "maxCells") ?? 1600,
                ["worldId"] = worldId
            };

            CallToolResult result = WorldAnalysisTools.GetLayoutCandidates().Handler(layoutArgs);
            string text = result.Content?.FirstOrDefault()?.Text ?? string.Empty;
            if (result.IsError)
            {
                error = "autoLayout layout_candidates failed: " + TrimRoomTemplateText(text, 400);
                return null;
            }

            var rect = (JObject.Parse(text)["planning"]?["candidates"]?.FirstOrDefault()?["rect"]) as JArray;
            if (rect == null || rect.Count < 4)
            {
                error = "autoLayout found no room candidates.";
                return null;
            }

            var rectDict = new Dictionary<string, int>
            {
                ["x1"] = rect[0].Value<int>(),
                ["y1"] = rect[1].Value<int>(),
                ["x2"] = rect[2].Value<int>(),
                ["y2"] = rect[3].Value<int>()
            };
            return BuildRoomTemplateAnchor(args, kind, rectDict["x1"], rectDict["y1"], worldId, rectDict);
        }

        private static JObject DiagnoseRoomTemplateResult(string text, bool isError)
        {
            var diagnostic = new JObject { ["status"] = isError ? "error" : "ok" };
            if (string.IsNullOrWhiteSpace(text))
                return diagnostic;

            JObject obj = null;
            try
            {
                obj = JObject.Parse(text);
            }
            catch
            {
            }

            // Structured signals are authoritative; keywords are a guess. The previous version
            // searched the whole serialized payload, which always contains an "obstructions"
            // key, so every failure came back "obstructed" -- research locks included.
            string category = null;
            if (obj != null)
            {
                if (FindBoolean(obj, "unlocked") == false)
                    category = "research_locked";
                else if (FindNonEmptyArray(obj, "obstructions"))
                    category = "obstructed";
                else if (FindBoolean(obj, "satisfied") == false)
                    category = "missing_material";
            }

            if (category == null)
            {
                // Fall back to keywords over message VALUES only, never field names.
                string lower = CollectMessageText(obj, text).ToLowerInvariant();
                if (ContainsAny(lower, "not researched", "tech locked", "未解锁", "未研究"))
                    category = "research_locked";
                else if (ContainsAny(lower, "obstruct", "blocked", "occupied", "阻挡", "堵塞", "占用", "被占", "挡住"))
                    category = "obstructed";
                else if (ContainsAny(lower, "material", "resource", "材料", "资源", "原料", "缺少"))
                    category = "missing_material";
                else if (ContainsAny(lower, "support", "foundation", "支撑", "地基", "依托"))
                    category = "missing_support";
                else if (ContainsAny(lower, "reach", "access", "不可达", "无法到达", "够不到", "路径"))
                    category = "unreachable";
                else if (ContainsAny(lower, "confirm", "确认", "confirm=true"))
                    category = "missing_confirm";
            }

            if (!string.IsNullOrEmpty(category))
            {
                diagnostic["category"] = category;
                diagnostic["nextRead"] = category == "unreachable"
                    ? "/active/dupes/reachability.md"
                    : category == "research_locked"
                        ? "colony_control domain=management kind=research action=list"
                        : "/active/map/cell_X_Y.md";
                diagnostic["hint"] = category == "research_locked"
                    ? "The building is not researched yet. Check techGate.lockedPrefabs; no amount of digging will help."
                    : "Use verificationPlan or nextActions before broad map reads.";
            }

            return diagnostic;
        }

        /// <summary>First value of a boolean property with this name, searched recursively.</summary>
        private static bool? FindBoolean(JToken token, string name)
        {
            JObject obj = token as JObject;
            if (obj != null)
            {
                foreach (var property in obj.Properties())
                {
                    if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)
                        && property.Value != null && property.Value.Type == JTokenType.Boolean)
                        return property.Value.Value<bool>();
                    bool? nested = FindBoolean(property.Value, name);
                    if (nested.HasValue)
                        return nested;
                }
                return null;
            }

            JArray array = token as JArray;
            if (array != null)
            {
                foreach (JToken item in array)
                {
                    bool? nested = FindBoolean(item, name);
                    if (nested.HasValue)
                        return nested;
                }
            }
            return null;
        }

        /// <summary>True when a property with this name holds a non-empty array anywhere in the payload.</summary>
        private static bool FindNonEmptyArray(JToken token, string name)
        {
            JObject obj = token as JObject;
            if (obj != null)
            {
                foreach (var property in obj.Properties())
                {
                    JArray candidate = property.Value as JArray;
                    if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)
                        && candidate != null && candidate.Count > 0)
                        return true;
                    if (FindNonEmptyArray(property.Value, name))
                        return true;
                }
                return false;
            }

            JArray array = token as JArray;
            if (array != null)
            {
                foreach (JToken item in array)
                {
                    if (FindNonEmptyArray(item, name))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Human-readable message values only. Keeps field names out of keyword matching,
        /// which is what made every failure look obstructed.
        /// </summary>
        private static string CollectMessageText(JObject obj, string fallback)
        {
            if (obj == null)
                return fallback ?? string.Empty;

            var parts = new List<string>();
            foreach (string key in new[] { "error", "reason", "message", "summary", "failedReason", "next", "suggestion" })
            {
                string value = obj[key]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    parts.Add(value);
            }
            return parts.Count > 0 ? string.Join("\n", parts.ToArray()) : string.Empty;
        }

        private static bool ContainsAny(string text, params string[] needles)
        {
            foreach (string needle in needles)
            {
                if (!string.IsNullOrEmpty(needle) && text.Contains(needle))
                    return true;
            }

            return false;
        }
    }
}
