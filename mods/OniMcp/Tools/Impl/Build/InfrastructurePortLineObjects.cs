using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OniMcp.Core;
using OniMcp.Support;
using UnityEngine;

namespace OniMcp.Tools
{
    public static partial class InfrastructurePortReadTools
    {
        private static Dictionary<string, object> Port(string layer, string role, string label, int cell, ObjectLayer[] layers, Dictionary<string, object> extra)
        {
            var result = new Dictionary<string, object>
            {
                ["layer"] = layer,
                ["role"] = role,
                ["label"] = label,
                ["cell"] = CellObject(cell),
                ["hasLine"] = HasLayer(cell, layers),
                ["line"] = LineObject(cell, layers)
            };
            foreach (var item in extra)
                result[item.Key] = item.Value;
            return result;
        }

        private static Dictionary<string, object> PowerStatus(GameObject go, string role, EnergyConsumer consumer = null, Generator generator = null)
        {
            var battery = go.GetComponent<Battery>();
            return new Dictionary<string, object>
            {
                ["connected"] = HasPowerConnection(go),
                ["circuitId"] = PowerCircuit(go),
                ["loadW"] = consumer != null ? (object)Math.Round(ToolUtil.SafeFloat(consumer.WattsNeededWhenActive), 1) : null,
                ["generatorW"] = generator != null ? (object)Math.Round(ToolUtil.SafeFloat(generator.WattageRating), 1) : null,
                ["batteryJ"] = battery != null ? (object)Math.Round(ToolUtil.SafeFloat(battery.JoulesAvailable), 1) : null,
                ["roleHint"] = role
            };
        }

        private static bool HasPowerConnection(GameObject go)
        {
            var consumer = go.GetComponent<EnergyConsumer>();
            if (consumer != null)
                return consumer.CircuitID != ushort.MaxValue;
            var generator = go.GetComponent<Generator>();
            if (generator != null)
                return generator.CircuitID != ushort.MaxValue;
            var battery = go.GetComponent<Battery>();
            return battery != null && battery.CircuitID != ushort.MaxValue;
        }

        private static object PowerCircuit(GameObject go)
        {
            var consumer = go.GetComponent<EnergyConsumer>();
            if (consumer != null)
                return consumer.CircuitID == ushort.MaxValue ? "-1" : consumer.CircuitID.ToString();
            var generator = go.GetComponent<Generator>();
            if (generator != null)
                return generator.CircuitID == ushort.MaxValue ? "-1" : generator.CircuitID.ToString();
            var battery = go.GetComponent<Battery>();
            return battery == null || battery.CircuitID == ushort.MaxValue ? "-1" : battery.CircuitID.ToString();
        }

        private static bool Wants(string kind, string layer)
        {
            return kind == "all" || kind == layer || (kind == "conduit" && (layer == "liquid" || layer == "gas"));
        }

        private static string NormalizeKind(string kind)
        {
            kind = (kind ?? "all").Trim().ToLowerInvariant();
            if (kind == "water" || kind == "liquid_conduit" || kind == "pipe") return "liquid";
            if (kind == "gas_conduit" || kind == "vent") return "gas";
            if (kind == "automation" || kind == "signal") return "logic";
            if (kind == "solid" || kind == "conveyor" || kind == "shipping") return "rail";
            if (kind == "electric" || kind == "wire") return "power";
            return string.IsNullOrEmpty(kind) ? "all" : kind;
        }

        private static ObjectLayer[] LayersFor(string layer)
        {
            return layer == "gas" ? GasLayers : LiquidLayers;
        }

        private static bool HasLayer(int cell, ObjectLayer[] layers)
        {
            return Grid.IsValidCell(cell) && layers.Any(layer => Grid.Objects[cell, (int)layer] != null);
        }

        private static Dictionary<string, object> LineObject(int cell, ObjectLayer[] layers)
        {
            var dirs = new List<string>();
            var to = new List<Dictionary<string, object>>();
            AddLineNeighbor(cell, layers, "U", 0, 1, dirs, to);
            AddLineNeighbor(cell, layers, "D", 0, -1, dirs, to);
            AddLineNeighbor(cell, layers, "L", -1, 0, dirs, to);
            AddLineNeighbor(cell, layers, "R", 1, 0, dirs, to);

            return new Dictionary<string, object>
            {
                ["glyph"] = LineGlyph(dirs),
                ["dirs"] = dirs.Count == 0 ? "." : string.Join("", dirs.ToArray()),
                ["to"] = to,
                ["bridge"] = BridgeId(cell)
            };
        }

        private static void AddLineNeighbor(
            int cell,
            ObjectLayer[] layers,
            string dir,
            int dx,
            int dy,
            List<string> dirs,
            List<Dictionary<string, object>> to)
        {
            int neighbor = Grid.XYToCell(Grid.CellColumn(cell) + dx, Grid.CellRow(cell) + dy);
            if (!HasLayer(neighbor, layers))
                return;
            dirs.Add(dir);
            to.Add(CellObject(neighbor));
        }

        private static string LineGlyph(List<string> dirs)
        {
            bool u = dirs.Contains("U");
            bool d = dirs.Contains("D");
            bool l = dirs.Contains("L");
            bool r = dirs.Contains("R");
            int count = (u ? 1 : 0) + (d ? 1 : 0) + (l ? 1 : 0) + (r ? 1 : 0);
            if (count == 0) return ".";
            if (count == 1) return "*";
            if (count == 4) return "十";
            if (u && d && !l && !r) return "|";
            if (l && r && !u && !d) return "一";
            if (u && r && !d && !l) return "└";
            if (u && l && !d && !r) return "┘";
            if (d && r && !u && !l) return "┌";
            if (d && l && !u && !r) return "┐";
            if (u && l && r && !d) return "┴";
            if (d && l && r && !u) return "┬";
            if (u && d && r && !l) return "├";
            if (u && d && l && !r) return "┤";
            return "?";
        }

        private static string BridgeId(int cell)
        {
            var go = Grid.IsValidCell(cell) ? Grid.Objects[cell, (int)ObjectLayer.Building] : null;
            if (go == null)
                return null;
            string id = go.GetComponent<BuildingComplete>()?.name ?? go.name;
            return id.IndexOf("Bridge", StringComparison.OrdinalIgnoreCase) >= 0 ? id : null;
        }

        private static Dictionary<string, object> CellObject(int cell)
        {
            if (!Grid.IsValidCell(cell))
                return new Dictionary<string, object> { ["valid"] = false };
            return new Dictionary<string, object>
            {
                ["x"] = Grid.CellColumn(cell),
                ["y"] = Grid.CellRow(cell),
                ["cell"] = cell,
                ["valid"] = true
            };
        }

        private static BuildingDef ResolveBuildingDef(GameObject go)
        {
            var kpid = go.GetComponent<KPrefabID>();
            string id = kpid?.PrefabTag.Name ?? go.name;
            return string.IsNullOrWhiteSpace(id) ? null : Assets.GetBuildingDef(id);
        }
    }
}
