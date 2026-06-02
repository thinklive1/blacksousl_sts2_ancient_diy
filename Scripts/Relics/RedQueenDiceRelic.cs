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

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar("RollRange", RollRange),
        new EnergyVar("Energy", MaxEnergyGain)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [RelicHoverTipHelpers.Details(this, "diceDetails")];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png"
    );

    public override async Task BeforeCombatStart()
    {
        await SetRerollIndex(0);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        await SetRerollIndex(0);
        RefreshAllCombatCards();
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPile, AbstractModel? source)
    {
        if (card.Owner == Owner)
        {
            RefreshCardPreview(card);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
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
        await IncrementRerollIndex();
        RefreshAllCombatCards();
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
        CombatState? combatState = Owner.Creature.CombatState;
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
        return Owner.Creature.GetPower<RedQueenDiceRerollPower>()?.Amount ?? 0;
    }

    private Task SetRerollIndex(int index)
    {
        if (Owner?.Creature == null || !CombatManager.Instance.IsInProgress)
        {
            return Task.CompletedTask;
        }

        return PowerCmd.SetAmount<RedQueenDiceRerollPower>(Owner.Creature, index, Owner.Creature, null);
    }

    private Task IncrementRerollIndex()
    {
        if (Owner?.Creature == null || !CombatManager.Instance.IsInProgress)
        {
            return Task.CompletedTask;
        }

        RedQueenDiceRerollPower? power = Owner.Creature.GetPower<RedQueenDiceRerollPower>();
        return power == null
            ? SetRerollIndex(1)
            : PowerCmd.ModifyAmount(power, 1, Owner.Creature, null);
    }
}
