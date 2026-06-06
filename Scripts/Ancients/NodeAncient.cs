using Godot;
using BlackSouls.Scripts.Cards;
using Blacksouls.Scripts;
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
    private IReadOnlyList<EventOption> Pool1Base => [
            CreateTimeQueenBlessingOption(),
            CreateWinterBellAllyOption(),
            CreateModRelicOption<NodeRibbonRelic>(),
            CreateStagnantGearOption(),
        ];

    private IReadOnlyList<EventOption> Pool1 => Pool1Base;

    private IReadOnlyList<EventOption> Pool2 => [
            CreateModRelicOption<DreamOfKadathRelic>(),
            CreateUnicornRoyalCrestOption(),
            CreateLionRoyalCrestOption(),
        ];

    private WeightedList<EventOption> CreatePool3()
    {
        WeightedList<EventOption> options = new()
        {
            { CreateCovenantOfNodeOption() ,1},
        };

        if (Owner != null && QuillPenRelic.CanBeOffered(Owner))
        {
            options.Add(CreateQuillPenOption(), 1);
        }

        if (Owner != null && CatCollarRelic.CanBeOffered(Owner))
        {
            options.Add(CreateCatCollarOption(), 1);
        }

        return options;
    }

    // 所有可能的选项
    public override IEnumerable<EventOption> AllPossibleOptions => [
        .. Pool1Base,
        CreateCatCollarOption(),
        .. Pool2,
        .. CreatePool3(),
        CreateModRelicOption<WhiteQueenSoldierRelic>()
    ];

    // 生成选项
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool isMultiplayer = Owner?.RunState.Players.Count > 1;
        List<EventOption> options =
        [
            Rng.NextItem(Pool1)!,
            Rng.NextItem(Pool2)!,
            CreatePool3().GetRandom(Rng),
        ];

        if (!isMultiplayer)
        {
            options.Add(CreateModRelicOption<WhiteQueenSoldierRelic>());
        }

        return options;
    }

    // 出现条件。这里是只能在第二幕出现（索引为1）
    public override bool IsAllowed(IRunState runState)
    {
        return !BsAncientConfig.DisableModAncients
            && runState.CurrentActIndex == 1;
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

    private EventOption CreateStagnantGearOption()
    {
        EventOption option = CreateModRelicOption<StagnantGearRelic>();
        option.HoverTips = option.HoverTips
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<StagnantGearCard>())
            .Append(HoverTipFactory.FromKeyword(MyKeywords.Encore));
        return option;
    }

    private EventOption CreateCatCollarOption()
    {
        EventOption option = CreateModRelicOption<CatCollarRelic>();
        option.HoverTips = option.HoverTips
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<CatSmileCard>())
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<CatBiteCard>());
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

    private EventOption CreateUnicornRoyalCrestOption()
    {
        EventOption option = CreateModRelicOption<UnicornRoyalCrestRelic>();
        option.HoverTips = option.HoverTips
            .Append(HoverTipFactory.FromPower<DexterityPower>());
        return option;
    }

    private EventOption CreateLionRoyalCrestOption()
    {
        EventOption option = CreateModRelicOption<LionRoyalCrestRelic>();
        option.HoverTips = option.HoverTips
            .Append(HoverTipFactory.FromPower<StrengthPower>())
            .Append(HoverTipFactory.FromPower<BufferPower>())
            .Append(HoverTipFactory.FromPower<PlatingPower>())
            .Append(HoverTipFactory.FromPower<IntangiblePower>());
        return option;
    }
}
