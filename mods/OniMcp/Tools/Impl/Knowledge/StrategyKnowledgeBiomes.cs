using System.Collections.Generic;

namespace OniMcp.Tools
{
    /// <summary>
    /// Biome knowledge: what each region holds, what it threatens, and when it is worth entering.
    ///
    /// Entries describe judgement rather than tables of numbers. Element temperatures, plant
    /// bands and ore yields vary by asteroid and by patch, so those belong in derive: read the
    /// actual cells before committing a dig.
    /// </summary>
    public static partial class StrategyKnowledgeTools
    {
        private static List<StrategyEntry> BiomeEntries()
        {
            return new List<StrategyEntry>
            {
                Entry(
                    "biome_temperate_start",
                    "biome",
                    StrategyDlc.Both,
                    "温带起始生态区 / Temperate starting biome",
                    new[] { "温带", "temperate", "起始", "starting", "沙岩", "sandstone", "藻类", "algae", "生态区", "biome" },
                    new[]
                    {
                        "起始区温度温和、无致命气体，是唯一可以不做任何防护就扩张的区域。前 30 周期几乎所有基建都应该留在这里。",
                        "The starting biome is the only region you can expand into with no protection at all. Everything through roughly cycle 30 belongs here.",
                        "主要资源：沙岩、藻类、氧气、泥土和可直接种植的作物。这些足够支撑早期制氧、农业和研究。",
                        "扩张方向优先左右水平推进，而不是急着上下钻。上方常接表层真空或高温，下方常接沼泽或高温区。"
                    },
                    null,
                    new[]
                    {
                        "起始区边界之外的温度和气体可能立刻致命；挖穿边界前先看目标格的元素和温度。",
                        "藻类是有限资源。把它当作过渡而不是长期制氧方案。"
                    },
                    new[]
                    {
                        "Per-cell element, temperature and gas before digging: world_editor command=read on the target cell.",
                        "Whole-map overview: oni://world/text-map.",
                        "What is actually in reach right now: colony_control domain=survival action=plan."
                    },
                    "OniMcp strategy corpus, wave 2"
                ),
                Entry(
                    "biome_swamp",
                    "biome",
                    StrategyDlc.Both,
                    "沼泽生态区 / Swamp biome",
                    new[] { "沼泽", "swamp", "marsh", "淤泥", "slime", "污染氧", "polluted oxygen", "肺孢子菌", "slimelung" },
                    new[]
                    {
                        "沼泽提供淤泥、污染氧和污染水，是藻类耗尽后最自然的下一个氧气来源，代价是肺孢子菌。",
                        "Slime is the reason to come here: it distills into algae. The cost is slimelung, which spreads through polluted oxygen and infects duplicants who breathe it.",
                        "进入前的准备顺序：先有隔离门，再有处理污染氧的手段，最后才大规模挖淤泥。",
                        "把淤泥搬回基地比把基地扩进沼泽更安全，因为病菌留在原地更容易控制。"
                    },
                    null,
                    new[]
                    {
                        "肺孢子菌在污染氧中存活，在氯气中死亡。挖开沼泽前想好气体怎么隔离。",
                        "沼泽区的污染水看起来像免费水源，但直接抽进基地会把病菌一起带回来。"
                    },
                    new[]
                    {
                        "Current germ counts on cells and items: world_editor command=read on the target cell.",
                        "Whether anyone is already infected: oni://dupes/status-check.",
                        "See entry germ_slimelung for handling."
                    },
                    "OniMcp strategy corpus, wave 2"
                ),
                Entry(
                    "biome_caustic",
                    "biome",
                    StrategyDlc.Both,
                    "腐蚀性生态区 / Caustic biome",
                    new[] { "腐蚀", "caustic", "氯气", "chlorine", "漂白石", "bleach stone", "消毒", "disinfect", "生态区", "biome" },
                    new[]
                    {
                        "腐蚀区的氯气会杀死病菌，这使它从威胁变成工具：氯气房是处理受污染物资最省事的消毒手段。",
                        "Chlorine kills germs, so the caustic biome is less a hazard than a resource. A chlorine room is the cheapest way to sterilise contaminated supplies.",
                        "漂白石升华产生氯气，可以用来给消毒房补气，不需要专门的气体生产。",
                        "复制人在氯气中无法呼吸，所以氯气区要么穿服进入，要么只做无人化处理。"
                    },
                    null,
                    new[]
                    {
                        "氯气比氧气重，会往下沉。消毒房漏气时先检查下方是否有通路。",
                        "不要把氯气引进有作物或牲畜的区域。"
                    },
                    new[]
                    {
                        "Gas layering order: see entry gas_density_order.",
                        "Per-cell gas identity: world_editor command=zoom views=oxygen."
                    },
                    "OniMcp strategy corpus, wave 2"
                ),
                Entry(
                    "biome_frozen",
                    "biome",
                    StrategyDlc.Both,
                    "寒冷生态区 / Frozen biome",
                    new[] { "寒冷", "frozen", "冰", "ice", "苔藓", "wheezewort", "低温", "cold", "保鲜", "生态区", "biome" },
                    new[]
                    {
                        "寒冷区最大的价值不是冰，而是低温本身：它是早期唯一免费的散热与保鲜环境。",
                        "The real asset here is the cold, not the ice. It is the only free cooling and food-preservation environment available early.",
                        "常见做法是把冰箱或食物储藏放在寒冷区边缘，用环境温度代替电力制冷。",
                        "野生的降温植物可以直接利用，不需要驯化，前提是它们所在的气体环境合适。"
                    },
                    null,
                    new[]
                    {
                        "复制人在低温中会受冻伤影响，长时间作业需要保暖或轮换。",
                        "把热源接进寒冷区会不可逆地消耗这份免费低温，规划前想清楚。"
                    },
                    new[]
                    {
                        "Per-cell temperature and comfort band: world_editor command=read on the target cell.",
                        "Buildings currently at overheat risk: oni://thermal/overheat-risk."
                    },
                    "OniMcp strategy corpus, wave 2"
                ),
                Entry(
                    "biome_oil_and_magma",
                    "biome",
                    StrategyDlc.Both,
                    "石油与岩浆区 / Oil and magma biomes",
                    new[] { "石油", "oil", "原油", "crude oil", "岩浆", "magma", "高温", "heat", "塑料", "plastic", "生态区", "biome" },
                    new[]
                    {
                        "石油区是塑料和石油燃料的来源，岩浆区是无限热源。两者都在基地下方，且都会把热带上来。",
                        "Both are heat problems before they are resources. Opening either without a thermal plan is the most common way a mid-game colony cooks itself.",
                        "进入顺序上，先有隔热建材和降温手段，再打通；不要因为看到原油就急着挖。",
                        "岩浆的价值在于配合蒸汽涡轮发电和材料熔炼，而不是直接接触。"
                    },
                    null,
                    new[]
                    {
                        "热量不会自己消失。挖通高温区之前先确认你有能力把热搬走或删除。",
                        "原油在高温下会变成石油再变成酸气，温度失控时链条会自己往前走。"
                    },
                    new[]
                    {
                        "Insulated tile effective conductivity: see entry insulated_tile_conductivity.",
                        "Heat removal options: see entry heat_deletion_methods.",
                        "Per-cell temperature before breaching: world_editor command=read on the target cell."
                    },
                    "OniMcp strategy corpus, wave 4"
                ),
                Entry(
                    "biome_space_surface",
                    "biome",
                    StrategyDlc.Both,
                    "表层与太空 / Surface and space",
                    new[] { "表层", "surface", "太空", "space", "真空", "vacuum", "陨石", "meteor", "太阳能", "solar", "月尘", "regolith" },
                    new[]
                    {
                        "表层同时是威胁和资源：陨石会砸穿建筑，但真空是最好的隔热层，阳光是免费电力。",
                        "Vacuum is the best insulator in the game, so the surface is where thermal designs want to be, provided you can survive the meteors.",
                        "标准做法是先建抗压门或牺牲层挡陨石，再在其下布置太阳能和火箭平台。",
                        "月尘会持续堆积，需要清扫或自动化处理，否则会压垮产能。"
                    },
                    null,
                    new[]
                    {
                        "真空隔热的前提是真的抽成真空；残留少量气体就会显著导热。",
                        "复制人在表层需要氧气面罩或服装，否则会窒息。"
                    },
                    new[]
                    {
                        "Current surface exposure and cell contents: oni://world/text-map.",
                        "Meteor and surface events in this asteroid: read the live world rather than assuming a schedule."
                    },
                    "OniMcp strategy corpus, wave 5"
                ),
                Entry(
                    "biome_radioactive",
                    "biome",
                    StrategyDlc.SpacedOut,
                    "辐射生态区 / Radioactive biome",
                    new[] { "辐射", "radiation", "铀", "uranium", "辐射病", "rad sickness", "研究反应堆", "reactor", "生态区", "biome" },
                    new[]
                    {
                        "辐射区提供铀矿和辐射源，是 DLC 中高级研究和辐射螺栓的基础。",
                        "Radiation is cumulative and invisible in the way heat is not: duplicants accumulate rads long before they show symptoms.",
                        "进入前先有辐射防护服或铅屏蔽，并把长期作业改成短轮换。",
                        "辐射也可以被利用：辐射螺栓驱动高级研究，野生植物在辐射下有不同表现。"
                    },
                    null,
                    new[]
                    {
                        "辐射穿透普通建材；只有特定材料能有效屏蔽。",
                        "把辐射源放在基地附近之前，先确认复制人的日常路径不会穿过辐射场。"
                    },
                    new[]
                    {
                        "Per-duplicant radiation exposure and sickness state: oni://dupes/status-check.",
                        "Whether the running game is Spaced Out at all: colony_control domain=snapshot action=get."
                    },
                    "OniMcp strategy corpus, wave 6"
                )
            };
        }
    }
}
