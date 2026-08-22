using System.Collections.Generic;

namespace OniMcp.Tools
{
    /// <summary>
    /// Core mechanics, thresholds and formulas.
    /// Restored from the mechanics tool removed in 8d50350 and re-tagged bilingually.
    /// This data is static C# and does not touch the in-game codex database, which is
    /// the part that was crash-prone on this runtime.
    /// </summary>
    public static partial class StrategyKnowledgeTools
    {
        private static List<StrategyEntry> MechanicsEntries()
        {
            return new List<StrategyEntry>
            {
                Entry(
                    "gas_density_order",
                    "gas_fluid",
                    StrategyDlc.Both,
                    "气体分层顺序 / Gas stratification order",
                    new[] { "气体", "gas", "分层", "stratification", "密度", "density", "氢气", "hydrogen", "二氧化碳", "carbon dioxide", "氯气", "chlorine" },
                    new[]
                    {
                        "常用分层从上到下：氢气、天然气、氧气、污染氧、二氧化碳、氯气。",
                        "Top to bottom: hydrogen, natural gas, oxygen, polluted oxygen, carbon dioxide, chlorine.",
                        "用于气体隔离、保鲜格、气泵口位置和排 CO2 坑判断。"
                    },
                    null,
                    new[] { "分层会被气体流动、泵、门、热胀冷缩和小质量乱流短暂打乱。" },
                    new[] { "Per-cell gas identity and mass: world_editor command=read on the target cell, or oni://world/text-map." },
                    "缺氧机制速查/U59"
                ),
                Entry(
                    "forced_phase_change",
                    "thermal",
                    StrategyDlc.Both,
                    "强制相变边界 / Forced phase change thresholds",
                    new[] { "相变", "phase change", "闪蒸", "flash", "隔热体", "insulation", "深渊晶石", "abyssalite", "熔融钨", "tungsten" },
                    new[]
                    {
                        "高温隔热体超过目标液体蒸发点约 3C，且液体本身低于相变点约 3C、空间不少于 2 格时，可触发约 5kg 液体强制相变。",
                        "高温气体超过深渊晶石熔点约 3C 时，可把约 5kg 深渊晶石转为熔融钨。"
                    },
                    new[]
                    {
                        "hot_insulated_solid_temp > liquid_vaporization_temp + 3C",
                        "liquid_temp < liquid_vaporization_temp - 3C",
                        "phase_space_cells >= 2",
                        "insulated_solid_delta_temp < 10C"
                    },
                    new[] { "这类机制常用于特殊工业和玩具级模块，实装前必须留测试余量。" },
                    null,
                    "缺氧机制速查"
                ),
                Entry(
                    "liquid_flow_thresholds",
                    "gas_fluid",
                    StrategyDlc.Both,
                    "液体流动与液滴推动 / Liquid flow and droplet push",
                    new[] { "液体", "liquid", "流动", "flow", "水", "water", "油", "oil", "液滴", "droplet", "推动", "push" },
                    new[]
                    {
                        "单格液体存在最小流动质量；水约 40g，油约 400g。",
                        "液滴被推开时，会按当前质量的一部分产生侧向推动，常用于理解液门、液推和微量液体布局。"
                    },
                    new[] { "water_min_flow_mass ~= 40g", "oil_min_flow_mass ~= 400g", "pushed_drop_split ~= 12.5%" },
                    new[] { "微量液体结构对保存/加载、复制人经过和新液体落入很敏感，实际模块要留维护空间。" },
                    null,
                    "缺氧机制速查"
                ),
                Entry(
                    "insulated_tile_conductivity",
                    "thermal",
                    StrategyDlc.Both,
                    "隔热砖有效导热率 / Insulated tile effective conductivity",
                    new[] { "隔热砖", "insulated tile", "导热率", "conductivity", "换热", "heat transfer", "火成岩", "igneous", "陶瓷", "ceramic" },
                    new[]
                    {
                        "隔热砖有效导热率按材料导热率乘以 (2/255)^2 估算。",
                        "火成岩材料 k=2 时，隔热砖有效 k 约为 0.000123。"
                    },
                    new[] { "effective_k = material_k * (2 / 255)^2", "igneous_insulated_tile_k ~= 2 * 0.0000615 = 0.000123" },
                    new[] { "极端温差下仍会换热；隔热砖不是绝对绝热。" },
                    new[] { "Per-material thermal conductivity: read the element definition rather than assuming a constant." },
                    "缺氧机制速查"
                ),
                Entry(
                    "cell_heat_exchange",
                    "thermal",
                    StrategyDlc.Both,
                    "方格换热公式 / Cell heat exchange formula",
                    new[] { "热量", "heat", "换热", "heat exchange", "公式", "formula", "导热率", "conductivity", "tick" },
                    new[]
                    {
                        "常用方格换热估算由温差、tick 时间、导热率和双方换热系数组成。",
                        "一个游戏换热 tick 通常按 0.2 秒估算；固体-气体换热常用系数乘积 25。"
                    },
                    new[] { "q = deltaT * deltaTime * k * s1 * s2", "deltaTime = 0.2s", "solid_gas_s1_s2 = 25" },
                    new[] { "不同相态和接触对象有不同系数，模块计算要按实际接触面复核。" },
                    null,
                    "缺氧机制速查"
                ),
                Entry(
                    "electrolyzer_water_limit",
                    "oxygen",
                    StrategyDlc.Both,
                    "电解器极限入水温度 / Electrolyzer max inlet water temperature",
                    new[] { "电解器", "electrolyzer", "制氧", "oxygen", "入水温度", "inlet temperature", "超温", "overheat", "SPOM", "101.3" },
                    new[]
                    {
                        "按产物带走热量估算，电解器理论极限入水温度约 101.3C。",
                        "工程上建议把入水温度控制在 100C 以下，避免波动导致超温损坏。",
                        "Theoretical inlet ceiling is about 101.3C; design for under 100C so fluctuation does not overheat the build."
                    },
                    new[] { "0.888 * (102.4 - a) * 1.005 + 0.112 * (102.4 - a) * 2.4 = 1.25", "a ~= 101.3C" },
                    new[] { "设备材料、周边环境和水包温度波动会改变风险，别按理论值贴边运行。" },
                    new[] { "Electrolyzer overheat temperature and power draw: oni://buildings/defs, or read_control domain=buildings for the placed instance." },
                    "缺氧机制速查"
                ),
                Entry(
                    "high_pressure_oxygen",
                    "oxygen",
                    StrategyDlc.Both,
                    "高压制氧方案对比 / High-pressure oxygen approaches",
                    new[] { "高压制氧", "high pressure oxygen", "斜推", "diagonal push", "直推", "straight push", "液推", "liquid push", "制氧模块", "SPOM" },
                    new[]
                    {
                        "斜角气推依赖气体斜向推挤，稳定性通常较高，但需要正确引导气体。",
                        "斜角液推和直推可行但调试敏感；直推通常需要油层达到约 300g/格以上。"
                    },
                    new[] { "straight_push_oil_layer >= 300g_per_tile" },
                    new[] { "新手或长期档优先用容错高的方案，不要把液滴质量压到极限。" },
                    null,
                    "缺氧机制速查"
                ),
                Entry(
                    "trace_gas_food_preservation",
                    "food",
                    StrategyDlc.Both,
                    "微量气体保鲜 / Trace-gas food preservation",
                    new[] { "保鲜", "preservation", "食物", "food", "微量气体", "trace gas", "氢气", "hydrogen", "深冻", "deep freeze" },
                    new[]
                    {
                        "微克级气体可让食物所在格处于指定气体环境，并降低与邻格换热影响。",
                        "常用做法是用 5-10 微克氢气配合低温导热管，把食物温度压到 -18C 以下实现永久保鲜。"
                    },
                    new[] { "hydrogen_mass ~= 5-10 microgram", "food_temp < -18C", "example_pipe_temp = -100C steel radiant pipe" },
                    new[] { "检查食物实际格温和气体是否被挤走；复制人取放、掉落和清扫可能破坏布局。" },
                    new[] { "Current food stock and spoilage state: oni://resources/food." },
                    "缺氧机制速查"
                ),
                Entry(
                    "critter_overcrowding_happiness",
                    "ranching",
                    StrategyDlc.Both,
                    "小动物过度拥挤幸福度 / Critter overcrowding happiness",
                    new[] { "养殖", "ranching", "小动物", "critter", "过度拥挤", "overcrowding", "幸福度", "happiness", "繁殖", "breeding" },
                    new[]
                    {
                        "可用房间大小、单只快乐空间需求和当前动物数量估算拥挤幸福度。",
                        "结果影响繁殖、掉毛等养殖产出相关行为。"
                    },
                    new[] { "happiness = (int(room_size / happy_space_per_critter) - critter_count + 1) - 5" },
                    new[] { "蛋、幼体和不同物种的计数口径要结合游戏内状态确认。" },
                    new[] { "Per-species space requirement and current room size: read the critter definition and oni://rooms/list rather than assuming." },
                    "缺氧机制速查/U48+"
                ),
                Entry(
                    "arbor_tree_dense_planting",
                    "farming",
                    StrategyDlc.Both,
                    "乔木树密植节奏 / Arbor tree dense planting rhythm",
                    new[] { "种植", "planting", "乔木树", "arbor tree", "密植", "dense planting", "乙醇", "ethanol", "木料", "lumber" },
                    new[]
                    {
                        "乔木树常见密植节奏是在 7 格宽度内放 3 棵，按种一格、空一格的节奏排布。",
                        "这个条目只保存布局节奏；灌溉、温度、光照和收获自动化仍要按当前存档设计。"
                    },
                    new[] { "pattern_width_7 = tree_empty_tree_empty_tree" },
                    new[] { "实际可种位置受自然砖、花盆/农砖、树干生长空间和自动化收割路径影响。" },
                    null,
                    "缺氧机制速查"
                ),
                Entry(
                    "pip_planting_rule_note",
                    "farming",
                    StrategyDlc.Both,
                    "树鼠种植规则提示 / Pip planting rule note",
                    new[] { "树鼠", "pip", "种植", "planting", "自然种植", "wild planting", "一格无限种植" },
                    new[]
                    {
                        "树鼠种植依赖周围已有植物、目标格类型、种子和空间判定；特殊布局可做高密度自然种植。",
                        "本条目只保留机制提示，具体模块应在当前存档里逐格验证。"
                    },
                    null,
                    new[] { "树鼠路径、可达性、种子选择和既有植物范围会影响判定；不要只凭单个坐标下结论。" },
                    null,
                    "缺氧机制速查"
                ),
                Entry(
                    "automation_priority_stack",
                    "automation",
                    StrategyDlc.Both,
                    "差事优先级层级 / Errand priority stack",
                    new[] { "优先级", "priority", "差事", "errand", "自动化", "automation", "火箭", "rocket", "复制人", "duplicant" },
                    new[]
                    {
                        "差事排序不是只看建筑数字优先级；火箭、个人需求、急迫度和箭头优先级会压过普通 1-9 数字。",
                        "理解该层级有助于解释为什么复制人不去做看似高优先级的任务。"
                    },
                    new[] { "rocket_entry(400) > personal_need(200) > urgent(100) > arrow_priority(50..10) > numeric_priority(9..1) > hidden_priority(<1)" },
                    new[] { "实际可执行性还受可达性、权限、日程、材料和工作类型个人优先级影响。" },
                    new[] { "Per-duplicant job priorities and current errand: oni://dupes/status-check." },
                    "缺氧机制速查"
                ),
                Entry(
                    "pipe_blockage_detector",
                    "automation",
                    StrategyDlc.Both,
                    "管道堵塞检测 / Pipe blockage detection",
                    new[] { "管道", "pipe", "堵塞", "blockage", "桥", "bridge", "白口", "过滤门", "filter gate", "自动化", "automation" },
                    new[]
                    {
                        "单桥检测可以用桥白口是否有元素判断流动/堵塞状态：流动时白口通常为空，堵塞时出现元素。",
                        "信号应加过滤门，避免短周期跳变导致执行器抖动。"
                    },
                    null,
                    new[] { "该判断依赖具体桥和管路布局，建好后用液体/气体包运行状态验证。" },
                    null,
                    "缺氧机制速查"
                ),
                Entry(
                    "power_wire_limits",
                    "power",
                    StrategyDlc.Both,
                    "导线与变压器上限 / Wire and transformer limits",
                    new[] { "电力", "power", "导线", "wire", "过载", "overload", "变压器", "transformer", "负载", "load" },
                    new[]
                    {
                        "小变压器常用额定 1kW，大变压器 4kW。",
                        "普通导线 1kW、导线束 2kW、高负荷导线和接头板 20kW 是常用规划边界。"
                    },
                    new[]
                    {
                        "small_transformer = 1kW",
                        "large_transformer = 4kW",
                        "wire = 1kW",
                        "conductive_wire = 2kW",
                        "heavy_watt_wire = 20kW",
                        "heavy_watt_joint_plate = 20kW"
                    },
                    new[] { "过载看同一电路的消费者总潜在负载，不是只看实时发电量。" },
                    new[] { "Live circuit load, battery charge and wire ratings in the current save: oni://power/summary and oni://power/ports." },
                    "缺氧机制速查"
                ),
                Entry(
                    "rocket_landing_beacon",
                    "space",
                    StrategyDlc.SpacedOut,
                    "定位信标落点范围 / Landing beacon touchdown window",
                    new[] { "火箭", "rocket", "定位信标", "landing beacon", "落点", "landing", "DLC", "太空", "space" },
                    new[]
                    {
                        "定位信标落点范围常按信标左 3 格、右 2 格估算。",
                        "规划火箭平台和障碍清理时要为该范围留空间。"
                    },
                    new[] { "landing_window = beacon_x - 3 .. beacon_x + 2" },
                    new[] { "不同舱块、地形和平台设计仍需用当前存档地图验证。" },
                    null,
                    "缺氧机制速查"
                ),
                Entry(
                    "atmo_suit_durability",
                    "dupes",
                    StrategyDlc.Both,
                    "气压服耐久估算 / Atmo suit durability estimate",
                    new[] { "气压服", "atmo suit", "耐久", "durability", "磨损", "wear", "太空服", "exosuit" },
                    new[]
                    {
                        "气压服默认磨损可按每周期 10% 粗估；扣除约 3 格睡眠后，实际常见磨损约 8.75%/周期。",
                        "完全磨损约 11.4 周期。"
                    },
                    new[] { "base_wear = 10% per cycle", "typical_wear ~= 8.75% per cycle", "empty_to_broken ~= 11.4 cycles" },
                    new[] { "实际耐久取决于穿脱时长、日程和复制人是否长时间在服内。" },
                    null,
                    "缺氧机制速查"
                )
            };
        }
    }
}
