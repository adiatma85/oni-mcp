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
        private static JArray RoomShellAnchors(RoomTemplateAnchor anchor, bool doorOnLeft = false, bool omitLeftWall = false, bool omitRightWall = false)
        {
            var anchors = new JArray();
            int left = anchor.X;
            int right = anchor.X + anchor.Width - 1;
            int bottom = anchor.Y;
            int top = anchor.Y + anchor.Height - 1;

            for (int x = left; x <= right; x++)
            {
                anchors.Add(Anchor(x, bottom));
                anchors.Add(Anchor(x, top));
            }

            for (int y = bottom + 1; y < top; y++)
            {
                if (!omitLeftWall && (!doorOnLeft || (y != bottom + 1 && y != bottom + 2)))
                    anchors.Add(Anchor(left, y));
                if (!omitRightWall && (doorOnLeft || (y != bottom + 1 && y != bottom + 2)))
                    anchors.Add(Anchor(right, y));
            }

            return anchors;
        }

 private static JArray OneAnchor(int x, int y)
 {
 return new JArray(Anchor(x, y));
 }

 private static JArray HorizontalRunAnchors(int x, int y, int length)
 {
 var anchors = new JArray();
 for (int offset = 0; offset < Math.Max(0, length); offset++)
 anchors.Add(Anchor(x + offset, y));
 return anchors;
 }

 private static JArray VerticalWallAnchors(int x, int y, int height)
 {
 var anchors = new JArray();
 for (int offset = 0; offset < height; offset++)
 anchors.Add(Anchor(x, y + offset));
 return anchors;
 }

 private static JObject Anchor(int x, int y)
        {
            return new JObject { ["x"] = x, ["y"] = y };
        }

        private static JArray BuildRoomTemplateRoomSummary(string kind, RoomTemplateAnchor anchor)
        {
            if (kind != "starter")
                return new JArray(RoomSummary(kind, anchor));

            int roomWidth = Math.Max(7, (anchor.Width - 1) / 2);
            return new JArray
            {
                RoomSummary("toilet", anchor.With(anchor.X, anchor.Y, roomWidth, anchor.Height)),
                RoomSummary("lab", anchor.With(anchor.X + roomWidth + 1, anchor.Y, roomWidth, anchor.Height))
            };
        }

        private static JObject RoomSummary(string kind, RoomTemplateAnchor anchor)
        {
            RoomTemplateDefinition definition = GetRoomTemplateDefinition(kind);
            var core = new JArray();
            var coreCells = new JArray();
            if (definition != null)
            {
                foreach (RoomTemplatePlacement placement in definition.Placements)
                {
                    int px, py;
                    ResolvePlacementCell(anchor, placement, out px, out py);
                    core.Add(placement.PrefabId);
                    coreCells.Add(CoreCell(placement.PrefabId, px, py));
                }
            }
            return new JObject
            {
                ["kind"] = kind,
                ["rect"] = new JObject
                {
                    ["x1"] = anchor.X,
                    ["y1"] = anchor.Y,
                    ["x2"] = anchor.X + anchor.Width - 1,
                    ["y2"] = anchor.Y + anchor.Height - 1
                },
                ["interiorDig"] = new JObject
                {
                    ["x1"] = anchor.X + 1,
                    ["y1"] = anchor.Y + 1,
                    ["x2"] = anchor.X + anchor.Width - 2,
                    ["y2"] = anchor.Y + anchor.Height - 2
                },
                ["coreBuildings"] = core,
                ["coreCells"] = coreCells
            };
        }

        private static JObject CoreCell(string prefabId, int x, int y)
        {
            return new JObject
            {
                ["prefabId"] = prefabId,
                ["x"] = x,
                ["y"] = y,
                ["cellPath"] = "/active/map/cell_" + x + "_" + y + ".md"
            };
        }
    }
}
