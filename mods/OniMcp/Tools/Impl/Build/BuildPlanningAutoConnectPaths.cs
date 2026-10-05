using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace OniMcp.Tools
{
    public static partial class BuildPlanningTools
    {
        private static List<CellCoord> ParsePathPoints(JToken token)
        {
            var result = new List<CellCoord>();
            var array = token as JArray;
            if (array == null)
                return result;

            foreach (var item in array)
            {
                int? x = null;
                int? y = null;
                var pair = item as JArray;
                if (pair != null && pair.Count >= 2)
                {
                    int parsedX;
                    int parsedY;
                    if (int.TryParse(pair[0]?.ToString(), out parsedX) && int.TryParse(pair[1]?.ToString(), out parsedY))
                    {
                        x = parsedX;
                        y = parsedY;
                    }
                }
                else
                {
                    var obj = item as JObject;
                    if (obj != null)
                    {
                        int parsedX;
                        int parsedY;
                        if (int.TryParse(obj["x"]?.ToString(), out parsedX) && int.TryParse(obj["y"]?.ToString(), out parsedY))
                        {
                            x = parsedX;
                            y = parsedY;
                        }
                    }
                }

                if (x.HasValue && y.HasValue)
                    result.Add(new CellCoord(x.Value, y.Value));
            }
            return result;
        }

        private static bool AddManhattanSegment(List<CellCoord> path, CellCoord from, CellCoord to, int maxCells, out string error)
        {
            error = null;

            long dx = Math.Abs((long)to.x - from.x);
            long dy = Math.Abs((long)to.y - from.y);
            long cellsToAdd = dx + dy + 1;
            if (path.Count > 0 && path[path.Count - 1].x == from.x && path[path.Count - 1].y == from.y)
                cellsToAdd--;

            long resultingCount = (long)path.Count + cellsToAdd;
            if (resultingCount > maxCells)
            {
                error = $"Path too large: {resultingCount} cells, maxCells={maxCells}";
                return false;
            }

            int x = from.x;
            int y = from.y;
            AddPathPoint(path, x, y);
            while (x != to.x)
            {
                x += to.x > x ? 1 : -1;
                AddPathPoint(path, x, y);
            }
            while (y != to.y)
            {
                y += to.y > y ? 1 : -1;
                AddPathPoint(path, x, y);
            }
            return true;
        }

        private static void AddPathPoint(List<CellCoord> path, int x, int y)
        {
            if (path.Count > 0 && path[path.Count - 1].x == x && path[path.Count - 1].y == y)
                return;
            path.Add(new CellCoord(x, y));
        }

        private static List<Dictionary<string, object>> BuildPathSegments(List<CellCoord> path)
        {
            var segments = new List<Dictionary<string, object>>();
            if (path == null || path.Count == 0)
                return segments;

            CellCoord start = path[0];
            CellCoord previous = path[0];
            int dx = 0;
            int dy = 0;

            for (int i = 1; i < path.Count; i++)
            {
                var current = path[i];
                int nextDx = Math.Sign(current.x - previous.x);
                int nextDy = Math.Sign(current.y - previous.y);
                if (i > 1 && (nextDx != dx || nextDy != dy))
                {
                    segments.Add(PathSegment(start, previous));
                    start = previous;
                }

                dx = nextDx;
                dy = nextDy;
                previous = current;
            }

            segments.Add(PathSegment(start, previous));
            return segments;
        }

        private static Dictionary<string, object> PathSegment(CellCoord start, CellCoord end)
        {
            return new Dictionary<string, object>
            {
                ["from"] = new { x = start.x, y = start.y },
                ["to"] = new { x = end.x, y = end.y },
                ["length"] = Math.Abs(end.x - start.x) + Math.Abs(end.y - start.y) + 1
            };
        }
    }
}
