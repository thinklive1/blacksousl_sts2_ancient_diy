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
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using BlackSouls.Scripts.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class RedQueenDiceRelic : ModRelicTemplate
{
    internal const int RollRange = 3;
    private const int MaxEnergyGain = 1;
    private bool _bigSuccessTriggered;
    private int _rerollIndex;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count == 1;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar("RollRange", RollRange),
        new EnergyVar("Energy", MaxEnergyGain)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [RelicHoverTipHelpers.Details(this, "diceDetails"), .. HoverTipFactory.FromCardWithCardHoverTips<RedQueenBigSuccessCard>()];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png"
    );

    [SavedProperty]
    public bool BlackSouls_BigSuccessTriggered
    {
        get => _bigSuccessTriggered;
        set
        {
            AssertMutable();
            _bigSuccessTriggered = value;
        }
    }

    public override Task BeforeCombatStart()
    {
        _rerollIndex = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        _rerollIndex = 0;
        RefreshAllCombatCards();
        await TryTriggerBigSuccess();
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPile, AbstractModel? source)
    {
        if (card.Owner == Owner)
        {
            RefreshCardPreview(card);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Creature.Side)
        {
            return;
        }

        RefreshAllCombatCards();
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

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return player == Owner ? amount + DynamicVars.Energy.BaseValue * MaxEnergyGain : amount;
    }

    public async Task Reroll(PlayerChoiceContext choiceContext, CardModel sourceCard)
    {
        if (sourceCard.Owner != Owner)
        {
            return;
        }

        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        _rerollIndex++;
        RefreshAllCombatCards();
        await TryTriggerBigSuccess();
    }

    private async Task TryTriggerBigSuccess()
    {
        if (BlackSouls_BigSuccessTriggered)
        {
            return;
        }

        var combatState = Owner.PlayerCombatState;
        if (combatState is null)
        {
            return;
        }

        List<CardModel> rollableCards = combatState.AllCards
            .Where(IsRollableCard)
            .ToList();
        if (rollableCards.Count == 0)
        {
            return;
        }

        int requiredSuccesses = Math.Max(1, (int)Math.Ceiling(rollableCards.Count / 3m));
        int successes = rollableCards.Count(card => GetDeterministicRoll(card) >= RollRange);
        if (successes < requiredSuccesses)
        {
            return;
        }

        BlackSouls_BigSuccessTriggered = true;
        Flash();
        ICombatState? currentCombat = Owner.Creature.CombatState;
        if (currentCombat == null)
        {
            return;
        }

        CardModel card = currentCombat.CreateCard<RedQueenBigSuccessCard>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner, CardPilePosition.Top);
    }

    private void RefreshAllCombatCards()
    {
        var combatState = Owner.PlayerCombatState;
        if (combatState is null)
        {
            return;
        }

        foreach (CardModel card in combatState.AllCards.ToList())
        {
            if (IsAffectedCard(card))
            {
                RefreshCardPreview(card);
            }
        }
    }

    private decimal GetCardRoll(CardModel? card, decimal amount)
    {
        if (!IsAffectedCard(card))
        {
            return 0;
        }

        int roll = GetDeterministicRoll(card!);
        return Math.Max(roll, -amount);
    }

    private int GetDeterministicRoll(CardModel card)
    {
        ICombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return 0;
        }

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Owner.NetId.GetHashCode();
            hash = hash * 31 + card.EntrySortingId;
            hash = hash * 31 + card.CurrentUpgradeLevel;
            hash = hash * 31 + GetCombatCardIndex(card);
            hash = hash * 31 + combatState.RoundNumber;
            hash = hash * 31 + GetRerollIndex();
            hash ^= hash >> 16;
            hash *= 0x45d9f3b;
            hash ^= hash >> 16;
            int range = RollRange * 2 + 1;
            return Math.Abs(hash % range) - RollRange;
        }
    }

    private bool IsAffectedCard(CardModel? card)
    {
        return CombatManager.Instance.IsInProgress
            && card?.Owner == Owner
            && card.IsInCombat;
    }

    private bool IsRollableCard(CardModel? card)
    {
        return IsAffectedCard(card)
            && (card!.Type == CardType.Attack || card.GainsBlock);
    }

    private void RefreshCardPreview(CardModel card)
    {
        card.UpdateDynamicVarPreview(CardPreviewMode.Normal, Owner.Creature, card.DynamicVars);
    }

    private int GetCombatCardIndex(CardModel card)
    {
        return Owner.PlayerCombatState?.AllCards.ToList().IndexOf(card) ?? 0;
    }

    private int GetRerollIndex()
    {
        return _rerollIndex;
    }
}
