using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterEnchantment]
public class FeedingEnchantment : ModEnchantmentTemplate
{
    public const int InitialDamagePercent = 150;
    private const int CombatEndDecay = 15;
    private const int KillGrowth = 50;
    private const int MinDamagePercent = 50;
    private const int MaxDamagePercent = 150;

    private int _killsThisCombat;
    private bool _isPlayingThisCard;
    private readonly HashSet<Creature> _damagedTargetsThisPlay = [];

    public override bool ShowAmount => true;

    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/HorrifyingGluttonRelic.png"
    );

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType == CardType.Attack;
    }

    public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props)
    {
        return Amount / 100m;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card == Card)
        {
            _isPlayingThisCard = true;
            _damagedTargetsThisPlay.Clear();
        }

        return Task.CompletedTask;
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (IsDamageFromThisCard(dealer, target, cardSource) && result.TotalDamage > 0)
        {
            _damagedTargetsThisPlay.Add(target);
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card != Card)
        {
            return Task.CompletedTask;
        }

        int kills = _damagedTargetsThisPlay.Count(target => target.IsDead);
        if (kills > 0)
        {
            AddKills(kills);
        }

        _isPlayingThisCard = false;
        _damagedTargetsThisPlay.Clear();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (TryGetDeckFeedingEnchantment(out _))
        {
            _killsThisCombat = 0;
            return Task.CompletedTask;
        }

        SetAmount(Math.Max(MinDamagePercent, Amount - CombatEndDecay));
        if (_killsThisCombat > 0)
        {
            SetAmount(Math.Min(MaxDamagePercent, Amount + KillGrowth * _killsThisCombat));
            _killsThisCombat = 0;
        }

        return Task.CompletedTask;
    }

    private bool IsDamageFromThisCard(Creature? dealer, Creature target, CardModel? cardSource)
    {
        return _isPlayingThisCard
            && dealer == Card.Owner?.Creature
            && target.Side != dealer?.Side
            && (cardSource == Card || cardSource?.DeckVersion == Card || cardSource == Card.DeckVersion);
    }

    private void AddKills(int kills)
    {
        if (TryGetDeckFeedingEnchantment(out FeedingEnchantment? deckFeeding) && deckFeeding != null)
        {
            deckFeeding._killsThisCombat += kills;
            return;
        }

        _killsThisCombat += kills;
    }

    private bool TryGetDeckFeedingEnchantment(out FeedingEnchantment? deckFeeding)
    {
        deckFeeding = Card.DeckVersion?.Enchantment as FeedingEnchantment;
        return deckFeeding != null && deckFeeding != this;
    }

    private void SetAmount(int amount)
    {
        Amount = amount;
        Card.DynamicVars.RecalculateForUpgradeOrEnchant();
    }
}
