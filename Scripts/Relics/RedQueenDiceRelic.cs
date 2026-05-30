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
    private const int MaxEnergyGain = 1;

    private readonly Dictionary<CardModel, int> _cardRolls = [];

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar("RollRange", RollRange),
        new EnergyVar("Energy", MaxEnergyGain)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [RelicHoverTipHelpers.Details(this, "diceDetails")];

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
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return Task.CompletedTask;
        }

        RollAllCombatCards();
        return SyncDiceStatus();
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPile, AbstractModel? source)
    {
        if (card.Owner == Owner)
        {
            if (IsAffectedCard(card) && !_cardRolls.ContainsKey(card))
            {
                _cardRolls[card] = RollOffset();
            }

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
        await SyncDiceStatus();
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        _cardRolls.Clear();
        return Task.CompletedTask;
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
        RollAllCombatCards();
        await SyncDiceStatus();
    }

    private void RollAllCombatCards()
    {
        ResetRolledCards();

        var combatState = Owner.PlayerCombatState;
        if (combatState is null)
        {
            return;
        }

        foreach (CardModel card in combatState.AllCards.ToList())
        {
            if (IsAffectedCard(card))
            {
                _cardRolls[card] = RollOffset();
                RefreshCardPreview(card);
            }
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
            && card.IsInCombat;
    }

    private void RefreshCardPreview(CardModel card)
    {
        card.UpdateDynamicVarPreview(CardPreviewMode.Normal, Owner.Creature, card.DynamicVars);
    }
}
