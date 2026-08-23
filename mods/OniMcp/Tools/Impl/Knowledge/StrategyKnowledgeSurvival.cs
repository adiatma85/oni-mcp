using System.Collections.Generic;

namespace OniMcp.Tools
{
    /// <summary>
    /// Immediate-response knowledge: what to do when a colony health rule fires.
    ///
    /// These exist because the advisor exposed a gap. Asking the corpus about "food" while
    /// duplicants were starving returned trace-gas preservation, a high-scoring match to the
    /// wrong question. Rules need entries about the failure they describe, not the topic they
    /// belong to.
    /// </summary>
    public static partial class StrategyKnowledgeTools
    {
        private static List<StrategyEntry> SurvivalEntries()
        {
            return new List<StrategyEntry>
            {
                Entry(
                    "reachability_failure",
                    "reachability",
                    StrategyDlc.Both,
                    "复制人无法到达目标 / Duplicants cannot reach a target",
                    new[] { "可达性", "reachability", "寻路", "pathing", "够不到", "unreachable", "被困", "trapped", "梯子", "ladder", "收割", "harvest" },
                    new[]
                    {
                        "游戏会把无法到达的差事静默跳过，不会报错。表现是资源就在那里，复制人却一直不去做。",
                        "The game silently skips errands it cannot path to, so \"food exists but nobody harvests it\" is almost always a navigation problem, not a farming problem.",
                        "常见原因：缺梯子或杆子造成的垂直断层、被液体封死的通道、门权限设置、区域限制，或差事在复制人当前日程外。",
                        "排查顺序：先确认目标格本身可达，再看有没有连续地板/梯子路径，最后才怀疑优先级和日程。"
                    },
                    null,
                    new[]
                    {
                        "复制人站在目标附近不代表可达；对角线和单格缺口都会阻断路径。",
                        "别急着重建农场。先修路，通常一段梯子就能恢复十几个待收割差事。"
                    },
                    new[]
                    {
                        "Per-duplicant position, current errand and suspected trapped state: oni://dupes/status-check.",
                        "Whether specific harvestables are reachable right now: colony_control domain=bio kind=farming action=list_harvestables readyOnly=true.",
                        "Frontier dig candidates that would open a path: colony_control domain=survival action=plan."
                    },
                    "OniMcp strategy corpus, wave 2"
                ),
                Entry(
                    "food_shortage_response",
                    "food",
                    StrategyDlc.Both,
                    "断粮应急处理 / Responding to a food shortage",
                    new[] { "断粮", "food shortage", "饥饿", "starving", "卡路里", "calories", "应急", "emergency", "口粮", "ration" },
                    new[]
                    {
                        "断粮时先找已有卡路里，再考虑增产。地上的可收割作物、野生植物和散落食物通常比新建农场快得多。",
                        "When food runs short, harvest and gather before you build. A new farm takes cycles to mature; ready harvestables and loose food are available this cycle.",
                        "确认食物是否真的吃不到：可收割但无人收割往往是可达性问题，不是产量问题。",
                        "短期手段：收割待收作物、挖野生食物、把散落食物扫进冰箱或储物柜让复制人能取用。"
                    },
                    new[] { "kcal_needed_per_cycle = dupes * 1000 (a duplicant burns about 1000 kcal per cycle)" },
                    new[]
                    {
                        "别在断粮时增加复制人，打印舱新人会让缺口立刻扩大。",
                        "食物腐败也会造成账面充足但实际不可食用；检查保质状态而不是只看总量。"
                    },
                    new[]
                    {
                        "Current stock, food types and spoilage: oni://resources/food.",
                        "Ready and reachable harvestables: colony_control domain=bio kind=farming action=list_harvestables readyOnly=true.",
                        "Per-duplicant calorie burn in this game version: read the duplicant definition rather than assuming 1000."
                    },
                    "OniMcp strategy corpus, wave 2"
                ),
                Entry(
                    "idle_generator_response",
                    "power",
                    StrategyDlc.Both,
                    "发电机空转与电池耗尽 / Idle generators and draining batteries",
                    new[] { "发电机", "generator", "空转", "idle", "电池", "battery", "耗尽", "draining", "手摇", "manual generator", "断电", "unpowered" },
                    new[]
                    {
                        "手摇发电机默认就是空闲的：复制人只在需要时才去摇。电池有电时发电机不转属于正常现象，不是故障。",
                        "An idle manual generator with charged batteries is normal play. It only matters once the batteries run down, so judge the grid by stored charge, not by generator activity.",
                        "煤发电机空转通常是缺煤或没有复制人补料；检查储量和供应差事，而不是重建电网。",
                        "消费端全部断电但电池还有电，往往是线路没接上或电路被分成了两个互不相连的网。"
                    },
                    new[] { "battery_runway_cycles ~= stored_joules / (consumer_watts * 600)" },
                    new[]
                    {
                        "别按发电机是否运转判断电力健康，按电池储量趋势判断。",
                        "接入新负载前先看导线额定值，避免过载烧线。"
                    },
                    new[]
                    {
                        "Battery charge, circuit load and generator state: oni://power/summary.",
                        "Which ports actually have wire attached: oni://power/ports.",
                        "Wire and transformer ratings: see entry power_wire_limits."
                    },
                    "OniMcp strategy corpus, wave 2"
                )
            };
        }
    }
}
