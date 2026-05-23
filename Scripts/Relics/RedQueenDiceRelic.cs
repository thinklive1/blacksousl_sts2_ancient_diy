using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class RedQueenDiceRelic : ModRelicTemplate
{
    internal const int RollRange = 3;
    private const int RerollEnergyGain = 1;

    private readonly Dictionary<CardModel, int> _cardRolls = [];
    private bool _hasPlayedCardThisTurn;
    private CardModel? _firstCardPlayedThisTurn;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar("RollRange", RollRange),
        new EnergyVar("RerollEnergyGain", RerollEnergyGain)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<RedQueenRerollCard>()
            .Prepend(RelicHoverTipHelpers.Details(this, "diceDetails"));

    internal static IEnumerable<DynamicVar> DiceStatusVars => [
        new CardsVar("RollRange", RollRange)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png"
    );

    public override Task BeforeCombatStart()
    {
        _cardRolls.Clear();
        _hasPlayedCardThisTurn = false;
        _firstCardPlayedThisTurn = null;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        Flash();
        _hasPlayedCardThisTurn = false;
        _firstCardPlayedThisTurn = null;
        await AddRerollCardToHand();
        RollHand();
        await SyncDiceStatus();
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (_hasPlayedCardThisTurn || card.Owner != Owner)
        {
            return playCount;
        }

        _hasPlayedCardThisTurn = true;
        _firstCardPlayedThisTurn = card;
        return playCount;
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPile, AbstractModel? source)
    {
        if (card.Owner == Owner && oldPile == PileType.Hand)
        {
            RefreshCardPreview(card);
            await SyncDiceStatus();
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Creature.Side)
        {
            return;
        }

        ResetRolledCards();
        _hasPlayedCardThisTurn = false;
        _firstCardPlayedThisTurn = null;
        await SyncDiceStatus();
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        _cardRolls.Clear();
        _hasPlayedCardThisTurn = false;
        _firstCardPlayedThisTurn = null;
        return Task.CompletedTask;
    }

    public async Task Reroll(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (_firstCardPlayedThisTurn != card)
        {
            return;
        }

        Flash();
        RollHand();
        await SyncDiceStatus();
        await PlayerCmd.GainEnergy(RerollEnergyGain, Owner);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        return GetCardRoll(cardSource, amount);
    }

    public override decimal ModifyBlockAdditive(
        Creature target,
        decimal amount,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        return GetCardRoll(cardSource, amount);
    }

    private async Task AddRerollCardToHand()
    {
        CombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        CardModel card = combatState.CreateCard<RedQueenRerollCard>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
    }

    private void RollHand()
    {
        ResetRolledCards();

        foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards)
        {
            _cardRolls[card] = RollOffset();
            RefreshCardPreview(card);
        }
    }

    private int RollOffset()
    {
        return Owner.RunState.Rng.CombatCardSelection.NextInt(-RollRange, RollRange + 1);
    }

    private decimal GetCardRoll(CardModel? card, decimal amount)
    {
        if (!IsAffectedCard(card) || !_cardRolls.TryGetValue(card!, out int roll))
        {
            return 0;
        }

        return Math.Max(roll, -amount);
    }

    private Task SyncDiceStatus()
    {
        return PowerCmd.SetAmount<RedQueenDiceCurrentPower>(
            Owner.Creature,
            ActiveRolledCardCount(),
            Owner.Creature,
            null);
    }

    private int ActiveRolledCardCount()
    {
        return _cardRolls.Keys.Count(IsAffectedCard);
    }

    private void ResetRolledCards()
    {
        foreach (CardModel card in _cardRolls.Keys.ToList())
        {
            RefreshCardPreview(card);
        }

        _cardRolls.Clear();
    }

    private bool IsAffectedCard(CardModel? card)
    {
        return CombatManager.Instance.IsInProgress
            && card?.Owner == Owner
            && card.Pile?.Type == PileType.Hand;
    }

    private void RefreshCardPreview(CardModel card)
    {
        card.UpdateDynamicVarPreview(CardPreviewMode.Normal, Owner.Creature, card.DynamicVars);
    }
}
