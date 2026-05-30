using Godot;
using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;
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

    private IReadOnlyList<EventOption> CreatePool2(bool isMultiplayer)
    {
        List<EventOption> options =
        [
            CreateAliceRibbonOption(),
            CreateOldFilmBOption(),
        ];

        if (!isMultiplayer)
        {
            options.Insert(1, CreateRedQueenMirrorOption());
        }

        return options;
    }

    public override IEnumerable<EventOption> AllPossibleOptions => [
        .. Pool1,
        .. CreatePool2(isMultiplayer: false),
        CreateModRelicOption<CovenantOfPrickettRelic>(),
        CreateAliceCurseOption(),
        CreateQuillPenOption(),
        CreateModRelicOption<RedQueenSoldierRelic>()
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool isMultiplayer = Owner?.RunState.Players.Count > 1;
        IReadOnlyList<EventOption> pool2 = CreatePool2(isMultiplayer);

        return
        [
            Rng.NextItem(Pool1)!,
            Rng.NextItem(pool2)!,
            CreatePool3().GetRandom(Rng),
            CreateModRelicOption<RedQueenSoldierRelic>(),
        ];
    }

    public override bool IsAllowed(IRunState runState)
    {
        return !BsAncientConfig.DisableModAncients
            && runState.CurrentActIndex == 2;
    }

    private EventOption CreateRedQueenDiceOption()
    {
        return CreateModRelicOption<RedQueenDiceRelic>();
    }

    private EventOption CreateRedQueenMirrorOption()
    {
        EventOption option = CreateModRelicOption<RedQueenMirrorRelic>();
        option.HoverTips = option.HoverTips
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<RedQueenMirrorCard>());
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

    private EventOption CreateAliceCurseOption()
    {
        EventOption option = CreateModRelicOption<AliceCurseRelic>();
        option.HoverTips = option.HoverTips
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<AliceCurseCard>());
        return option;
    }

    private WeightedList<EventOption> CreatePool3()
    {
        bool hasEnoughCurses = Owner != null && PileType.Deck.GetPile(Owner).Cards.Count(IsCurseCard) >= 3;
        int aliceCurseWeight = hasEnoughCurses ? 8 : 10;
        int covenantWeight = hasEnoughCurses ? 1 : 45;
        int quillPenWeight = hasEnoughCurses ? 1 : 45;

        return new WeightedList<EventOption>
        {
            { CreateModRelicOption<CovenantOfPrickettRelic>(), covenantWeight },
            { CreateAliceCurseOption(), aliceCurseWeight },
            { CreateQuillPenOption(), quillPenWeight },
        };
    }

    private static bool IsCurseCard(CardModel card)
    {
        return card.Type == CardType.Curse;
    }
}
