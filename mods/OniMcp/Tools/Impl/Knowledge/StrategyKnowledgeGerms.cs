using System.Collections.Generic;

namespace OniMcp.Tools
{
    /// <summary>
    /// Disease, water and sanitation. Wave 3: the systems that decide whether a colony
    /// that survived its first fifty cycles keeps working.
    /// </summary>
    public static partial class StrategyKnowledgeTools
    {
        private static List<StrategyEntry> GermEntries()
        {
            return new List<StrategyEntry>
            {
                Entry(
                    "germ_transmission_basics",
                    "germs",
                    StrategyDlc.Both,
                    "病菌传播基础 / How germs actually spread",
                    new[] { "病菌", "germs", "疾病", "disease", "传播", "transmission", "感染", "infection", "消毒", "disinfect" },
                    new[]
                    {
                        "病菌不会自己攻击复制人。它需要一条传播路径：呼吸受污染气体、食用带菌食物、或接触带菌液体和物体。切断路径比消灭病菌更有效。",
                        "Germs need a route: breathed, eaten, or touched. Cutting the route is nearly always cheaper than sterilising the source.",
                        "洗手池和消毒台放在正确的位置比放很多个更重要：它们必须在污染区与清洁区之间的唯一通路上。",
                        "病菌在不同介质中的存活速度差别很大，某些环境会让它自然衰减，某些会让它繁殖。"
                    },
                    null,
                    new[]
                    {
                        "把带菌物资搬进基地再处理，等于把传播路径修到自己家门口。",
                        "复制人洗手后仍会携带手上以外的污染；单靠洗手池不能解决气体传播。"
                    },
                    new[]
                    {
                        "Germ counts on a specific cell or item: world_editor command=read on that cell.",
                        "Who is currently sick or incubating: oni://dupes/status-check.",
                        "Colony-level disease diagnostics: oni://colony/diagnostics."
                    },
                    "OniMcp strategy corpus, wave 3"
                ),
                Entry(
                    "germ_food_poisoning",
                    "germs",
                    StrategyDlc.Both,
                    "食物中毒 / Food poisoning",
                    new[] { "食物中毒", "food poisoning", "病菌", "germs", "厕所", "toilet", "洗手", "wash basin", "污染水" },
                    new[]
                    {
                        "食物中毒几乎总是从厕所出来的复制人把病菌带到食物或水上开始的。源头是流程，不是食物本身。",
                        "Food poisoning is a workflow problem: it starts with a duplicant leaving a toilet and touching food or water. Fix the path, not the pantry.",
                        "标准处理是把洗手设施放在厕所出口的必经之路上，让复制人无法绕过。",
                        "已经带菌的食物可以隔离等待病菌自然衰减，通常比销毁更划算。"
                    },
                    null,
                    new[]
                    {
                        "洗手池只在复制人经过时生效；放在死角等于没放。",
                        "污染水直接用于农业或烹饪会把病菌带进食物链。"
                    },
                    new[]
                    {
                        "Current food germ state: oni://resources/food.",
                        "Whether the sanitation path is actually on the route: oni://rooms/list plus the map view."
                    },
                    "OniMcp strategy corpus, wave 3"
                ),
                Entry(
                    "germ_slimelung",
                    "germs",
                    StrategyDlc.Both,
                    "肺孢子菌处理 / Handling slimelung",
                    new[] { "肺孢子菌", "slimelung", "淤泥", "slime", "污染氧", "polluted oxygen", "沼泽", "swamp", "氯气", "chlorine" },
                    new[]
                    {
                        "肺孢子菌通过污染氧传播，所以它本质上是气体管理问题，不是医疗问题。",
                        "Slimelung travels in polluted oxygen, which makes it a gas containment problem. Treating sick duplicants without containing the gas just repeats the cycle.",
                        "两种可靠处理方式：把带菌淤泥放进氯气房消毒，或把污染氧隔离并让病菌在其中自然衰减。",
                        "挖沼泽时先做气闸，再挖；否则污染氧会顺着通道进入基地。"
                    },
                    null,
                    new[]
                    {
                        "氧气面罩能防吸入，但不能阻止病菌随物资进入基地。",
                        "病菌数量下降需要时间；刚进氯气房的淤泥不等于已消毒。"
                    },
                    new[]
                    {
                        "Germ counts on the slime and on the air: world_editor command=read on those cells.",
                        "Chlorine as a disinfectant: see entry biome_caustic.",
                        "Swamp entry planning: see entry biome_swamp."
                    },
                    "OniMcp strategy corpus, wave 3"
                ),
                Entry(
                    "water_budget_and_recycling",
                    "water",
                    StrategyDlc.Both,
                    "水循环与预算 / Water budget and recycling",
                    new[] { "水", "water", "净水器", "water sieve", "污染水", "polluted water", "循环", "recycling", "预算", "budget" },
                    new[]
                    {
                        "水在缺氧里几乎不会真正消失，只会在干净水和污染水之间来回。所以水危机通常是回收链断了，而不是水用光了。",
                        "Water rarely leaves the system; it moves between clean and polluted. A water shortage is usually a broken recycling loop rather than genuine consumption.",
                        "净水器把污染水变回干净水但不去除病菌，这一点常被误解：它解决的是元素问题，不是卫生问题。",
                        "厕所、洗手池和农业是主要的污染水来源，也是最容易接回循环的三处。"
                    },
                    null,
                    new[]
                    {
                        "电解制氧是真正会消耗水的用途之一；接入前先确认水源可持续。",
                        "净水器需要过滤介质，介质耗尽会让整条循环停摆。"
                    },
                    new[]
                    {
                        "Current clean and polluted water stock: oni://resources/inventory.",
                        "SPOM water demand specifically: see entry spom_water_budget.",
                        "Whether germs survive sieving: check the germ count on the output, do not assume."
                    },
                    "OniMcp strategy corpus, wave 3"
                ),
                Entry(
                    "ranching_wild_vs_domestic",
                    "ranching",
                    StrategyDlc.Both,
                    "野生与驯化养殖 / Wild versus domestic ranching",
                    new[] { "养殖", "ranching", "野生", "wild", "驯化", "domestic", "梳理", "grooming", "牧场", "stable", "小动物", "critter" },
                    new[]
                    {
                        "野生动物不需要照料但产出低；驯化动物产出高但需要持续的梳理和喂食工时。选择取决于你缺的是资源还是人力。",
                        "Wild critters cost no labour and produce little. Domesticated ones produce far more but consume grooming time every cycle. Choose based on which you are short of.",
                        "早期通常野生更划算：复制人少，工时比资源更稀缺。",
                        "驯化的价值在于可预测的产出，这对依赖稳定输入的生产链很重要。"
                    },
                    null,
                    new[]
                    {
                        "过度拥挤会同时压低野生和驯化动物的表现；先看空间再看数量。",
                        "牧场需要复制人可达；把牧场放在偏远角落会让梳理差事排不上。"
                    },
                    new[]
                    {
                        "Overcrowding maths: see entry critter_overcrowding_happiness.",
                        "Per-species space and diet requirements: read the critter definitions rather than assuming.",
                        "Current rooms and their sizes: oni://rooms/list."
                    },
                    "OniMcp strategy corpus, wave 3"
                ),
                Entry(
                    "atmo_suits_and_checkpoints",
                    "dupes",
                    StrategyDlc.Both,
                    "气压服与检查站 / Atmo suits and checkpoints",
                    new[] { "气压服", "atmo suit", "检查站", "checkpoint", "船坞", "dock", "外勤", "expedition", "隔离", "airlock" },
                    new[]
                    {
                        "气压服把危险区域从不可进入变成可作业，代价是穿脱时间、耐久和额外的基建。",
                        "Suits convert a hazardous region from off-limits to workable. The cost is donning time, durability and the dock infrastructure feeding them.",
                        "检查站的作用是强制复制人在通过前穿服；没有检查站，他们会直接走进危险区。",
                        "船坞需要氧气和电力供应；把它们建在危险区外侧而不是里面。"
                    },
                    null,
                    new[]
                    {
                        "服装耐久耗尽后复制人会继续工作但失去防护，这个转变不明显。",
                        "穿脱时间会显著降低短途作业效率；不是所有任务都值得穿服。"
                    },
                    new[]
                    {
                        "Suit durability estimates: see entry atmo_suit_durability.",
                        "Who is currently suited and where: oni://dupes/status-check."
                    },
                    "OniMcp strategy corpus, wave 3"
                )
            };
        }
    }
}
