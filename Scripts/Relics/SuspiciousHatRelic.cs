using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands.Builders;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class SuspiciousHatRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool SpawnsPets => !IsCanonical && (CopiedRelic()?.SpawnsPets ?? false);

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (IsCanonical)
            {
                return [
                    new HoverTip(
                        new LocString("relics", $"{Id.Entry}.details.title"),
                        "复制你遗物栏中位于最右侧，且稀有度不是先古的原版遗物。若没有符合条件的遗物，则不产生效果。")
                ];
            }

            RelicModel? copied = CopiedRelic();
            string copiedTitle = copied?.Title.GetFormattedText() ?? "无";
            string description =
                $"当前复制：[gold]{copiedTitle}[/gold]。只会复制你遗物栏中位于最右侧，且稀有度不是先古的遗物。若没有符合条件的遗物，则不产生效果。";

            return [
                new HoverTip(new LocString("relics", $"{Id.Entry}.details.title"), description)
            ];
        }
    }

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/SuspiciousHatRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/SuspiciousHatRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/SuspiciousHatRelic.png"
    );

    public override Task BeforeCombatStart()
    {
        RelicModel? copied = CopiedRelic();
        if (copied is BoundPhylactery)
        {
            return OstyCmd.Summon(new ThrowingPlayerChoiceContext(), Owner, copied.DynamicVars.Summon.BaseValue, this);
        }

        if (copied is PhylacteryUnbound)
        {
            return OstyCmd.Summon(new ThrowingPlayerChoiceContext(), Owner, copied.DynamicVars["StartOfCombat"].BaseValue, this);
        }

        return copied?.BeforeCombatStart() ?? Task.CompletedTask;
    }

    public override Task BeforeCombatStartLate()
    {
        return CopiedRelic()?.BeforeCombatStartLate() ?? Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        return CopiedRelic()?.AfterRoomEntered(room) ?? Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        return CopiedRelic()?.BeforeSideTurnStart(choiceContext, side, combatState) ?? Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        return CopiedRelic()?.AfterPlayerTurnStart(choiceContext, player) ?? Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        return CopiedRelic()?.AfterPlayerTurnStartLate(choiceContext, player) ?? Task.CompletedTask;
    }

    public override Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        RelicModel? copied = CopiedRelic();
        if (copied is PhylacteryUnbound && side == CombatSide.Player)
        {
            return OstyCmd.Summon(new ThrowingPlayerChoiceContext(), Owner, copied.DynamicVars["StartOfTurn"].BaseValue, this);
        }

        return copied?.AfterSideTurnStart(side, combatState) ?? Task.CompletedTask;
    }

    public override Task BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)
    {
        return CopiedRelic()?.BeforePlayPhaseStart(choiceContext, player) ?? Task.CompletedTask;
    }

    public override Task BeforePlayPhaseStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        return CopiedRelic()?.BeforePlayPhaseStartLate(choiceContext, player) ?? Task.CompletedTask;
    }

    public override Task BeforeTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side)
    {
        return CopiedRelic()?.BeforeTurnEndVeryEarly(choiceContext, side) ?? Task.CompletedTask;
    }

    public override Task BeforeTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side)
    {
        return CopiedRelic()?.BeforeTurnEndEarly(choiceContext, side) ?? Task.CompletedTask;
    }

    public override Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        return CopiedRelic()?.BeforeTurnEnd(choiceContext, side) ?? Task.CompletedTask;
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        return CopiedRelic()?.AfterTurnEnd(choiceContext, side) ?? Task.CompletedTask;
    }

    public override Task AfterEnergyReset(Player player)
    {
        return CopiedRelic()?.AfterEnergyReset(player) ?? Task.CompletedTask;
    }

    public override Task AfterEnergyResetLate(Player player)
    {
        RelicModel? copied = CopiedRelic();
        if (copied is BoundPhylactery
            && player == Owner
            && player.Creature.CombatState?.RoundNumber != 1)
        {
            return OstyCmd.Summon(new ThrowingPlayerChoiceContext(), Owner, copied.DynamicVars.Summon.BaseValue, this);
        }

        return copied?.AfterEnergyResetLate(player) ?? Task.CompletedTask;
    }

    public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        return CopiedRelic()?.BeforeHandDraw(player, choiceContext, combatState) ?? Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        return CopiedRelic()?.AfterCombatEnd(room) ?? Task.CompletedTask;
    }

    public override Task AfterCombatVictoryEarly(CombatRoom room)
    {
        return CopiedRelic()?.AfterCombatVictoryEarly(room) ?? Task.CompletedTask;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        return CopiedRelic()?.AfterCombatVictory(room) ?? Task.CompletedTask;
    }

    public override Task AfterCreatureAddedToCombat(Creature creature)
    {
        return CopiedRelic()?.AfterCreatureAddedToCombat(creature) ?? Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        return CopiedRelic()?.AfterCardEnteredCombat(card) ?? Task.CompletedTask;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        return CopiedRelic()?.BeforeCardPlayed(cardPlay) ?? Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        return CopiedRelic()?.AfterCardPlayed(context, cardPlay) ?? Task.CompletedTask;
    }

    public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return CopiedRelic()?.AfterCardPlayedLate(choiceContext, cardPlay) ?? Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPile, AbstractModel? source)
    {
        return CopiedRelic()?.AfterCardChangedPiles(card, oldPile, source) ?? Task.CompletedTask;
    }

    public override Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        return CopiedRelic()?.AfterCardDiscarded(choiceContext, card) ?? Task.CompletedTask;
    }

    public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        return CopiedRelic()?.AfterCardExhausted(choiceContext, card, causedByEthereal) ?? Task.CompletedTask;
    }

    public override Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
    {
        return CopiedRelic()?.AfterShuffle(choiceContext, shuffler) ?? Task.CompletedTask;
    }

    public override Task AfterHandEmptied(PlayerChoiceContext choiceContext, Player player)
    {
        return CopiedRelic()?.AfterHandEmptied(choiceContext, player) ?? Task.CompletedTask;
    }

    public override Task AfterStarsSpent(int amount, Player spender)
    {
        return CopiedRelic()?.AfterStarsSpent(amount, spender) ?? Task.CompletedTask;
    }

    public override Task AfterTakingExtraTurn(Player player)
    {
        return CopiedRelic()?.AfterTakingExtraTurn(player) ?? Task.CompletedTask;
    }

    public override Task AfterAttack(AttackCommand command)
    {
        return CopiedRelic()?.AfterAttack(command) ?? Task.CompletedTask;
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        return CopiedRelic()?.AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource) ?? Task.CompletedTask;
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        return CopiedRelic()?.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource) ?? Task.CompletedTask;
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        return CopiedRelic()?.AfterCurrentHpChanged(creature, delta) ?? Task.CompletedTask;
    }

    public override Task AfterBlockCleared(Creature creature)
    {
        return CopiedRelic()?.AfterBlockCleared(creature) ?? Task.CompletedTask;
    }

    public override Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
    {
        return CopiedRelic()?.AfterPreventingBlockClear(preventer, creature) ?? Task.CompletedTask;
    }

    public override Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature target,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        return CopiedRelic()?.AfterDeath(choiceContext, target, wasRemovalPrevented, deathAnimLength) ?? Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        return CopiedRelic()?.ModifyDamageAdditive(target, amount, props, dealer, cardSource) ?? 0;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        return CopiedRelic()?.ModifyDamageMultiplicative(target, amount, props, dealer, cardSource) ?? 1;
    }

    public override decimal ModifyBlockAdditive(
        Creature target,
        decimal amount,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        return CopiedRelic()?.ModifyBlockAdditive(target, amount, props, cardSource, cardPlay) ?? 0;
    }

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        return CopiedRelic()?.ModifyBlockMultiplicative(target, block, props, cardSource, cardPlay) ?? 1;
    }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        return CopiedRelic()?.ModifyHandDraw(player, count) ?? count;
    }

    public override decimal ModifyHandDrawLate(Player player, decimal count)
    {
        return CopiedRelic()?.ModifyHandDrawLate(player, count) ?? count;
    }

    public override decimal ModifyHpLostBeforeOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        return CopiedRelic()?.ModifyHpLostBeforeOsty(target, amount, props, dealer, cardSource) ?? amount;
    }

    public override decimal ModifyHpLostAfterOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        return CopiedRelic()?.ModifyHpLostAfterOsty(target, amount, props, dealer, cardSource) ?? amount;
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return CopiedRelic()?.ModifyMaxEnergy(player, amount) ?? amount;
    }

    public override int ModifyXValue(CardModel card, int originalValue)
    {
        return CopiedRelic()?.ModifyXValue(card, originalValue) ?? originalValue;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        RelicModel? copied = CopiedRelic();
        if (copied != null)
        {
            return copied.TryModifyEnergyCostInCombat(card, originalCost, out modifiedCost);
        }

        modifiedCost = originalCost;
        return false;
    }

    public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        RelicModel? copied = CopiedRelic();
        if (copied != null)
        {
            return copied.TryModifyStarCost(card, originalCost, out modifiedCost);
        }

        modifiedCost = originalCost;
        return false;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        return CopiedRelic()?.ModifyCardPlayCount(card, target, playCount) ?? playCount;
    }

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        return CopiedRelic()?.AfterModifyingCardPlayCount(card) ?? Task.CompletedTask;
    }

    public override Task AfterModifyingHpLostBeforeOsty()
    {
        return CopiedRelic()?.AfterModifyingHpLostBeforeOsty() ?? Task.CompletedTask;
    }

    public override Task AfterModifyingHpLostAfterOsty()
    {
        return CopiedRelic()?.AfterModifyingHpLostAfterOsty() ?? Task.CompletedTask;
    }

    public override bool ShouldClearBlock(Creature creature)
    {
        return CopiedRelic()?.ShouldClearBlock(creature) ?? true;
    }

    public override bool ShouldDraw(Player player, bool fromHandDraw)
    {
        return CopiedRelic()?.ShouldDraw(player, fromHandDraw) ?? true;
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        return CopiedRelic()?.ShouldPlay(card, autoPlayType) ?? true;
    }

    public override bool ShouldTakeExtraTurn(Player player)
    {
        return CopiedRelic()?.ShouldTakeExtraTurn(player) ?? false;
    }

    public override bool ShouldPlayerResetEnergy(Player player)
    {
        return CopiedRelic()?.ShouldPlayerResetEnergy(player) ?? true;
    }

    public override decimal ModifyPowerAmountGiven(
        PowerModel power,
        Creature giver,
        decimal amount,
        Creature? target,
        CardModel? cardSource)
    {
        return CopiedRelic()?.ModifyPowerAmountGiven(power, giver, amount, target, cardSource) ?? amount;
    }

    public override Task AfterModifyingPowerAmountGiven(PowerModel power)
    {
        return CopiedRelic()?.AfterModifyingPowerAmountGiven(power) ?? Task.CompletedTask;
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        RelicModel? copied = CopiedRelic();
        if (copied != null)
        {
            return copied.TryModifyPowerAmountReceived(canonicalPower, target, amount, applier, out modifiedAmount);
        }

        modifiedAmount = amount;
        return false;
    }

    public override Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        return CopiedRelic()?.AfterModifyingPowerAmountReceived(power) ?? Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        return CopiedRelic()?.AfterPowerAmountChanged(power, amount, applier, cardSource) ?? Task.CompletedTask;
    }

    private RelicModel? CopiedRelic()
    {
        return Owner?.Relics
            .Where(relic => relic is not SuspiciousHatRelic
                && relic.Rarity != RelicRarity.Ancient
                && IsVanillaRelic(relic))
            .LastOrDefault();
    }

    private static bool IsVanillaRelic(RelicModel relic)
    {
        return relic.GetType().Namespace == "MegaCrit.Sts2.Core.Models.Relics";
    }
}
