using System.Collections.Generic;

namespace OniMcp.Tools
{
    /// <summary>
    /// Wave 1 pilot knowledge: oxygen production and the SPOM build set.
    /// These entries carry judgement the game never states (ordering, prerequisites,
    /// failure modes). Rates and power draws that the game does expose are referenced
    /// through Derive so they cannot drift out of date against a balance patch.
    /// </summary>
    public static partial class StrategyKnowledgeTools
    {
        private static List<StrategyEntry> OxygenEntries()
        {
            return new List<StrategyEntry>
            {
                Entry(
                    "spom_prerequisites",
                    "oxygen",
                    StrategyDlc.Both,
                    "SPOM 建造前提 / When a SPOM is worth building",
                    new[] { "SPOM", "制氧模块", "oxygen module", "自供电", "self powered", "电解器", "electrolyzer", "前提", "prerequisite" },
                    new[]
                    {
                        "SPOM 是自供电制氧模块：电解器产出的氢气驱动氢气发电机，发电量应超过模块自身的电解器和气泵功耗。",
                        "A SPOM only pays off once three things are already true: a secured water supply, researched gas plumbing and power tech, and enough duplicants that simple oxygen sources cannot keep up.",
                        "过早建 SPOM 是常见错误。前期用氧气扩散机、藻类缸或铁锈脱氧机更省资源，因为 SPOM 需要持续供水和管道基建。",
                        "判断顺序：先确认供水稳定，再确认管道和电力科技已解锁，最后才考虑 SPOM。"
                    },
                    new[]
                    {
                        "electrolyzer_output_split = 888g/s oxygen : 112g/s hydrogen per 1000g/s water",
                        "self_powered_check: hydrogen_generator_output > electrolyzer_draw + pump_draw"
                    },
                    new[]
                    {
                        "SPOM 的持续耗水很高，没有稳定水源时它会把水位抽干，比缺氧更早出问题。",
                        "Do not treat the 888:112 split as a licence to skip the power check: pump count drives whether the module is actually net positive."
                    },
                    new[]
                    {
                        "Electrolyzer / gas pump / hydrogen generator power draw and throughput: oni://buildings/defs.",
                        "Current water reserves: oni://resources/inventory.",
                        "Whether the required tech is unlocked: colony_control domain=management kind=research action=list.",
                        "Current oxygen production and duplicant count: colony_control domain=snapshot action=get."
                    },
                    "OniMcp strategy corpus, wave 1"
                ),
                Entry(
                    "spom_layout_principles",
                    "oxygen",
                    StrategyDlc.Both,
                    "SPOM 布局原则 / SPOM layout principles",
                    new[] { "SPOM", "布局", "layout", "电解器", "electrolyzer", "氢气", "hydrogen", "气泵", "gas pump", "分层", "stratification" },
                    new[]
                    {
                        "电解器放在房间上部，因为氢气密度最低会浮到顶部，氧气留在下方，这样两台气泵可以各自抽到接近纯净的气体。",
                        "Put the hydrogen pump at the very top of the chamber and the oxygen pump below it; the separation is done by gravity, not by filters, which is what makes the design cheap.",
                        "房间要密封，否则氢气会漏进基地，氧气泵会吸到混合气体。",
                        "常见替代做法是用气体过滤器代替分层，代价是额外功耗，优点是对房间密封要求低。"
                    },
                    null,
                    new[]
                    {
                        "分层依赖气体不被搅动。复制人穿行、门开合和额外气泵都会短暂打乱分层。",
                        "电解器超过额定气压会停止工作，房间过小会更早触发。"
                    },
                    new[]
                    {
                        "Gas layering order: see entry gas_density_order.",
                        "Electrolyzer inlet temperature ceiling: see entry electrolyzer_water_limit.",
                        "Live per-cell gas composition after building: world_editor command=zoom views=oxygen."
                    },
                    "OniMcp strategy corpus, wave 1"
                ),
                Entry(
                    "spom_power_budget",
                    "oxygen",
                    StrategyDlc.Both,
                    "SPOM 电力平衡 / SPOM power balance",
                    new[] { "SPOM", "电力", "power", "自供电", "self powered", "氢气发电机", "hydrogen generator", "功耗", "draw" },
                    new[]
                    {
                        "自供电的含义是模块内部发电量大于模块自身功耗，多余电力才能外送。",
                        "The module is only self-powered if hydrogen generator output exceeds the electrolyzer plus every pump you added. Each extra pump erodes the surplus.",
                        "接入基地电网前先确认导线额定值，避免过载烧线。"
                    },
                    new[] { "surplus = hydrogen_generator_output - (electrolyzer_draw + pumps * pump_draw)" },
                    new[]
                    {
                        "氢气发电机不是连续满载运行，产氢速率不足时它会间歇停机，实际平均功率低于额定值。",
                        "别把 SPOM 当作基地主电源；它的设计目标是制氧，供电只是副产品。"
                    },
                    new[]
                    {
                        "Wire and transformer capacity: see entry power_wire_limits.",
                        "Live grid load and battery state: oni://power/summary.",
                        "Exact building power draw in this game version: oni://buildings/defs."
                    },
                    "OniMcp strategy corpus, wave 1"
                ),
                Entry(
                    "spom_water_budget",
                    "oxygen",
                    StrategyDlc.Both,
                    "SPOM 耗水预算 / SPOM water budget",
                    new[] { "SPOM", "水", "water", "耗水", "consumption", "供水", "supply", "电解器", "electrolyzer" },
                    new[]
                    {
                        "电解器满载时按每秒 1kg 水估算，一个周期 600 秒即约 600kg 水，这是 SPOM 最大的隐性成本。",
                        "A SPOM converts a water problem into an oxygen solution. If the water source is not renewable, it moves the failure a few dozen cycles into the future instead of solving it.",
                        "可持续水源包括净水器回收的污水、蒸汽间歇泉和水泵抽取的自然水体；只靠初始水池不足以长期运行。"
                    },
                    new[] { "water_per_cycle ~= electrolyzer_rate * 600s (about 600kg at full duty)" },
                    new[]
                    {
                        "电解器只在气压未超限时运行，实际耗水常低于满载值；用实际存档数据校准，不要直接按满载规划水源。",
                        "污水必须先经净水器处理才能进电解器。"
                    },
                    new[]
                    {
                        "Current water and polluted water stock: oni://resources/inventory.",
                        "Electrolyzer consumption rate in this game version: oni://buildings/defs."
                    },
                    "OniMcp strategy corpus, wave 1"
                )
            };
        }
    }
}
