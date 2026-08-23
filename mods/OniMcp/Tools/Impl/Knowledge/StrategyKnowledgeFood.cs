using System.Collections.Generic;

namespace OniMcp.Tools
{
    /// <summary>
    /// Food chain progression and cooking. Waves 2, 3 and 5.
    ///
    /// Specific crop yields, growth times and recipe outputs are read from the game rather
    /// than quoted: they differ between base game and Spaced Out and shift between patches.
    /// What is stable is the shape of the progression and why each step exists.
    /// </summary>
    public static partial class StrategyKnowledgeTools
    {
        private static List<StrategyEntry> FoodEntries()
        {
            return new List<StrategyEntry>
            {
                Entry(
                    "food_chain_progression",
                    "food",
                    StrategyDlc.Both,
                    "食物链推进顺序 / Food chain progression",
                    new[] { "食物", "food", "农业", "farming", "作物", "crop", "卡路里", "calories", "推进", "progression" },
                    new[]
                    {
                        "食物progression 的每一步解决的是不同问题：先解决够不够吃，再解决稳不稳定，最后才解决好不好吃。",
                        "The three stages are quantity, then reliability, then quality. Jumping to quality food before supply is reliable wastes the effort, because a morale bonus does not help a colony that runs out.",
                        "第一阶段靠采集和最容易种的作物撑住卡路里。第二阶段把种植环境固定下来，让产出可预测。第三阶段引入烹饪提升士气。",
                        "任何一步的前提都是种植环境（温度、气体、灌溉）已经稳定；环境不稳时增加田地数量只是放大波动。"
                    },
                    new[] { "kcal_needed_per_cycle = dupes * 1000" },
                    new[]
                    {
                        "作物成熟需要若干周期，扩产不是即时见效的；断粮时先找现成卡路里。",
                        "产量数字随版本变化，规划时按当前存档实际收成校准。"
                    },
                    new[]
                    {
                        "Current stock, types and spoilage: oni://resources/food.",
                        "Per-crop growth requirements in this version: read the plant definitions rather than assuming.",
                        "Emergency handling: see entry food_shortage_response."
                    },
                    "OniMcp strategy corpus, wave 2"
                ),
                Entry(
                    "food_preservation_strategy",
                    "food",
                    StrategyDlc.Both,
                    "食物保存策略 / Food preservation strategy",
                    new[] { "保鲜", "preservation", "腐败", "spoilage", "冰箱", "refrigerator", "无菌", "sterile", "二氧化碳", "carbon dioxide" },
                    new[]
                    {
                        "食物腐败取决于温度和所处气体环境。冷藏只是其中一种手段，把食物放进合适的气体里同样有效且不耗电。",
                        "Refrigeration is not the only answer. Food stored in the right atmosphere keeps without power, which matters early when the grid is tight.",
                        "二氧化碳环境常被用作免费保鲜区，因为它比空气重，会自然沉在基地底部形成稳定气层。",
                        "彻底停止腐败需要把食物温度压到冰点以下，这通常是中期以后才值得的投入。"
                    },
                    null,
                    new[]
                    {
                        "腐败会让账面上的卡路里总量与实际可食用量脱节；看保质状态而不是只看总量。",
                        "把食物放在通路上会被复制人不断搬动，破坏你设计的保存环境。"
                    },
                    new[]
                    {
                        "Deep-freeze technique with trace gas: see entry trace_gas_food_preservation.",
                        "Current spoilage state: oni://resources/food.",
                        "Gas layering so a CO2 pocket stays put: see entry gas_density_order."
                    },
                    "OniMcp strategy corpus, wave 3"
                ),
                Entry(
                    "cooking_and_morale",
                    "food",
                    StrategyDlc.Both,
                    "烹饪与士气 / Cooking and morale",
                    new[] { "烹饪", "cooking", "料理", "recipe", "士气", "morale", "食物品质", "food quality", "厨房", "kitchen" },
                    new[]
                    {
                        "烹饪的主要回报是士气而不是卡路里。有些配方甚至会损失总卡路里，但换来的士气让技能投资成为可能。",
                        "Cooking is a morale system wearing a food system's clothes. Some recipes lose calories on conversion; you accept that to raise the morale ceiling.",
                        "引入烹饪的正确时机是：供给已稳定，并且你开始给复制人加技能点。在此之前它是纯粹的额外工时。",
                        "评估配方时看每单位输入换来的士气，以及输入本身是否可持续，而不是只看成品品质等级。"
                    },
                    null,
                    new[]
                    {
                        "高品质配方常依赖需要专门种植链的原料；先确认原料可持续再改造厨房。",
                        "烹饪占用复制人工时，产线要靠近食物储存和餐厅以减少搬运。"
                    },
                    new[]
                    {
                        "Recipe inputs, outputs and quality in this version: oni://production/recipes.",
                        "Which cooking buildings are unlocked: oni://buildings/defs.",
                        "Morale mechanics: see entry morale_and_decor."
                    },
                    "OniMcp strategy corpus, wave 5"
                ),
                Entry(
                    "colony_scaling_milestones",
                    "colony",
                    StrategyDlc.Both,
                    "殖民地规模里程碑 / Colony scaling milestones",
                    new[] { "规模", "scaling", "里程碑", "milestone", "长期", "long run", "扩张", "expansion", "周期", "cycle" },
                    new[]
                    {
                        "殖民地扩张的限制因素会随规模变化：早期是氧气和食物，中期是热量和水，后期是复制人工时和物流。",
                        "The binding constraint changes as you grow. Early it is oxygen and food, mid it is heat and water, late it is duplicant labour and logistics. Solving last era's problem harder does not help.",
                        "每次准备扩张前先问：当前真正的瓶颈是什么？增加复制人只在瓶颈是工时的时候有帮助。",
                        "自动化和物流（清扫机、传送带）的价值随规模上升，因为它们替代的是最稀缺的资源，也就是复制人时间。"
                    },
                    null,
                    new[]
                    {
                        "在瓶颈是热量时增加人口会同时加剧热量和食物压力。",
                        "规模越大，单点故障影响越广；扩张前先确认关键系统有余量。"
                    },
                    new[]
                    {
                        "What is currently binding: oni://colony/advice.",
                        "Long-run survivability triage: colony_control domain=survival action=plan.",
                        "Live metrics trend: colony_control domain=snapshot action=get with watch."
                    },
                    "OniMcp strategy corpus, wave 5"
                ),
                Entry(
                    "rockets_and_logistics",
                    "space",
                    StrategyDlc.Both,
                    "火箭与星际物流 / Rockets and interplanetary logistics",
                    new[] { "火箭", "rocket", "发射", "launch", "引擎", "engine", "燃料", "fuel", "货舱", "cargo", "物流", "logistics" },
                    new[]
                    {
                        "火箭的设计约束是一个三角：引擎推力决定能带多少质量，质量决定能装多少货和燃料，燃料决定能飞多远。改一个必然影响另外两个。",
                        "Range, payload and engine are a single trade: adding cargo costs range, adding fuel costs cargo. Design backwards from the destination you actually need.",
                        "第一枚火箭的目标应该是拿到你本地缺少的资源，而不是飞得最远。",
                        "地面配套（发射井、燃料补给、货物装卸）的建设量常被低估，往往比火箭本身更耗时。"
                    },
                    null,
                    new[]
                    {
                        "发射会摧毁发射路径上的建筑，规划时留出净空。",
                        "基础版与 Spaced Out 的火箭系统差异很大，不要混用两套经验。"
                    },
                    new[]
                    {
                        "Available rocket modules, engines and their stats: oni://rockets/module-defs.",
                        "Landing beacon touchdown window: see entry rocket_landing_beacon.",
                        "Which game version is running: colony_control domain=snapshot action=get."
                    },
                    "OniMcp strategy corpus, wave 6"
                ),
                Entry(
                    "radiation_and_reactors",
                    "radiation",
                    StrategyDlc.SpacedOut,
                    "辐射与研究反应堆 / Radiation and research reactors",
                    new[] { "辐射", "radiation", "辐射螺栓", "radbolt", "反应堆", "reactor", "铀", "uranium", "辐射病", "rad sickness", "屏蔽", "shielding" },
                    new[]
                    {
                        "辐射既是危害也是资源：它会让复制人患辐射病，也驱动高级研究和辐射螺栓系统。",
                        "Radiation accumulates silently. Unlike heat, a duplicant walking through a radioactive area shows no immediate effect, so exposure is usually discovered after the damage.",
                        "处理原则是把辐射源和复制人日常路径彻底分开，而不是靠防护缩短暴露时间。",
                        "研究反应堆需要冷却和废料处理，把它当作一个完整的工业系统而不是单个建筑。"
                    },
                    null,
                    new[]
                    {
                        "普通建材对辐射屏蔽效果有限；依赖错误的材料等于没有屏蔽。",
                        "反应堆失控的后果是不可逆的，实装前先确认冷却链已完成。"
                    },
                    new[]
                    {
                        "Per-duplicant radiation exposure and sickness: oni://dupes/status-check.",
                        "Radioactive biome entry planning: see entry biome_radioactive.",
                        "Reactor building requirements in this version: oni://buildings/defs."
                    },
                    "OniMcp strategy corpus, wave 6"
                )
            };
        }
    }
}
