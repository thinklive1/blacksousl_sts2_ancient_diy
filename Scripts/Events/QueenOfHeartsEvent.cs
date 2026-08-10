using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Queen Of Hearts event.</summary>
[RegisterActEvent(typeof(Hive))]
public sealed class QueenOfHeartsEvent : ModEventTemplate
{
    private const int GoldGain = 200;
    private const int MaxCardsToEnchant = 3;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://bs_ancient/assets/images/events/QueenOfHeartsEvent.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new GoldVar(GoldGain),
        new CardsVar(MaxCardsToEnchant)
    ];

    public override bool IsAllowed(IRunState runState)
    {
        return BsAncientConfig.EnableModEvents
            && runState.CurrentActIndex == 1
            && (runState.Players.Any(player => player.GetRelic<QueenTartRelic>() != null)
                || QueenTartModifier.FindActive(runState) != null);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(
            this,
            RequestWeapon,
            InitialOptionKey("WEAPON"),
            HoverTipFactory.FromRelic<RedQueenGuillotineRelic>()),
        new EventOption(this, RequestGold, InitialOptionKey("GOLD")),
        new EventOption(
            this,
            GetSuitCandidates().Count > 0 ? RequestPlayingCards : null,
            InitialOptionKey(GetSuitCandidates().Count > 0 ? "PLAYING_CARDS" : "PLAYING_CARDS_LOCKED")),
    ];

    private async Task RequestWeapon()
    {
        await RelicCmd.Obtain<RedQueenGuillotineRelic>(Owner!);
        await RemoveTart();
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.WEAPON.description"));
    }

    private async Task RequestGold()
    {
        await PlayerCmd.GainGold(GoldGain, Owner!);
        await RemoveTart();
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.GOLD.description"));
    }

    private async Task RequestPlayingCards()
    {
        List<CardModel> candidates = GetSuitCandidates();
        if (candidates.Count > 0)
        {
            List<CardModel> selected = (await CardSelectCmd.FromSimpleGrid(
                    new BlockingPlayerChoiceContext(),
                    candidates,
                    Owner!,
                    new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 0, MaxCardsToEnchant)
                    {
                        RequireManualConfirmation = true
                    }))
                .ToList();

            foreach (CardModel card in selected)
            {
                ApplyRandomSuit(card);
                NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
                if (vfx != null)
                {
                    NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
                }
            }
        }

        await RemoveTart();
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.PLAYING_CARDS.description"));
    }

    private List<CardModel> GetSuitCandidates()
    {
        return PileType.Deck.GetPile(Owner!).Cards
            .Where(card => card.Enchantment == null && ModelDb.Enchantment<HeartSuitEnchantment>().CanEnchant(card))
            .ToList();
    }

    private void ApplyRandomSuit(CardModel card)
    {
        decimal amount = Owner!.RunState.Rng.Niche.NextInt(PlayingCardSuitEnchantment.MaxTriggersPerCombat)
            + PlayingCardSuitEnchantment.MinTriggersPerCombat;

        switch (Owner.RunState.Rng.Niche.NextInt(4))
        {
            case 0:
                CardCmd.Enchant<HeartSuitEnchantment>(card, amount);
                break;
            case 1:
                CardCmd.Enchant<DiamondSuitEnchantment>(card, amount);
                break;
            case 2:
                CardCmd.Enchant<ClubSuitEnchantment>(card, amount);
                break;
            default:
                CardCmd.Enchant<SpadeSuitEnchantment>(card, amount);
                break;
        }
    }

    private async Task RemoveTart()
    {
        if (Owner!.GetRelic<QueenTartRelic>() is { } tart)
        {
            await RelicCmd.Remove(tart);
        }

        if (QueenTartModifier.FindActive(Owner!.RunState) is { } modifier)
        {
            modifier.MarkClaimed();
        }
    }
}
