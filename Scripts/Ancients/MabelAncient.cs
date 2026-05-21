using Godot;
using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace BlackSouls.Scripts;

[RegisterActAncient(typeof(Hive))]
[RegisterActAncient(typeof(Glory))]
public class MabelAncient : ModAncientEventTemplate
{
    public override Color ButtonColor => new(0f, 0f, 0f, 0.5f);

    public override Color DialogueColor => Colors.Black;

    public override string? CustomBackgroundScenePath => "res://bs_ancient/assets/scenes/mabel_ancient.tscn";

    public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
        MapIconPath: "res://bs_ancient/assets/images/map/mabel.png",
        MapIconOutlinePath: "res://bs_ancient/assets/images/map/mabel_outline.png",
        RunHistoryIconPath: "res://bs_ancient/assets/images/map/mabel.png",
        RunHistoryIconOutlinePath: "res://bs_ancient/assets/images/map/mabel_outline.png"
    );

    private IReadOnlyList<EventOption> CreatePool1(bool isMultiplayer)
    {
        List<EventOption> options =
        [
            CreateModRelicOption<LittleMermaidFavorRelic>(),
            CreatePrincessFrogFavorOption(),
            CreateSnowWhiteFavorOption(),
            CreateCinderellaFavorOption(),
        ];

        if (!isMultiplayer)
        {
            options.Insert(0, CreateModRelicOption<RapunzelFavorRelic>());
        }

        return options;
    }

    private IReadOnlyList<EventOption> CreatePool2(bool isMultiplayer)
    {
        List<EventOption> options =
        [
            CreateStageEndOption(),
        ];

        if (!isMultiplayer)
        {
            options.Insert(0, CreateHlanithWineOption());
        }

        return options;
    }

    private IReadOnlyList<EventOption> FullPool1 => [
        CreateModRelicOption<RapunzelFavorRelic>(),
        CreateModRelicOption<LittleMermaidFavorRelic>(),
        CreatePrincessFrogFavorOption(),
        CreateSnowWhiteFavorOption(),
        CreateCinderellaFavorOption(),
    ];

    private IReadOnlyList<EventOption> FullPool2 => [
        CreateHlanithWineOption(),
        CreateStageEndOption(),
    ];

    private WeightedList<EventOption> FullPool3 => new()
    {
        { CreateEternalVanityOption(), 1 },
        { CreateModRelicOption<MysteryOfNightSkyRelic>(), 1 },
        { CreateGiftOfChaosOption(), 1 },
    };

    public override IEnumerable<EventOption> AllPossibleOptions => [CreateFavorChoiceOption(false), .. FullPool1, .. FullPool2, .. FullPool3];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool isMultiplayer = Owner?.RunState.Players.Count > 1;
        IReadOnlyList<EventOption> pool1 = CreatePool1(isMultiplayer);
        IReadOnlyList<EventOption> pool2 = CreatePool2(isMultiplayer);
        WeightedList<EventOption> pool3 = CreatePool3(isMultiplayer);
        List<EventOption> options =
        [
            CreateFavorChoiceOption(isMultiplayer),
            pool3.GetRandom(Rng),
        ];

        if (pool2.Count > 0)
        {
            options.Insert(1, Rng.NextItem(pool2)!);
        }

        return options;
    }

    private WeightedList<EventOption> CreatePool3(bool isMultiplayer)
    {
        WeightedList<EventOption> options = new()
        {
            { CreateEternalVanityOption(), 1 },
            { CreateModRelicOption<MysteryOfNightSkyRelic>(), 1 },
            { CreateGiftOfChaosOption(), 1 },
        };

        return options;
    }

    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex is 1 or 2;
    }

    private EventOption CreateFavorChoiceOption(bool isMultiplayer)
    {
        return new EventOption(this, () => ChooseFavor(isMultiplayer), InitialOptionKey("CHOOSE_FAVOR"));
    }

    private Task ChooseFavor(bool isMultiplayer)
    {
        List<EventOption> options = CreatePool1(isMultiplayer)
            .ToList()
            .UnstableShuffle(Rng)
            .Take(3)
            .ToList();

        SetEventState(L10NLookup($"{Id.Entry}.pages.CHOOSE_FAVOR.description"), options);
        return Task.CompletedTask;
    }

    private EventOption CreatePrincessFrogFavorOption()
    {
        EventOption option = CreateModRelicOption<PrincessFrogFavorRelic>();
        option.HoverTips = option.HoverTips
            .Append(HoverTipFactory.FromPower<WeakPower>())
            .Append(HoverTipFactory.FromPower<VulnerablePower>())
            .Append(HoverTipFactory.FromPower<FrailPower>());
        return option;
    }

    private EventOption CreateSnowWhiteFavorOption()
    {
        EventOption option = CreateModRelicOption<SnowWhiteFavorRelic>();
        option.HoverTips = option.HoverTips
            .Append(HoverTipFactory.FromPower<DexterityPower>());
        return option;
    }

    private EventOption CreateCinderellaFavorOption()
    {
        EventOption option = CreateModRelicOption<CinderellaFavorRelic>();
        option.HoverTips = option.HoverTips
            .Append(HoverTipFactory.FromPower<StrengthPower>());
        return option;
    }

    private EventOption CreateHlanithWineOption()
    {
        EventOption option = CreateModRelicOption<HlanithWineRelic>();
        option.HoverTips = option.HoverTips
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<HlanithWineCard>());
        return option;
    }

    private EventOption CreateStageEndOption()
    {
        EventOption option = CreateModRelicOption<StageEndRelic>();
        option.HoverTips = option.HoverTips
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<StageEndCard>())
            .Append(HoverTipFactory.FromPower<MadnessPower>());
        return option;
    }

    private EventOption CreateEternalVanityOption()
    {
        EventOption option = CreateModRelicOption<EternalVanityRelic>();
        option.HoverTips = option.HoverTips
            .Append(HoverTipFactory.FromKeyword(CardKeyword.Ethereal));
        return option;
    }

    private EventOption CreateGiftOfChaosOption()
    {
        EventOption option = CreateModRelicOption<GiftOfChaosRelic>();
        option.HoverTips = option.HoverTips
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<ChaosFusionCard>());
        return option;
    }
}
