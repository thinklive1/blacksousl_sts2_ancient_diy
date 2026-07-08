using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Girl In Maze event.</summary>
[RegisterActEvent(typeof(Glory))]
public sealed class GirlInMazeEvent : ModEventTemplate
{
    private const int MaxHpGain = 8;
    private const string PortraitPath = "res://bs_ancient/assets/images/events/GirlInMazeEvent.png";
    private const string DefaultPortraitPath = "res://images/events/bs_ancient_event_girl_in_maze_event.png";
    private static readonly System.Reflection.PropertyInfo? EventOptionTextKeyProperty =
        typeof(EventOption).GetProperty(
            nameof(EventOption.TextKey),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
    private static readonly System.Reflection.PropertyInfo? EventOptionTitleProperty =
        typeof(EventOption).GetProperty(
            nameof(EventOption.Title),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
    private static readonly System.Reflection.PropertyInfo? EventOptionDescriptionProperty =
        typeof(EventOption).GetProperty(
            nameof(EventOption.Description),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: PortraitPath
    );

    public override bool IsShared => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(MaxHpGain)];

    public override bool IsAllowed(IRunState runState)
    {
        return BsAncientConfig.EnableModEvents
            && !BsAncientConfig.DisableTestingEvents
            && runState.Players.Count == 1
            && runState.CurrentActIndex == 2
            && runState.Players.All(player =>
                MirrorSan.GetValue(player) < 0
                && player.GetRelic<GirlHandMirrorRelic>() == null
                && HasBasicHandMirror(player));
    }

    public override IEnumerable<string> GetAssetPaths(IRunState runState)
    {
        return base.GetAssetPaths(runState)
            .Select(path => path == DefaultPortraitPath ? PortraitPath : path)
            .Append(PortraitPath)
            .Distinct();
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        PickUpMirrorOption(),
        BreakMirrorOption(),
        PlainOption(DoNothing, InitialOptionKey("NOTHING"))
    ];

    private EventOption PickUpMirrorOption()
    {
        string textKey = InitialOptionKey("PICK_UP");
        EventOption option = RelicOption<GirlHandMirrorRelic>(PickUpMirror, textKey);
        RestoreOptionText(option, textKey);
        return option;
    }

    private EventOption PlainOption(Func<Task> onChosen, string textKey)
    {
        EventOption option = RelicOption<GirlHandMirrorRelic>(onChosen, textKey);
        RestoreOptionText(option, textKey);
        option.HoverTips = [];
        return option;
    }

    private EventOption BreakMirrorOption()
    {
        string textKey = InitialOptionKey("BREAK");
        EventOption option = RelicOption<GirlHandMirrorRelic>(BreakMirror, textKey);
        RestoreOptionText(option, textKey);
        option.HoverTips = HoverTipFactory.FromCardWithCardHoverTips<PervasiveMaliceCard>()
            .Append(HoverTipFactory.FromCard<Injury>());
        return option;
    }

    private void RestoreOptionText(EventOption option, string textKey)
    {
        string baseKey = textKey.EndsWith(".title", StringComparison.Ordinal)
            ? textKey[..^".title".Length]
            : textKey;
        string titleKey = $"{baseKey}.title";
        string descriptionKey = $"{baseKey}.description";
        EventOptionTextKeyProperty?.SetValue(option, titleKey);
        EventOptionTitleProperty?.SetValue(option, L10NLookup(titleKey));
        EventOptionDescriptionProperty?.SetValue(option, L10NLookup(descriptionKey));
    }

    private async Task PickUpMirror()
    {
        await RelicCmd.Obtain<GirlHandMirrorRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.PICK_UP.description"));
    }

    private async Task BreakMirror()
    {
        await AddCardToDeck<Injury>();
        await AddCardToDeck<PervasiveMaliceCard>();
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.BREAK.description"));
    }

    private async Task DoNothing()
    {
        await CreatureCmd.GainMaxHp(Owner!.Creature, DynamicVars.MaxHp.BaseValue);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.NOTHING.description"));
    }

    private async Task AddCardToDeck<T>() where T : CardModel
    {
        CardModel card = Owner!.RunState.CreateCard<T>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck, MegaCrit.Sts2.Core.Entities.Cards.CardPilePosition.Top, this, false), 2f);
    }

    private static bool HasBasicHandMirror(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        return player.GetRelic<PumpkinHandMirrorRelic>() != null
            || player.GetRelic<RabbitHandMirrorRelic>() != null
            || player.GetRelic<JackHandMirrorRelic>() != null;
    }
}
