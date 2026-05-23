using Godot;
using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace BlackSouls.Scripts;

[RegisterActAncient(typeof(Glory))]
public class PrickettAncient : ModAncientEventTemplate
{
    public override Color ButtonColor => new(0.8f, 0.08f, 0.08f, 0.5f);

    public override Color DialogueColor => new(0.8f, 0.08f, 0.08f);

    public override string? CustomBackgroundScenePath => "res://bs_ancient/assets/scenes/prickett_ancient.tscn";

    public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
        MapIconPath: "res://bs_ancient/assets/images/map/prickett.png",
        MapIconOutlinePath: "res://bs_ancient/assets/images/map/prickett_outline.png",
        RunHistoryIconPath: "res://bs_ancient/assets/images/map/prickett.png",
        RunHistoryIconOutlinePath: "res://bs_ancient/assets/images/map/prickett_outline.png"
    );

    private IReadOnlyList<EventOption> Pool1 => [
        CreateModRelicOption<RedQueenAlbumRelic>(),
        CreateRedQueenDiceOption(),
        CreateOldFilmAOption(),
        ];

    private IReadOnlyList<EventOption> Pool2 => [
        CreateAliceRibbonOption(),
        CreateQuillPenOption(),
        CreateOldFilmBOption(),
        ];

    private WeightedList<EventOption> Pool3 => new()
    {
        { CreateModRelicOption<CovenantOfPrickettRelic>(), 1 },
    };

    public override IEnumerable<EventOption> AllPossibleOptions => [.. Pool1, .. Pool2, .. Pool3, CreateModRelicOption<RedQueenSoldierRelic>()];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Rng.NextItem(Pool1)!,
            Rng.NextItem(Pool2)!,
            Pool3.GetRandom(Rng),
            CreateModRelicOption<RedQueenSoldierRelic>(),
        ];
    }

    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex == 2;
    }

    private EventOption CreateRedQueenDiceOption()
    {
        EventOption option = CreateModRelicOption<RedQueenDiceRelic>();
        option.HoverTips = option.HoverTips
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<RedQueenRerollCard>());
        return option;
    }

    private EventOption CreateAliceRibbonOption()
    {
        EventOption option = CreateModRelicOption<AliceRibbonRelic>();
        option.HoverTips = option.HoverTips
            .Append(HoverTipFactory.FromPower<StrengthPower>());
        return option;
    }

    private EventOption CreateQuillPenOption()
    {
        EventOption option = CreateModRelicOption<QuillPenRelic>();
        option.HoverTips = option.HoverTips
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<PowerOfRewrite>());
        return option;
    }

    private EventOption CreateOldFilmAOption()
    {
        EventOption option = CreateModRelicOption<OldFilmA>();
        option.HoverTips = option.HoverTips
            .Append(HoverTipFactory.FromPower<ViolenceDemonPower>());
        return option;
    }

    private EventOption CreateOldFilmBOption()
    {
        EventOption option = CreateModRelicOption<OldFilmB>();
        option.HoverTips = option.HoverTips
            .Append(HoverTipFactory.FromPower<VulnerablePower>());
        return option;
    }
}
