using System;
using System.Collections.Generic;
using System.Linq;

namespace OniMcp.Tools
{
    /// <summary>
    /// Curated Oxygen Not Included strategy knowledge.
    /// Stores mechanics, formulas and judgement that the game itself never states.
    /// Values the game already exposes (build costs, tech ids, plant temperature bands)
    /// belong in <see cref="StrategyEntry.Derive"/> as a lookup path, not copied here,
    /// so the corpus cannot drift out of date against a game update.
    /// </summary>
    public static partial class StrategyKnowledgeTools
    {
        internal enum StrategyDlc
        {
            Base,
            SpacedOut,
            Both
        }

        internal class StrategyEntry
        {
            public string Id { get; set; }
            public string Category { get; set; }
            public StrategyDlc Dlc { get; set; }
            public string Title { get; set; }
            public List<string> Tags { get; set; }
            public List<string> Body { get; set; }
            public List<string> Formulas { get; set; }
            public List<string> Cautions { get; set; }
            public List<string> Derive { get; set; }
            public string Source { get; set; }
        }

        /// <summary>
        /// Every topic provider is listed here explicitly; partial classes cannot self-register.
        /// Adding a topic file means adding one line below.
        /// </summary>
        private static readonly List<StrategyEntry> AllEntriesCache = BuildAllEntries();

        private static List<StrategyEntry> BuildAllEntries()
        {
            var entries = new List<StrategyEntry>();
            entries.AddRange(MechanicsEntries());
            entries.AddRange(OxygenEntries());
            entries.AddRange(SurvivalEntries());
            entries.AddRange(BiomeEntries());
            entries.AddRange(ResearchEntries());
            entries.AddRange(GermEntries());
            entries.AddRange(ThermalEntries());
            entries.AddRange(FoodEntries());
            return entries;
        }

        internal static List<StrategyEntry> AllEntries()
        {
            return AllEntriesCache;
        }

        /// <summary>
        /// Minimum score for an entry to be cited as supporting reasoning.
        /// A bare category hit is worth 40, so anything below that is an incidental token match.
        /// Without this floor the advisor attached a 4-point match to a 108-point one and
        /// presented both as the reason behind a recommendation.
        /// </summary>
        internal const int SupportingKnowledgeMinScore = 40;

        /// <summary>
        /// Scored lookup for other tools that want to cite curated reasoning,
        /// e.g. the colony advisor joining a fired rule to the knowledge behind it.
        /// Pass minScore to drop weak matches; explicit user queries should keep it at 0 and let
        /// the visible score speak, but anything presented as authoritative should filter.
        /// </summary>
        internal static List<StrategyEntry> Lookup(string query, string category, int limit, int minScore)
        {
            string normalizedQuery = Normalize(query);
            string normalizedCategory = Normalize(category);
            return AllEntries()
                .Where(entry => string.IsNullOrEmpty(normalizedCategory) || Normalize(entry.Category) == normalizedCategory)
                .Select(entry => new { Entry = entry, Score = Score(entry, normalizedQuery) })
                .Where(item => string.IsNullOrEmpty(normalizedQuery) || item.Score > Math.Max(0, minScore))
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Entry.Id)
                .Take(Math.Max(1, limit))
                .Select(item => item.Entry)
                .ToList();
        }

        private static StrategyEntry Entry(
            string id,
            string category,
            StrategyDlc dlc,
            string title,
            IEnumerable<string> tags,
            IEnumerable<string> body,
            IEnumerable<string> formulas,
            IEnumerable<string> cautions,
            IEnumerable<string> derive,
            string source)
        {
            return new StrategyEntry
            {
                Id = id,
                Category = category,
                Dlc = dlc,
                Title = title,
                Tags = tags == null ? new List<string>() : tags.ToList(),
                Body = body == null ? new List<string>() : body.ToList(),
                Formulas = formulas == null ? new List<string>() : formulas.ToList(),
                Cautions = cautions == null ? new List<string>() : cautions.ToList(),
                Derive = derive == null ? new List<string>() : derive.ToList(),
                Source = source
            };
        }

        internal static bool MatchesDlc(StrategyEntry entry, string requested)
        {
            if (string.IsNullOrEmpty(requested) || requested == "any" || requested == "all")
                return true;
            if (entry.Dlc == StrategyDlc.Both)
                return true;
            if (requested == "base" || requested == "vanilla")
                return entry.Dlc == StrategyDlc.Base;
            if (requested == "spacedout" || requested == "spaced_out" || requested == "dlc" || requested == "so")
                return entry.Dlc == StrategyDlc.SpacedOut;
            return true;
        }

        internal static string DlcName(StrategyDlc dlc)
        {
            if (dlc == StrategyDlc.Base)
                return "base";
            if (dlc == StrategyDlc.SpacedOut)
                return "spaced_out";
            return "both";
        }

        internal static Dictionary<string, object> ToDictionary(StrategyEntry entry, int score, string detail)
        {
            var result = new Dictionary<string, object>
            {
                ["id"] = entry.Id,
                ["category"] = entry.Category,
                ["dlc"] = DlcName(entry.Dlc),
                ["title"] = entry.Title,
                ["score"] = score,
                ["tags"] = entry.Tags
            };

            if (detail == "brief")
            {
                result["summary"] = entry.Body.FirstOrDefault() ?? "";
                if (entry.Formulas.Count > 0)
                    result["formula"] = entry.Formulas[0];
                return result;
            }

            result["body"] = entry.Body;
            result["formulas"] = entry.Formulas;
            result["cautions"] = entry.Cautions;
            result["derive"] = entry.Derive;
            result["source"] = entry.Source;
            return result;
        }

        /// <summary>
        /// Weighted substring/token scoring carried over from the previous mechanics tool.
        /// Tags are bilingual on purpose: this matcher is substring based, so an entry with
        /// only English tags is invisible to a Chinese query.
        /// </summary>
        internal static int Score(StrategyEntry entry, string query)
        {
            if (string.IsNullOrEmpty(query))
                return 1;

            int score = 0;
            if (Contains(entry.Id, query))
                score += 80;
            if (Contains(entry.Title, query))
                score += 70;
            if (Contains(entry.Category, query))
                score += 40;
            if (entry.Tags.Any(tag => Contains(tag, query)))
                score += 35;
            if (entry.Body.Any(text => Contains(text, query)) || entry.Formulas.Any(text => Contains(text, query)))
                score += 20;

            foreach (string token in Tokenize(query))
            {
                if (Contains(entry.Id, token) || Contains(entry.Title, token))
                    score += 10;
                if (entry.Tags.Any(tag => Contains(tag, token)))
                    score += 8;
                if (entry.Body.Any(text => Contains(text, token))
                    || entry.Formulas.Any(text => Contains(text, token))
                    || entry.Cautions.Any(text => Contains(text, token)))
                    score += 4;
            }

            return score;
        }

        private static bool Contains(string value, string query)
        {
            return !string.IsNullOrEmpty(value)
                && !string.IsNullOrEmpty(query)
                && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IEnumerable<string> Tokenize(string value)
        {
            return (value ?? "")
                .Split(new[] { ' ', '\t', '\r', '\n', '_', '-', ',', '.', '/', ':', ';', '(', ')', '[', ']', '，', '。', '、' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(Normalize)
                .Where(token => token.Length > 1);
        }

        internal static string Normalize(string value)
        {
            return (value ?? "").Trim().ToLowerInvariant();
        }
    }
}
