using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OniMcp.Core;
using OniMcp.Support;

namespace OniMcp.Tools
{
    /// <summary>
    /// Ranked "what should I do next" advisor.
    ///
    /// Joins three inputs that previously never met:
    ///   1. live colony facts read from the running game,
    ///   2. Klei's own per-cycle ColonyDiagnostic verdicts, which the mod already had access to
    ///      but never consumed, preferring to recompute worse versions of the same signals,
    ///   3. the curated strategy corpus, which supplies the reason behind each recommendation.
    ///
    /// Output is one ordered list. Every entry carries a numeric priority, so a caller does not
    /// have to guess whether "critical food" outranks "critical stress".
    /// </summary>
    public static partial class SurvivalPlanTools
    {
        internal static CallToolResult HandleColonyAdvice(JObject args)
        {
            args = args ?? new JObject();
            bool visibleOnly = ToolUtil.GetBool(args, "visibleOnly", true);
            int limit = ToolUtil.ClampLimit(args, 10, 40);

            ColonyRuleSet.ColonyFacts facts = ReadColonyFacts(args, visibleOnly);
            List<ColonyRuleSet.ColonyRule> fired = ColonyRuleSet.Evaluate(facts);
            List<Dictionary<string, object>> kleiConcerns = ReadKleiDiagnosticConcerns(args);

            var recommendations = new List<Dictionary<string, object>>();
            foreach (ColonyRuleSet.ColonyRule rule in fired.Take(limit))
                recommendations.Add(BuildRecommendation(rule));

            var payload = new Dictionary<string, object>
            {
                ["cycle"] = GameClock.Instance != null ? (int)GameClock.Instance.GetCycle() : -1,
                ["situation"] = fired.Count == 0
                    ? "No rule fired. The colony is within every configured threshold."
                    : $"{fired.Count} issue(s) detected; highest priority is {fired[0].Category}.",
                ["facts"] = FactsToDictionary(facts),
                ["recommendations"] = recommendations,
                ["gameDiagnostics"] = kleiConcerns,
                ["contract"] = "recommendations are sorted by priority, highest first. knowledge[].derive lists values to read from the live game rather than trusting a cached number.",
                ["tokenHint"] = "Read recommendations[0] first. Each carries why, knowledge and a re-invocable action with dryRun=true."
            };

            return CallToolResult.Text(JsonConvert.SerializeObject(payload, McpJsonUtil.Settings));
        }

        private static ColonyRuleSet.ColonyFacts ReadColonyFacts(JObject args, bool visibleOnly)
        {
            var buildings = CountBuildings();
            var allReady = ReadyHarvestables(visibleOnly).Take(12).ToList();
            int reachable = allReady.Count(item =>
                item.TryGetValue("reachable", out object value) && value is bool flag && flag);

            var facts = new ColonyRuleSet.ColonyFacts
            {
                Dupes = Components.LiveMinionIdentities.Count,
                FoodKcal = UsableFoodKcal(visibleOnly),
                FoodKcalPerDupe = ToolUtil.GetFloat(args, "foodKcalPerDupe") ?? ColonyRuleSet.DefaultFoodKcalPerDupe,
                MaxStress = MaxStress(),
                OxygenProducers = CountMatching(buildings, "OxygenDiffuser", "MineralDeoxidizer", "Electrolyzer"),
                Toilets = CountMatching(buildings, "Outhouse", "FlushToilet"),
                Beds = CountMatching(buildings, "Bed", "LuxuryBed"),
                ResearchStations = CountMatching(buildings, "ResearchCenter", "AdvancedResearchCenter"),
                Batteries = CountMatching(buildings, "Battery", "BatteryMedium", "BatterySmart"),
                ReadyHarvestables = allReady.Count,
                ReachableHarvestables = reachable
            };

            ApplyPowerFacts(args, facts);
            return facts;
        }

        /// <summary>
        /// Read grid state so the advisor can spot a colony coasting on battery charge.
        /// Guarded: power circuit internals move between game builds, and a failed read should
        /// downgrade this one rule rather than break the whole advisory.
        /// </summary>
        private static void ApplyPowerFacts(JObject args, ColonyRuleSet.ColonyFacts facts)
        {
            try
            {
                int worldId = ToolUtil.ResolveWorldId(args);
                int generators = 0;
                int generatorsRunning = 0;
                foreach (Generator generator in Components.Generators.Items)
                {
                    if (generator == null || generator.GetMyWorldId() != worldId)
                        continue;
                    generators++;
                    if (generator.IsProducingPower())
                        generatorsRunning++;
                }

                int consumers = 0;
                int consumersPowered = 0;
                foreach (EnergyConsumer consumer in Components.EnergyConsumers.Items)
                {
                    if (consumer == null || consumer.GetMyWorldId() != worldId)
                        continue;
                    consumers++;
                    if (consumer.IsPowered)
                        consumersPowered++;
                }

                float capacityJ = 0f;
                float storedJ = 0f;
                foreach (Battery battery in Components.Batteries.Items)
                {
                    if (battery == null || battery.GetMyWorldId() != worldId)
                        continue;
                    capacityJ += ToolUtil.SafeFloat(battery.Capacity);
                    storedJ += ToolUtil.SafeFloat(battery.JoulesAvailable);
                }

                facts.GeneratorCount = generators;
                facts.GeneratorOperational = generatorsRunning;
                facts.ConsumerCount = consumers;
                facts.ConsumerPowered = consumersPowered;
                // No batteries at all reads as 100%: there is no reserve to run down, so the
                // idle-generator rule should stay quiet and let the "no battery" rule speak.
                facts.BatteryStoredPercent = capacityJ > 0f ? storedJ / capacityJ * 100f : 100f;
                facts.PowerScanned = true;
            }
            catch (Exception ex)
            {
                OniMcpLog.Debug("[OniMcp] advisor power scan skipped: " + ex.Message);
                facts.PowerScanned = false;
            }
        }

