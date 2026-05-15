using Godot;
using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace BlackSouls.Scripts;

[RegisterActAncient(typeof(Hive))] // 指定只有荣耀这章生成
// [RegisterSharedAncient] // 如果需要自定义生成条件，可以注册成通用再重载isAllowed
public class NodeAncient : ModAncientEventTemplate
{
    // 选项按钮颜色
    public override Color ButtonColor => new(0.12f, 0.2f, 0.8f, 0.5f);
    // 对话框颜色
    public override Color DialogueColor => Colors.White;

    // 自定义场景的路径
    public override string? CustomBackgroundScenePath => "res://bs_ancient/assets/scenes/node_ancient.tscn";

    // 自定义地图图标和轮廓的路径
    public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
        MapIconPath: "res://bs_ancient/assets/images/map/node.png",
        MapIconOutlinePath: "res://bs_ancient/assets/images/map/node_outline.png",
        RunHistoryIconPath: "res://bs_ancient/assets/images/map/node.png",
        RunHistoryIconOutlinePath: "res://bs_ancient/assets/images/map/node_outline.png"
    );

    // 固定池一和二
    private IReadOnlyList<EventOption> Pool1 => [
            CreateTimeQueenBlessingOption(),
            CreateWinterBellAllyOption(),
        ];
    private IReadOnlyList<EventOption> Pool2 => [
            CreateModRelicOption<DreamOfKadathRelic>(),
            CreateQuillPenOption(),
        ];

    // 带权重池三。权重越大越有机会生成。当然你也可以写自定义的列表生成函数
    private WeightedList<EventOption> Pool3 => new()
    {
        { CreateCovenantOfNodeOption() ,1},
    };

    // 所有可能的选项
    public override IEnumerable<EventOption> AllPossibleOptions => [.. Pool1, .. Pool2, .. Pool3];

    // 生成选项
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Rng.NextItem(Pool1)!,
            Rng.NextItem(Pool2)!,
            Pool3.GetRandom(Rng),
        ];
    }

    // 出现条件。这里是只能在第二幕出现（索引为1）
    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex == 1;
    }

    private EventOption CreateTimeQueenBlessingOption()
    {
        EventOption option = CreateModRelicOption<TimeQueenBlessingRelic>();
        option.HoverTips = option.HoverTips
            .Concat(HoverTipFactory.FromEnchantment<ReplayEnchantment>())
            .Append(HoverTipFactory.FromKeyword(CardKeyword.Retain));
        return option;
    }

    private EventOption CreateWinterBellAllyOption()
    {
        EventOption option = CreateModRelicOption<WinterBellAllyRelic>();
        option.HoverTips = option.HoverTips
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<GerdaCard>())
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<FlorenceCard>())
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<GhostHunterCard>());
        return option;
    }

    private EventOption CreateQuillPenOption()
    {
        EventOption option = CreateModRelicOption<QuillPenRelic>();
        option.HoverTips = option.HoverTips
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<PowerOfRewrite>());
        return option;
    }

    private EventOption CreateCovenantOfNodeOption()
    {
        EventOption option = CreateModRelicOption<CovenantOfNodeRelic>();
        option.HoverTips = option.HoverTips
            .Append(HoverTipFactory.FromPower<RegenPower>());
        return option;
    }
}
