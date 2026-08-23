using System.Collections.Generic;

namespace OniMcp.Tools
{
    /// <summary>
    /// Research and duplicant management.
    ///
    /// Deliberately no hardcoded tech ids. Tech identifiers and tree shape change between
    /// updates and between base game and Spaced Out, so entries describe what to unlock and
    /// why in that order, and derive points at the live research tree for the actual ids.
    /// A corpus that names a tech the running game no longer has is worse than one that
    /// tells you how to look it up.
    /// </summary>
    public static partial class StrategyKnowledgeTools
    {
        private static List<StrategyEntry> ResearchEntries()
        {
            return new List<StrategyEntry>
            {
                Entry(
                    "research_ordering_principle",
                    "research",
                    StrategyDlc.Both,
                    "研究顺序原则 / How to order research",
                    new[] { "研究", "research", "科技", "tech", "顺序", "order", "优先级", "priority", "科技树", "tech tree" },
                    new[]
                    {
                        "研究顺序的判断标准不是科技本身有多强，而是它解锁的东西需要多久才能见效。",
                        "Order research by lead time, not by power. Anything with a long ramp — crops that take cycles to mature, buildings that need a room built first — must be unlocked before you need it, not when you need it.",
                        "早期优先级大致是：解决当前会杀死殖民地的问题 > 缩短未来瓶颈的前置 > 提升效率的锦上添花。",
                        "常见错误是按科技树从左到右推进。应该按当前殖民地的实际短板选下一个。"
                    },
                    null,
                    new[]
                    {
                        "研究本身消耗资源和复制人时间；在断粮或缺氧时研究不是优先事项。",
                        "科技 id 在不同版本和 DLC 间会变化，不要照抄任何攻略里的 id。"
                    },
                    new[]
                    {
                        "The actual tech ids, tiers, prerequisites and costs in this game: colony_control domain=management kind=research action=list.",
                        "What is currently being researched: oni://research/status.",
                        "Which colony problem should drive the next pick: oni://colony/advice."
                    },
                    "OniMcp strategy corpus, wave 2"
                ),
                Entry(
                    "research_early_targets",
                    "research",
                    StrategyDlc.Both,
                    "早期研究目标 / Early research targets",
                    new[] { "早期研究", "early research", "开局", "opening", "科技", "tech", "农业", "farming", "制氧", "oxygen" },
                    new[]
                    {
                        "早期研究的实际目标只有四类：稳定氧气、稳定食物、处理排泄与卫生、以及解锁管道和电力基建。",
                        "The first four capabilities worth unlocking are: a renewable oxygen source, a farmable food source, sanitation, and the plumbing plus power tech that everything later depends on.",
                        "农业类科技要早于你真正缺食物的时候解锁，因为作物成熟需要若干周期，等到断粮再种就来不及了。",
                        "管道和电力是很多后续模块的隐性前置，包括 SPOM 和净水循环；把它们当作基建而不是可选项。"
                    },
                    null,
                    new[]
                    {
                        "不要为了解锁高级建筑跳过卫生科技；厕所和洗手设施对早期士气和疾病影响很大。",
                        "本条目只描述能力类别，不给具体科技名；请从当前存档的研究列表解析实际 id。"
                    },
                    new[]
                    {
                        "Resolve capability to a real tech id: colony_control domain=management kind=research action=list with a keyword.",
                        "Whether a building you want is already unlocked: oni://buildings/defs (the `unlocked` field).",
                        "Current food and oxygen headroom: colony_control domain=snapshot action=get."
                    },
                    "OniMcp strategy corpus, wave 2"
                ),
                Entry(
                    "printing_pod_selection",
                    "dupes",
                    StrategyDlc.Both,
                    "打印舱选择策略 / Printing pod selection",
                    new[] { "打印舱", "printing pod", "复制人", "duplicant", "选人", "selection", "补给包", "care package", "特质", "traits" },
                    new[]
                    {
                        "每多一个复制人就是每周期多约 1000 千卡的食物和持续的氧气消耗。人口增长应该跟在产能后面，而不是前面。",
                        "A duplicant is a permanent recurring cost, not a one-off gain. Take one only when food and oxygen already have headroom for it.",
                        "选人时看兴趣和负面特质，而不是当前技能等级：技能可以练，负面特质跟一辈子。",
                        "当产能吃紧时，补给包（资源、种子、动物）通常比新复制人更有价值。"
                    },
                    new[] { "added_food_need_per_cycle ~= 1000 kcal per duplicant" },
                    new[]
                    {
                        "严重负面特质（如需要极高装饰、无法执行关键工种）在小殖民地里代价被放大。",
                        "拒绝打印是合法选择；打印舱会在下一次周期继续提供机会。"
                    },
                    new[]
                    {
                        "Current food headroom and per-duplicant burn: oni://resources/food and colony_control domain=snapshot action=get.",
                        "Existing duplicant traits and aptitudes: oni://dupes.",
                        "Exact calorie burn in this game version: read the duplicant definition rather than trusting 1000."
                    },
                    "OniMcp strategy corpus, wave 2"
                ),
                Entry(
                    "dupe_traits_and_aptitudes",
                    "dupes",
                    StrategyDlc.Both,
                    "复制人特质与天赋 / Duplicant traits and aptitudes",
                    new[] { "特质", "traits", "天赋", "aptitude", "技能", "skills", "分工", "roles", "复制人", "duplicant" },
                    new[]
                    {
                        "天赋决定升级某个工种的性价比，特质决定这个复制人长期是资产还是负担。",
                        "Aptitude tells you who to invest skill points in. Traits tell you who will quietly cost you for the rest of the run.",
                        "早期分工大致按：挖掘与建造、研究与操作、农业与养殖、后勤运输。四个人就能覆盖这四类。",
                        "技能点会提高士气需求。给一个复制人加技能不是免费的，士气跟不上会引发压力。"
                    },
                    null,
                    new[]
                    {
                        "别把所有技能点堆在一个人身上：他一旦被困或生病，那条产线就停了。",
                        "工种优先级设置比技能等级更常是复制人不干活的原因。"
                    },
                    new[]
                    {
                        "Per-duplicant traits, aptitudes, skills and current errand: oni://dupes and oni://dupes/status-check.",
                        "Errand priority mechanics: see entry automation_priority_stack."
                    },
                    "OniMcp strategy corpus, wave 2"
                ),
                Entry(
                    "morale_and_decor",
                    "dupes",
                    StrategyDlc.Both,
                    "士气与装饰 / Morale and decor",
                    new[] { "士气", "morale", "装饰", "decor", "压力", "stress", "房间", "room", "娱乐", "recreation" },
                    new[]
                    {
                        "士气是技能需求与生活质量之间的差额。加技能会抬高需求，改善住宿、食物和装饰会抬高供给。",
                        "Morale is a balance, not a resource: skill points raise the requirement, better food, beds, rooms and decor raise the supply.",
                        "早期最划算的士气来源是：私人卧室房间加成、达标的餐厅、以及基本装饰。这些都不需要高级科技。",
                        "压力上升通常是士气长期为负的结果，而不是单一事件。看趋势而不是看某一次波动。"
                    },
                    null,
                    new[]
                    {
                        "房间加成需要满足具体的尺寸和家具条件，差一格就不生效。",
                        "压力反应（呕吐、破坏）会造成次生问题，处理压力要早于它触发反应。"
                    },
                    new[]
                    {
                        "Which rooms currently qualify and for what bonus: oni://rooms/list.",
                        "Current stress levels per duplicant: oni://dupes/status-check.",
                        "Exact room size and furniture requirements in this version: read the room definitions rather than assuming."
                    },
                    "OniMcp strategy corpus, wave 2"
                ),
                Entry(
                    "schedules_and_priorities",
                    "dupes",
                    StrategyDlc.Both,
                    "日程与优先级 / Schedules and priorities",
                    new[] { "日程", "schedule", "优先级", "priority", "轮班", "shift", "差事", "errand", "分工" },
                    new[]
                    {
                        "日程决定复制人什么时候能干活，优先级决定他们干什么。两者中日程更常被忽略。",
                        "Splitting duplicants across staggered shifts keeps toilets, beds and the printing pod from bottlenecking, and keeps some coverage awake at all times.",
                        "工种优先级（每人对每类工作的偏好）比建筑上的数字优先级更常决定实际行为。",
                        "如果某件事一直没人做，先查可达性，再查工种优先级，最后才调数字优先级。"
                    },
                    null,
                    new[]
                    {
                        "把所有人设成同一班会在同一时刻抢厕所和床。",
                        "数字优先级不是绝对的；火箭、个人需求和急迫差事会压过它。"
                    },
                    new[]
                    {
                        "Per-duplicant schedule and job priorities: oni://dupes/status-check.",
                        "Full errand priority ordering: see entry automation_priority_stack.",
                        "Why a specific errand is not being done: see entry reachability_failure."
                    },
                    "OniMcp strategy corpus, wave 2"
                )
            };
        }
    }
}
