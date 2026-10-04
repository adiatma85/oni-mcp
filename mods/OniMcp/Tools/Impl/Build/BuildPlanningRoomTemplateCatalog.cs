using System;
using System.Collections.Generic;
using System.Linq;

namespace OniMcp.Tools
{
    /// <summary>
    /// Data table describing what each room template contains.
    ///
    /// Replaces an if/else chain whose final `else` was a silent catch-all that emitted a
    /// ResearchCenter: any kind added without its own branch quietly became a lab. Driving
    /// placements from a table makes an unknown kind an explicit error and makes each new
    /// build set a data entry rather than a code change.
    ///
    /// Offsets are relative to the room anchor, which is the lower-left cell of the room
    /// including its wall. Each placement offset is the building's own lower-left footprint
    /// cell, matching how building_control planning interprets an anchor. Footprints below
    /// were read from the running game, not assumed: HydrogenGenerator is 4x3, Electrolyzer
    /// and GasPump are 2x2.
    /// </summary>
    public static partial class BuildPlanningTools
    {
        private class RoomTemplatePlacement
        {
            public string PrefabId { get; set; }
            public int Dx { get; set; }
            public int Dy { get; set; }
            /// <summary>Footprint width, used to keep the layout inside the room and self-document.</summary>
            public int Width { get; set; }
            public int Height { get; set; }
            public string Role { get; set; }
        }

        private class RoomTemplateDefinition
        {
            public string Kind { get; set; }
            public int DefaultWidth { get; set; }
            public int DefaultHeight { get; set; }
            public string LayoutPurpose { get; set; }
            public List<RoomTemplatePlacement> Placements { get; set; }
            /// <summary>Horizontal wire lane offset from the room anchor, or null for none.</summary>
            public int? WireLaneDy { get; set; }
            public string Note { get; set; }
        }

        private static readonly Dictionary<string, RoomTemplateDefinition> RoomTemplateCatalog =
            BuildRoomTemplateCatalog();

        private static Dictionary<string, RoomTemplateDefinition> BuildRoomTemplateCatalog()
        {
            var catalog = new Dictionary<string, RoomTemplateDefinition>(StringComparer.OrdinalIgnoreCase);

            catalog["toilet"] = new RoomTemplateDefinition
            {
                Kind = "toilet",
                DefaultWidth = 8,
                DefaultHeight = 4,
                LayoutPurpose = "bathroom",
                Placements = new List<RoomTemplatePlacement>
                {
                    Place("Outhouse", 2, 1, 1, 2, "waste"),
                    // Dx=-2 reproduces the previous X + max(4, width-3): width is floored at 8,
                    // so both expressions resolve to width-3 and the basin keeps its old cell.
                    Place("WashBasin", -2, 1, 1, 2, "hygiene")
                }
            };

            catalog["lab"] = new RoomTemplateDefinition
            {
                Kind = "lab",
                DefaultWidth = 8,
                DefaultHeight = 4,
                LayoutPurpose = "lab",
                Placements = new List<RoomTemplatePlacement>
                {
                    Place("ResearchCenter", 2, 1, 3, 2, "research")
                }
            };

            catalog["washroom"] = new RoomTemplateDefinition
            {
                Kind = "washroom",
                DefaultWidth = 8,
                DefaultHeight = 5,
                LayoutPurpose = "bathroom",
                Placements = new List<RoomTemplatePlacement>
                {
                    Place("FlushToilet", 2, 1, 2, 3, "waste"),
                    Place("WashSink", -3, 1, 2, 3, "hygiene")
                },
                Note = "Plumbed washroom with FlushToilet and WashSink. Connect clean water intake and polluted water output (closed loop to WaterPurifier with overflow bridge recommended)."
            };

            catalog["farm"] = new RoomTemplateDefinition
            {
                Kind = "farm",
                DefaultWidth = 12,
                DefaultHeight = 4,
                LayoutPurpose = "farm",
                Placements = new List<RoomTemplatePlacement>
                {
                    Place("PlanterBox", 1, 1, 1, 1, "crop"),
                    Place("PlanterBox", 2, 1, 1, 1, "crop"),
                    Place("PlanterBox", 3, 1, 1, 1, "crop"),
                    Place("PlanterBox", 4, 1, 1, 1, "crop"),
                    Place("PlanterBox", 5, 1, 1, 1, "crop"),
                    Place("PlanterBox", 6, 1, 1, 1, "crop"),
                    Place("PlanterBox", 7, 1, 1, 1, "crop"),
                    Place("PlanterBox", 8, 1, 1, 1, "crop"),
                    Place("PlanterBox", 9, 1, 1, 1, "crop"),
                    Place("PlanterBox", 10, 1, 1, 1, "crop")
                },
                Note = "Standard greenhouse/farm room with planter boxes on the floor. Auto-harvest is enabled by default. Set crops with seed parameter or colony_control domain=bio bioDomain=farming action=set."
            };

            // Self-Powered Oxygen Module.
            // Vertical order follows gas stratification rather than convenience: hydrogen is the
            // lightest gas so its pump sits at the ceiling, oxygen collects lower so its pump sits
            // near the floor. The electrolyzer is OnFloor and feeds both. Room height is driven by
            // the 4x3 hydrogen generator plus clearance for the ceiling pump.
            catalog["spom"] = new RoomTemplateDefinition
            {
                Kind = "spom",
                DefaultWidth = 12,
                DefaultHeight = 9,
                LayoutPurpose = "power",
                WireLaneDy = 4,
                Placements = new List<RoomTemplatePlacement>
                {
                    Place("HydrogenGenerator", 1, 1, 4, 3, "burns hydrogen to power the module"),
                    Place("Electrolyzer", 6, 1, 2, 2, "splits water into oxygen and hydrogen"),
                    Place("GasPump", 9, 1, 2, 2, "oxygen pump, low where oxygen settles"),
                    Place("GasPump", 9, 6, 2, 2, "hydrogen pump, at the ceiling where hydrogen rises")
                },
                Note = "Places the chamber and core machines only. Gas and liquid piping is deliberately left out: correct routing depends on each building's port cells and on where your water supply and oxygen distribution already run, which a blind template cannot know. Connect water in, oxygen out and hydrogen to the generator as a follow-up step."
            };

            return catalog;
        }

