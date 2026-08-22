using System;
using System.Collections.Generic;
using System.Linq;

namespace OniMcp.Tools
{
    /// <summary>
    /// Single source of truth for colony health rules.
    ///
    /// These thresholds were previously duplicated across SurvivalPlanTools blockers,
    /// SnapshotResearchAlertTools.BuildAlerts and DiagnosticsTools.BuildDiagnostics, and had
    /// drifted apart: stress fired at &gt;40 in two of them and &gt;=60 in the third, with messages
    /// in different languages. Callers should evaluate here so a threshold change lands once.
    ///
    /// Priority orders recommendations. Higher runs first. Severity is the label a human reads;
    /// priority is what the advisor sorts on, because "critical" alone cannot separate
    /// "nobody can breathe" from "someone is mildly stressed".
    /// </summary>
    public static class ColonyRuleSet
    {
        public class ColonyRule
        {
            public string Category { get; set; }
            public string Severity { get; set; }
            public int Priority { get; set; }
            public string Message { get; set; }
            /// <summary>Query used to pull supporting reasoning out of the strategy corpus.</summary>
            public string KnowledgeQuery { get; set; }
        }

        public class ColonyFacts
        {
            public int Dupes { get; set; }
            public float FoodKcal { get; set; }
            public float FoodKcalPerDupe { get; set; }
            public float MaxStress { get; set; }
            public int OxygenProducers { get; set; }
            public int Toilets { get; set; }
            public int Beds { get; set; }
            public int ResearchStations { get; set; }
            public int Batteries { get; set; }
            public int BreathableCells { get; set; }
            public bool AtmosphereScanned { get; set; }
            public int ReadyHarvestables { get; set; }
            public int ReachableHarvestables { get; set; }
            public int GeneratorCount { get; set; }
            public int GeneratorOperational { get; set; }
            public int ConsumerCount { get; set; }
            public int ConsumerPowered { get; set; }
            public float BatteryStoredPercent { get; set; }
            public bool PowerScanned { get; set; }
        }

        public const float DefaultFoodKcalPerDupe = 2000f;
        public const float StressWarningThreshold = 40f;
        public const float StressCriticalThreshold = 60f;
        public const int BreathableCellsPerDupe = 20;
        public const float BatteryLowPercent = 25f;

        public static List<ColonyRule> Evaluate(ColonyFacts f)
        {
            var rules = new List<ColonyRule>();
            if (f == null)
                return rules;

            float perDupe = f.FoodKcalPerDupe > 0f ? f.FoodKcalPerDupe : DefaultFoodKcalPerDupe;
            float foodNeed = f.Dupes * perDupe;

            if (f.Dupes <= 0)
                Add(rules, "dupes", "critical", 100, "No live duplicants detected.", "dupes");

            if (f.Dupes > 0 && f.FoodKcal < foodNeed)
            {
                Add(rules, "food", "critical", 95,
                    $"Food {Math.Round(f.FoodKcal, 1)} kcal is below the {Math.Round(foodNeed, 1)} kcal needed for {f.Dupes} duplicants.",
                    "food_shortage_response");
            }

            if (f.OxygenProducers == 0)
                Add(rules, "oxygen", "critical", 90, "No oxygen producer is built.", "oxygen");

            if (f.AtmosphereScanned && f.Dupes > 0 && f.BreathableCells < f.Dupes * BreathableCellsPerDupe)
            {
                Add(rules, "oxygen", "warning", 85,
                    $"Breathable space {f.BreathableCells} cells is thin for {f.Dupes} duplicants.",
                    "oxygen");
            }

            // Idle generators are only a problem once the batteries they are backing run low.
            // A manual generator sitting unused with charged batteries is normal play, not a
            // fault: duplicants only crank it on demand. Gating on charge keeps this rule from
            // nagging on a healthy colony.
            if (f.PowerScanned && f.GeneratorCount > 0 && f.GeneratorOperational == 0
                && f.ConsumerCount > 0 && f.BatteryStoredPercent < BatteryLowPercent)
            {
                Add(rules, "power", "warning", 80,
                    $"Batteries at {Math.Round(f.BatteryStoredPercent, 1)}% with all {f.GeneratorCount} generators idle and {f.ConsumerCount} consumers connected.",
                    "idle_generator_response");
            }

            if (f.ReadyHarvestables > 0 && f.ReachableHarvestables == 0)
            {
                Add(rules, "food_reachability", "warning", 75,
                    "Harvestable food exists but no duplicant can currently reach it.",
                    "reachability_failure");
            }

            if (f.MaxStress >= StressCriticalThreshold)
                Add(rules, "stress", "critical", 70, $"Max stress {Math.Round(f.MaxStress, 1)}%.", "dupes stress");
            else if (f.MaxStress >= StressWarningThreshold)
                Add(rules, "stress", "warning", 55, $"Max stress {Math.Round(f.MaxStress, 1)}%.", "dupes stress");

            if (f.Toilets == 0)
                Add(rules, "hygiene", "warning", 60, "No toilet is built.", "hygiene");

            if (f.Dupes > 0 && f.Beds < f.Dupes)
                Add(rules, "sleep", "info", 45, $"Beds {f.Beds}/{f.Dupes}.", "dupes");

            if (f.Batteries == 0)
                Add(rules, "power", "info", 35, "No battery is built.", "power");

            if (f.ResearchStations == 0)
                Add(rules, "research", "info", 30, "No research station is built.", "research");

            return rules.OrderByDescending(r => r.Priority).ToList();
        }

        private static void Add(List<ColonyRule> rules, string category, string severity, int priority, string message, string knowledgeQuery)
        {
            rules.Add(new ColonyRule
            {
                Category = category,
                Severity = severity,
                Priority = priority,
                Message = message,
                KnowledgeQuery = knowledgeQuery
            });
        }
    }
}
