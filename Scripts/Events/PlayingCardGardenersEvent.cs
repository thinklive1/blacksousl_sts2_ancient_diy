using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Lets owners of the Queen's deck repaint their non-Heart Suit cards.</summary>
[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Underdocks))]
[RegisterActEvent(typeof(Hive))]
[RegisterActEvent(typeof(Glory))]
public sealed class PlayingCardGardenersEvent : ModEventTemplate
{
    private const int PaintGoldCost = 107;
    private const int RequiredNonHeartCards = 3;
    private const string PortraitPath = "res://bs_ancient/assets/images/events/PlayingCardGardenersEvent.jpg";

    public override EventAssetProfile AssetProfile => new(InitialPortraitPath: PortraitPath);

    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(PaintGoldCost)];

    public override bool IsAllowed(IRunState runState)
    {
        return BsAncientConfig.EnableModEvents
            && runState.CurrentActIndex is >= 0 and <= 2
            && runState.Players.All(HasGardenerCondition);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool canBuyPaint = Owner!.Gold >= PaintGoldCost;
        bool canHelp = GetHeartCandidates(Owner).Count > 0;

        return
        [
            new EventOption(
                this,
                canBuyPaint ? BuyPaint : null,
                InitialOptionKey(canBuyPaint ? "BUY_PAINT" : "BUY_PAINT_LOCKED")),
            new EventOption(
                this,
                canHelp ? HelpGardeners : null,
                InitialOptionKey(canHelp ? "HELP" : "HELP_LOCKED")),
        ];
    }

    private async Task BuyPaint()
    {
        await PlayerCmd.LoseGold(PaintGoldCost, Owner!, GoldLossType.Spent);

        foreach (CardModel card in GetNonHeartSuitCards(Owner!).ToList())
        {
            RepaintAsHeart(card);
            ShowEnchantVfx(card);
        }

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.BUY_PAINT.description"));
    }

    private Task HelpGardeners()
    {
        List<CardModel> candidates = GetHeartCandidates(Owner!);
        if (candidates.Count > 0)
        {
            CardModel? selected = Owner!.RunState.Rng.Niche.NextItem(candidates);
            if (selected != null)
            {
                decimal amount = Owner.RunState.Rng.Niche.NextInt(PlayingCardSuitEnchantment.MaxTriggersPerCombat)
                    + PlayingCardSuitEnchantment.MinTriggersPerCombat;
                CardCmd.Enchant<HeartSuitEnchantment>(selected, amount);
                ShowEnchantVfx(selected);
            }
        }

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.HELP.description"));
        return Task.CompletedTask;
    }

    private static bool HasGardenerCondition(Player player)
    {
        return player.GetRelic<QueenOfHeartsPlayingCardDeckRelic>() != null
            && GetNonHeartSuitCards(player).Count >= RequiredNonHeartCards;
    }

    private static List<CardModel> GetNonHeartSuitCards(Player player)
    {
        return PileType.Deck.GetPile(player).Cards
            .Where(card => card.Enchantment is PlayingCardSuitEnchantment { PokerSuit: not PlayingCardSuit.Heart })
            .ToList();
    }

    private static List<CardModel> GetHeartCandidates(Player player)
    {
        return PileType.Deck.GetPile(player).Cards
            .Where(card => card.Enchantment == null && ModelDb.Enchantment<HeartSuitEnchantment>().CanEnchant(card))
            .ToList();
    }

    private static void RepaintAsHeart(CardModel card)
    {
        if (card.Enchantment is not PlayingCardSuitEnchantment original)
        {
            return;
        }

        decimal rank = original.Amount;
        int remainingTriggers = original.BlackSouls_TriggerBudgetInitialized
            ? original.BlackSouls_RemainingTriggers
            : Math.Clamp((int)rank, PlayingCardSuitEnchantment.MinTriggersPerCombat, PlayingCardSuitEnchantment.MaxTriggersPerCombat);

        CardCmd.ClearEnchantment(card);
        CardCmd.Enchant<HeartSuitEnchantment>(card, rank);

        if (card.Enchantment is HeartSuitEnchantment heart)
        {
            // The new suit keeps both the displayed rank and the already-spent run-wide budget.
            heart.BlackSouls_RemainingTriggers = remainingTriggers;
            heart.BlackSouls_TriggerBudgetInitialized = true;
            heart.RecalculateValues();
        }
    }

    private static void ShowEnchantVfx(CardModel card)
    {
        NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
        if (vfx != null)
        {
            NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
        }
    }
}