        private static RoomTemplatePlacement Place(string prefabId, int dx, int dy, int width, int height, string role)
        {
            return new RoomTemplatePlacement
            {
                PrefabId = prefabId,
                Dx = dx,
                Dy = dy,
                Width = width,
                Height = height,
                Role = role
            };
        }

        /// <summary>
        /// Which of a template's buildings the colony cannot build yet.
        ///
        /// Without this the template half-succeeds in silence: a SPOM asked for before
        /// Electrolyzer and HydrogenGenerator are researched still digs the room, builds the
        /// shell and places both gas pumps, then drops the two machines that make it a SPOM.
        /// Reporting the gap is far more useful than a per-anchor failure count.
        /// </summary>
        private static List<string> LockedTemplatePrefabs(RoomTemplateDefinition definition)
        {
            var locked = new List<string>();
            if (definition == null || definition.Placements == null)
                return locked;

            foreach (RoomTemplatePlacement placement in definition.Placements)
            {
                if (locked.Contains(placement.PrefabId))
                    continue;
                string resolvedId;
                string resolveError;
                BuildingDef def = ResolveBuildingDef(placement.PrefabId, out resolvedId, out resolveError);
                if (def != null && !IsTechUnlocked(def))
                    locked.Add(placement.PrefabId);
            }
            return locked;
        }

        private static RoomTemplateDefinition GetRoomTemplateDefinition(string kind)
        {
            RoomTemplateDefinition definition;
            return RoomTemplateCatalog.TryGetValue(kind ?? string.Empty, out definition) ? definition : null;
        }

        /// <summary>
        /// Resolve a placement offset into an absolute cell.
        /// A negative Dx is measured from the room's right wall, so a fixture can sit at the far
        /// end of a room whose width the caller widened.
        /// </summary>
        private static void ResolvePlacementCell(RoomTemplateAnchor anchor, RoomTemplatePlacement placement, out int x, out int y)
        {
            x = placement.Dx >= 0
                ? anchor.X + placement.Dx
                : anchor.X + Math.Max(1, anchor.Width - 1 + placement.Dx);
            y = anchor.Y + placement.Dy;
        }
    }
}
