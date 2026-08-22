using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OniMcp.Core;
using OniMcp.Support;

namespace OniMcp.Tools
{
    /// <summary>
    /// Query surface for the static strategy corpus.
    /// Reached as server_control domain=strategy; not registered as a top-level tool,
    /// so the public tool surface is unchanged.
    /// </summary>
    public static partial class StrategyKnowledgeTools
    {
        public static McpTool ControlStrategy()
        {
            return new McpTool
            {
                Name = "strategy_knowledge_control",
                Group = "knowledge",
                Mode = "read",
                Risk = "none",
                Hidden = true,
                Tags = new List<string> { "strategy", "mechanics", "formula", "advice", "攻略", "机制", "公式" },
                Description = "Static Oxygen Not Included strategy corpus: mechanics, formulas, build-order judgement and biome notes. action=query searches, action=categories lists topic buckets, action=index returns an overview. Values the running game exposes are referenced through the derive field instead of copied, so read those live rather than trusting a cached number.",
                Parameters = new Dictionary<string, McpToolParameter>
                {
                    ["action"] = new McpToolParameter { Type = "string", Description = "query=search entries, categories=list topic buckets with counts, index=corpus overview. Default query.", Required = false, EnumValues = new List<string> { "query", "categories", "index" } },
                    ["query"] = new McpToolParameter { Type = "string", Description = "Keyword or question. Bilingual: SPOM, electrolyzer, 电解器, 分层, wire overload, 过载.", Required = false },
                    ["category"] = new McpToolParameter { Type = "string", Description = "Filter by topic bucket, e.g. oxygen, thermal, gas_fluid, power, food, farming, ranching, automation, dupes, space.", Required = false },
                    ["dlc"] = new McpToolParameter { Type = "string", Description = "Filter by game version: base, spaced_out, or any. Entries tagged both always match. Default any.", Required = false, EnumValues = new List<string> { "base", "spaced_out", "any" } },
                    ["detail"] = new McpToolParameter { Type = "string", Description = "brief returns a one-line summary per entry; full returns body, formulas, cautions, derive and source. Default full.", Required = false, EnumValues = new List<string> { "brief", "full" } },
                    ["limit"] = new McpToolParameter { Type = "integer", Description = "Maximum entries to return. Default 8, max 40.", Required = false }
                },
                Handler = HandleStrategy
            };
        }

        private static CallToolResult HandleStrategy(JObject args)
        {
            args = args ?? new JObject();
            string action = Normalize(args["action"] != null ? args["action"].ToString() : null);
            if (string.IsNullOrEmpty(action))
                action = "query";

            if (action == "categories")
                return JsonText(BuildCategoriesPayload());
            if (action == "index")
                return JsonText(BuildIndexPayload());
            if (action != "query" && action != "search")
                return CallToolResult.Error("action must be one of: query, categories, index");

            return JsonText(BuildQueryPayload(args));
        }

        private static Dictionary<string, object> BuildQueryPayload(JObject args)
        {
            string query = Normalize(args["query"] != null ? args["query"].ToString() : null);
            string category = Normalize(args["category"] != null ? args["category"].ToString() : null);
            string dlc = Normalize(args["dlc"] != null ? args["dlc"].ToString() : null);
            string detail = Normalize(args["detail"] != null ? args["detail"].ToString() : null) == "brief" ? "brief" : "full";
            int limit = ToolUtil.ClampLimit(args, 8, 40);

            var matches = AllEntries()
                .Where(entry => string.IsNullOrEmpty(category) || Normalize(entry.Category) == category)
                .Where(entry => MatchesDlc(entry, dlc))
                .Select(entry => new { Entry = entry, Score = Score(entry, query) })
                .Where(item => string.IsNullOrEmpty(query) || item.Score > 0)
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Entry.Category)
                .ThenBy(item => item.Entry.Title)
                .Take(limit)
                .ToList();

            return new Dictionary<string, object>
            {
                ["query"] = string.IsNullOrEmpty(query) ? null : query,
                ["category"] = string.IsNullOrEmpty(category) ? null : category,
                ["dlc"] = string.IsNullOrEmpty(dlc) ? "any" : dlc,
                ["detail"] = detail,
                ["returned"] = matches.Count,
                ["totalEntries"] = AllEntries().Count,
                ["contract"] = "body and formulas are curated judgement. derive lists what to read from the running game instead of trusting a cached number.",
                ["results"] = matches.Select(item => ToDictionary(item.Entry, item.Score, detail)).ToList(),
                ["nextActions"] = BuildStrategyNextActions(matches.Count)
            };
        }

        private static Dictionary<string, object> BuildCategoriesPayload()
        {
            return new Dictionary<string, object>
            {
                ["totalEntries"] = AllEntries().Count,
                ["categories"] = AllEntries()
                    .GroupBy(entry => entry.Category)
                    .OrderBy(group => group.Key)
                    .Select(group => new Dictionary<string, object>
                    {
                        ["category"] = group.Key,
                        ["count"] = group.Count()
                    })
                    .ToList()
            };
        }

        private static Dictionary<string, object> BuildIndexPayload()
        {
            return new Dictionary<string, object>
            {
                ["totalEntries"] = AllEntries().Count,
                ["dlcBreakdown"] = AllEntries()
                    .GroupBy(entry => DlcName(entry.Dlc))
                    .OrderBy(group => group.Key)
                    .Select(group => new Dictionary<string, object>
                    {
                        ["dlc"] = group.Key,
                        ["count"] = group.Count()
                    })
                    .ToList(),
                ["categories"] = BuildCategoriesPayload()["categories"],
                ["usage"] = new[]
                {
                    "server_control domain=strategy action=query query=\"SPOM\" for build judgement.",
                    "server_control domain=strategy action=query category=thermal for a topic sweep.",
                    "Pass dlc=base or dlc=spaced_out to hide entries that do not apply to the running game.",
                    "Verify anything listed under derive against the live save before acting on it."
                }
            };
        }

        private static List<Dictionary<string, object>> BuildStrategyNextActions(int returned)
        {
            var actions = new List<Dictionary<string, object>>();
            if (returned == 0)
            {
                actions.Add(new Dictionary<string, object>
                {
                    ["tool"] = "server_control",
                    ["arguments"] = new Dictionary<string, object> { ["domain"] = "strategy", ["action"] = "categories" },
                    ["why"] = "No entry matched. List the topic buckets and retry with a category."
                });
                return actions;
            }

            actions.Add(new Dictionary<string, object>
            {
                ["tool"] = "colony_control",
                ["arguments"] = new Dictionary<string, object> { ["domain"] = "snapshot", ["action"] = "get", ["profile"] = "minimal" },
                ["why"] = "Check the live colony before applying curated advice."
            });
            return actions;
        }

        private static CallToolResult JsonText(Dictionary<string, object> payload)
        {
            return CallToolResult.Text(JsonConvert.SerializeObject(payload, McpJsonUtil.Settings));
        }
    }
}
