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

/// <summary>Implements the Node ancient encounter.</summary>
[RegisterActAncient(typeof(Hive))]
public class NodeAncient : ModAncientEventTemplate
{
    public override Color ButtonColor => new(0.12f, 0.2f, 0.8f, 0.5f);
    public override Color DialogueColor => Colors.White;

    public override string? CustomBackgroundScenePath => "res://bs_ancient/assets/scenes/node_ancient.tscn";

    public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
        MapIconPath: "res://bs_ancient/assets/images/map/node.png",
        MapIconOutlinePath: "res://bs_ancient/assets/images/map/node_outline.png",
        RunHistoryIconPath: "res://bs_ancient/assets/images/map/node.png",
        RunHistoryIconOutlinePath: "res://bs_ancient/assets/images/map/node_outline.png"
    );

    // The first pool is reused by the run history option list.
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

    // Exposes all generated options so the event can build complete hover data.
    public override IEnumerable<EventOption> AllPossibleOptions => [
        .. Pool1Base,
        CreateCatCollarOption(),
        .. Pool2,
        .. CreatePool3(),
        CreateModRelicOption<WhiteQueenSoldierRelic>()
    ];

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

    // Node is only allowed in act 2, which uses zero-based index 1.
    public override bool IsAllowed(IRunState runState)
    {
        return !BsAncientConfig.DisableModAncients
            && BsAncientConfig.EnableNodeAncient
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
