using System.Collections.Generic;

namespace OniMcp.Tools
{
    /// <summary>
    /// Wave 4: heat management, geysers and industry.
    ///
    /// This is the highest factual-risk area of the corpus. Geyser output rates, dormancy
    /// periods and eruption temperatures are randomised per asteroid and per geyser instance,
    /// so any specific number quoted from a guide is wrong for your save. Entries here carry
    /// taming method and judgement; every rate goes through derive.
    /// </summary>
    public static partial class StrategyKnowledgeTools
    {
        private static List<StrategyEntry> ThermalEntries()
        {
            return new List<StrategyEntry>
            {
                Entry(
                    "heat_is_conserved",
                    "thermal",
                    StrategyDlc.Both,
                    "热量守恒与热管理思路 / Heat is conserved",
                    new[] { "热量", "heat", "散热", "cooling", "温度", "temperature", "过热", "overheat", "工业", "industry" },
                    new[]
                    {
                        "热量不会因为你把它推走而消失。降温建筑只是把热搬到别处，通常还额外产热。真正的解决方案只有三类：搬走、储存、或删除。",
                        "Cooling buildings move heat, they do not remove it, and they add their own waste heat on top. Only three things actually work: move it somewhere that does not matter, store it in a large mass, or delete it through a phase change or a turbine.",
                        "早期用免费手段：寒冷生态区、真空隔离、以及把产热设备放在远离生活区的地方。",
                        "中期开始才值得建主动降温，因为它需要电力、材料和一个能承接废热的出口。"
                    },
                    null,
                    new[]
                    {
                        "把热塞进一个封闭房间只是推迟问题；房间温度会一直升到设备损坏。",
                        "隔热砖不是绝对绝热，极端温差下仍会缓慢传导。"
                    },
                    new[]
                    {
                        "Which buildings are currently at overheat risk: oni://thermal/overheat-risk.",
                        "Per-cell temperature: world_editor command=zoom views=temperature.",
                        "Insulated tile conductivity: see entry insulated_tile_conductivity.",
                        "Cell heat exchange formula: see entry cell_heat_exchange."
                    },
                    "OniMcp strategy corpus, wave 4"
                ),
                Entry(
                    "heat_deletion_methods",
                    "thermal",
                    StrategyDlc.Both,
                    "热量删除手段 / Heat deletion methods",
                    new[] { "删除热量", "heat deletion", "蒸汽涡轮", "steam turbine", "相变", "phase change", "冷却", "cooling", "水冷" },
                    new[]
                    {
                        "游戏里真正能删除热量的手段很少：蒸汽涡轮把热转成电，相变吸收潜热，以及部分转换类建筑在过程中吃掉热量。",
                        "A steam turbine is the workhorse: it converts heat into power and returns cooler water, which is as close to deleting heat as the game offers.",
                        "涡轮加热调节器（aquatuner）的组合之所以标准，是因为调节器制造的废热正好可以被涡轮回收利用。",
                        "选择冷却剂时看比热和相变点是否匹配你的温度区间，而不是看它听起来多高级。"
                    },
                    null,
                    new[]
                    {
                        "调节器自身产热很高；没有涡轮承接时它会把房间烧穿。",
                        "相变类技巧对温度余量非常敏感，实装前留足缓冲。"
                    },
                    new[]
                    {
                        "Forced phase change thresholds: see entry forced_phase_change.",
                        "Building power draw and heat output in this version: oni://buildings/defs.",
                        "Live temperatures around the machine room: world_editor command=zoom views=temperature."
                    },
                    "OniMcp strategy corpus, wave 4"
                ),
                Entry(
                    "geyser_taming_principle",
                    "geyser",
                    StrategyDlc.Both,
                    "间歇泉驯化原则 / Geyser taming principle",
                    new[] { "间歇泉", "geyser", "喷泉", "vent", "驯化", "taming", "休眠", "dormancy", "产出", "output", "缓冲" },
                    new[]
                    {
                        "驯化间歇泉的核心不是接住喷发，而是抹平休眠。间歇泉会喷发一段时间再休眠一段时间，下游必须按平均产量设计，不是按喷发瞬时量。",
                        "Taming a geyser means smoothing dormancy, not catching the eruption. Size the storage buffer so downstream consumers never notice the geyser is asleep.",
                        "标准结构是：一个能容纳整个休眠期用量的缓冲池，加上一个防止喷口被自身产物压住的排放设计。",
                        "喷口被高压堵住会停止产出，所以抽走产物和储存产物同样重要。"
                    },
                    new[] { "buffer_needed ~= average_consumption_rate * longest_dormant_period" },
                    new[]
                    {
                        "每个间歇泉的产量、周期和休眠时长都是随机的，攻略上的具体数字对你的存档不成立。",
                        "喷发温度往往远高于基地可接受范围；接入前先想好降温。"
                    },
                    new[]
                    {
                        "This geyser's actual rate, period and dormancy: study it in game and read oni://geotuners/geysers.",
                        "Its current output temperature: world_editor command=read on the vent cell.",
                        "Cooling the output before use: see entry heat_deletion_methods."
                    },
                    "OniMcp strategy corpus, wave 4"
                ),
                Entry(
                    "geyser_types_and_priority",
                    "geyser",
                    StrategyDlc.Both,
                    "间歇泉类型与驯化顺序 / Geyser types and taming order",
                    new[] { "间歇泉", "geyser", "蒸汽", "steam", "水", "water", "天然气", "natural gas", "氯气", "chlorine", "氢气", "hydrogen", "火山", "volcano", "油井", "oil reservoir" },
                    new[]
                    {
                        "驯化顺序应该按需求排，不按难度排。缺水先驯水源，缺电先驯天然气，缺冷却能力再考虑高温类。",
                        "Water sources are usually first: they feed oxygen, farming and cooling all at once. Natural gas is the common second because it converts directly into power.",
                        "冷蒸汽喷泉通常比蒸汽喷泉更容易利用，因为它的输出温度更接近可直接使用的范围。",
                        "高温类（蒸汽喷泉、火山、金属火山）价值在于配合涡轮发电，而不是直接取材，处理它们需要先具备耐高温建材和降温能力。",
                        "污染水喷泉和污染氧喷口的产物都需要先处理再使用，把它们当作半成品资源。"
                    },
                    null,
                    new[]
                    {
                        "本条目只给类别层面的判断。每口泉的具体参数必须在你自己的存档里研究后读取。",
                        "火山类喷发会瞬间释放大量热，未做隔离就打开会波及整个基地。"
                    },
                    new[]
                    {
                        "Which geysers exist on this asteroid and their studied data: oni://geotuners/geysers.",
                        "Buffer sizing method: see entry geyser_taming_principle.",
                        "Whether you can cool the output yet: see entry heat_deletion_methods."
                    },
                    "OniMcp strategy corpus, wave 4"
                ),
                Entry(
                    "power_scaling",
                    "power",
                    StrategyDlc.Both,
                    "电力规模化 / Scaling power",
                    new[] { "电力", "power", "发电", "generator", "电网", "grid", "变压器", "transformer", "电池", "battery", "自动化", "automation" },
                    new[]
                    {
                        "电网扩张的瓶颈通常不是发电量，而是线路容量和电路划分。一条电路上的潜在总负载超过导线额定值就会烧线，哪怕实际发电量很小。",
                        "Split the grid before you scale generation. A transformer-isolated branch keeps a single heavy consumer from overloading the whole colony's wiring.",
                        "智能电池加自动化开关可以让发电机按需运行，减少燃料浪费和废热。",
                        "煤和氢是早中期主力；真正的长期方案是把工业废热接进涡轮回收。"
                    },
                    null,
                    new[]
                    {
                        "过载判定看电路上消费者的潜在总功率，不是当前实际用电。",
                        "电池有自放电，过量储能反而浪费。"
                    },
                    new[]
                    {
                        "Wire and transformer ratings: see entry power_wire_limits.",
                        "Live circuit load and battery charge: oni://power/summary.",
                        "Which ports already have wire: oni://power/ports.",
                        "Idle generators and battery drain: see entry idle_generator_response."
                    },
                    "OniMcp strategy corpus, wave 4"
                )
            };
        }
    }
}