        /// <summary>
        /// Klei computes its own opinion of ~15 subsystems every cycle. Surface the ones it is
        /// unhappy about; these are authoritative and cost nothing to read.
        /// </summary>
        private static List<Dictionary<string, object>> ReadKleiDiagnosticConcerns(JObject args)
        {
            var concerns = new List<Dictionary<string, object>>();
            try
            {
                ColonyDiagnosticUtility utility = ColonyDiagnosticUtility.Instance;
                if (utility == null || ClusterManager.Instance == null)
                    return concerns;

                int worldId = ToolUtil.GetInt(args, "worldId") ?? ClusterManager.Instance.activeWorldId;
                if (!utility.diagnosticDisplaySettings.ContainsKey(worldId))
                    return concerns;

                foreach (var setting in utility.diagnosticDisplaySettings[worldId])
                {
                    ColonyDiagnostic diagnostic = utility.GetDiagnostic(setting.Key, worldId);
                    if (diagnostic == null)
                        continue;

                    string opinion = diagnostic.LatestResult.opinion.ToString();
                    if (opinion == "Normal" || opinion == "Good")
                        continue;

                    concerns.Add(new Dictionary<string, object>
                    {
                        ["id"] = diagnostic.id,
                        ["name"] = diagnostic.name,
                        ["opinion"] = opinion,
                        ["message"] = diagnostic.LatestResult.Message
                    });
                }
            }
            catch (Exception ex)
            {
                OniMcpLog.Debug("[OniMcp] advisor klei diagnostics skipped: " + ex.Message);
            }
            return concerns;
        }

        private static Dictionary<string, object> BuildRecommendation(ColonyRuleSet.ColonyRule rule)
        {
            var knowledge = StrategyKnowledgeTools
                .Lookup(rule.KnowledgeQuery, null, 2, StrategyKnowledgeTools.SupportingKnowledgeMinScore)
                .Select(entry => new Dictionary<string, object>
                {
                    ["id"] = entry.Id,
                    ["title"] = entry.Title,
                    ["why"] = entry.Body.FirstOrDefault() ?? "",
                    ["derive"] = entry.Derive,
                    ["source"] = entry.Source
                })
                .ToList();

            return new Dictionary<string, object>
            {
                ["priority"] = rule.Priority,
                ["category"] = rule.Category,
                ["severity"] = rule.Severity,
                ["what"] = rule.Message,
                ["knowledge"] = knowledge,
                ["action"] = BuildRuleAction(rule)
            };
        }

        private static Dictionary<string, object> BuildRuleAction(ColonyRuleSet.ColonyRule rule)
        {
            switch (rule.Category)
            {
                case "food":
                case "food_reachability":
                    return Action("colony_control", new Dictionary<string, object>
                    {
                        ["domain"] = "bio", ["kind"] = "farming", ["action"] = "list_harvestables", ["readyOnly"] = true
                    }, "List ready harvestables and check reachability.");
                case "oxygen":
                    return Action("server_control", new Dictionary<string, object>
                    {
                        ["domain"] = "strategy", ["action"] = "query", ["query"] = "oxygen"
                    }, "Review oxygen options before committing to a build.");
                case "power":
                    return Action("read_control", new Dictionary<string, object>
                    {
                        ["domain"] = "infrastructure", ["action"] = "power_summary"
                    }, "Inspect the grid: which generators are idle and why.");
                case "hygiene":
                case "sleep":
                case "research":
                    return Action("building_control", new Dictionary<string, object>
                    {
                        ["domain"] = "planning", ["action"] = "room_template", ["kind"] = "starter", ["dryRun"] = true
                    }, "Preview the starter room template covering toilet, bed and research space.");
                case "stress":
                    return Action("dupes_control", new Dictionary<string, object>
                    {
                        ["domain"] = "info", ["action"] = "status_check"
                    }, "Find which duplicant is stressed and why.");
                default:
                    return Action("colony_control", new Dictionary<string, object>
                    {
                        ["domain"] = "diagnostic", ["action"] = "diagnostics"
                    }, "Re-read colony diagnostics.");
            }
        }

        private static Dictionary<string, object> Action(string tool, Dictionary<string, object> arguments, string why)
        {
            arguments["task"] = why;
            return new Dictionary<string, object>
            {
                ["tool"] = tool,
                ["arguments"] = arguments,
                ["why"] = why
            };
        }

        private static Dictionary<string, object> FactsToDictionary(ColonyRuleSet.ColonyFacts f)
        {
            var d = new Dictionary<string, object>
            {
                ["dupes"] = f.Dupes,
                ["foodKcal"] = Math.Round(f.FoodKcal, 1),
                ["maxStress"] = Math.Round(f.MaxStress, 1),
                ["oxygenProducers"] = f.OxygenProducers,
                ["toilets"] = f.Toilets,
                ["beds"] = f.Beds,
                ["researchStations"] = f.ResearchStations,
                ["batteries"] = f.Batteries,
                ["readyHarvestables"] = f.ReadyHarvestables,
                ["reachableHarvestables"] = f.ReachableHarvestables
            };
            if (f.PowerScanned)
            {
                d["generators"] = f.GeneratorCount;
                d["generatorsRunning"] = f.GeneratorOperational;
                d["consumers"] = f.ConsumerCount;
                d["consumersPowered"] = f.ConsumerPowered;
                d["batteryStoredPercent"] = Math.Round(f.BatteryStoredPercent, 1);
            }
            return d;
        }
    }
}
