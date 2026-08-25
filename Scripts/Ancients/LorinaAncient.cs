using Godot;
using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements Lorina's second-act ancient encounter.</summary>
[RegisterActAncient(typeof(Hive))]
public class LorinaAncient : ModAncientEventTemplate
{
    public override Color ButtonColor => new(0.55f, 0.03f, 0.08f, 0.5f);

    public override Color DialogueColor => new(0.85f, 0.08f, 0.12f);

    public override string? CustomBackgroundScenePath => "res://bs_ancient/assets/scenes/lorina_ancient.tscn";

    public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
        MapIconPath: "res://bs_ancient/assets/images/map/lorina.png",
        MapIconOutlinePath: "res://bs_ancient/assets/images/map/lorina_outline.png",
        RunHistoryIconPath: "res://bs_ancient/assets/images/map/lorina.png",
        RunHistoryIconOutlinePath: "res://bs_ancient/assets/images/map/lorina_outline.png"
    );

    public override IEnumerable<EventOption> AllPossibleOptions => [
        CreateModRelicOption<QueenOfHeartsRedDeckRelic>(),
        CreateModRelicOption<QueenOfHeartsYellowDeckRelic>(),
        CreateModRelicOption<QueenOfHeartsNebulaDeckRelic>(),
        CreateLastAceOption(),
        CreateFlippedAceOption(),
        CreateReversedAceOption(),
        CreateModRelicOption<QueenOfHeartsExecutionOrderRelic>(),
        CreateCroquetMalletOption(),
        CreateModRelicOption<QueenOfHeartsJudgmentOrderRelic>(),
        CreateModRelicOption<RoyalChipRelic>(),
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        IReadOnlyList<EventOption> deckPool = CreateDeckPool();
        IReadOnlyList<EventOption> acePool = CreateAcePool();
        IReadOnlyList<EventOption> executionPool = CreateExecutionPool();
        IReadOnlyList<EventOption> gamblePool = [CreateModRelicOption<RoyalChipRelic>()];

        return [
            Rng.NextItem(deckPool)!,
            Rng.NextItem(acePool)!,
            Rng.NextItem(executionPool)!,
            Rng.NextItem(gamblePool)!,
        ];
    }

    private IReadOnlyList<EventOption> CreateDeckPool()
    {
        return [
            CreateModRelicOption<QueenOfHeartsRedDeckRelic>(),
            CreateModRelicOption<QueenOfHeartsYellowDeckRelic>(),
            CreateModRelicOption<QueenOfHeartsNebulaDeckRelic>(),
        ];
    }

    private IReadOnlyList<EventOption> CreateAcePool()
    {
        return [
            CreateLastAceOption(),
            CreateFlippedAceOption(),
            CreateReversedAceOption(),
        ];
    }

    private IReadOnlyList<EventOption> CreateExecutionPool()
    {
        return [
            CreateModRelicOption<QueenOfHeartsExecutionOrderRelic>(),
            CreateCroquetMalletOption(),
            CreateModRelicOption<QueenOfHeartsJudgmentOrderRelic>()
        ];
    }

    private EventOption CreateCroquetMalletOption()
    {
        EventOption option = CreateModRelicOption<QueenOfHeartsCroquetMalletRelic>();
        option.HoverTips = option.HoverTips.Concat(
            HoverTipFactory.FromCardWithCardHoverTips<QueenOfHeartsCroquetMalletCard>());
        return option;
    }

    private EventOption CreateLastAceOption()
    {
        EventOption option = CreateModRelicOption<LastAceRelic>();
        option.HoverTips = option.HoverTips.Concat(
            HoverTipFactory.FromCardWithCardHoverTips<LastAceCard>());
        return option;
    }

    private EventOption CreateFlippedAceOption()
    {
        EventOption option = CreateModRelicOption<FlippedAceRelic>();
        return option;
    }

    private EventOption CreateReversedAceOption()
    {
        EventOption option = CreateModRelicOption<ReversedAceRelic>();
        option.HoverTips = option.HoverTips.Concat(
            HoverTipFactory.FromCardWithCardHoverTips<ReversedAceCard>());
        return option;
    }

    public override bool IsAllowed(IRunState runState)
    {
        return !BsAncientConfig.DisableModAncients
            && BsAncientConfig.EnableLorinaAncient
            && runState.CurrentActIndex == 1;
    }

}
