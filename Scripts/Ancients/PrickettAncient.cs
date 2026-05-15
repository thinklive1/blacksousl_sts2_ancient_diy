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
        ];

    private IReadOnlyList<EventOption> Pool2 => [
        CreateAliceRibbonOption(),
        CreateQuillPenOption(),
        ];

    private WeightedList<EventOption> Pool3 => new()
    {
        { CreateModRelicOption<CovenantOfPrickettRelic>(), 1 },
    };

    public override IEnumerable<EventOption> AllPossibleOptions => [.. Pool1, .. Pool2, .. Pool3];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Rng.NextItem(Pool1)!,
            Rng.NextItem(Pool2)!,
            Pool3.GetRandom(Rng),
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
            .Append(CreateRedQueenDiceDetailsHoverTip())
            .Append(HoverTipFactory.FromPower<StrengthPower>())
            .Append(HoverTipFactory.FromPower<DexterityPower>());
        return option;
    }

    private static HoverTip CreateRedQueenDiceDetailsHoverTip()
    {
        LocString description = new("relics", "BS_ANCIENT_RELIC_RED_QUEEN_DICE_RELIC.diceDetails.description");
        description.Add("CardDrawPerTurn", 1);
        description.Add("DexterityPower", 3);
        description.Add("CardsPerDraw", 4);
        description.Add("RewardCards", 5);

        return new HoverTip(
            new LocString("relics", "BS_ANCIENT_RELIC_RED_QUEEN_DICE_RELIC.diceDetails.title"),
            description);
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
}
