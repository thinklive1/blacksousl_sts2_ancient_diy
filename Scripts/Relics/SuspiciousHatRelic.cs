using System.Text.Json;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Merchant;
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
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands.Builders;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

#pragma warning disable CS8765, CS8604
/// <summary>Implements the Suspicious Hat relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class SuspiciousHatRelic : ModRelicTemplate
{
    private const string LocalizationPrefix = "BS_ANCIENT_RELIC_SUSPICIOUS_HAT_RELIC";
    private static readonly JsonSerializerOptions CopySaveOptions = new() { IncludeFields = true };
    private SuspiciousHatCopyState<RelicModel> _copyState = new();

    [SavedProperty]
    public string BlackSouls_CopiedRelicState
    {
        get => _copyState.Serialize(SerializeCopy);
        set
        {
            AssertMutable();
            _copyState.Restore(value ?? string.Empty, ReleaseCopy);
        }
    }

    public override bool ShowCounter => !IsCanonical && (CopiedRelic()?.ShowCounter ?? false);

    public override int DisplayAmount => !IsCanonical ? CopiedRelic()?.DisplayAmount ?? 0 : 0;

    protected override void DeepCloneFields()
    {
        // Do not share a proxy, its mutable collections, or callbacks with another hat.
        string saved = _copyState.Serialize(SerializeCopy);
        base.DeepCloneFields();
        _copyState = new();
        _copyState.Restore(saved, ReleaseCopy);
    }

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool SpawnsPets => !IsCanonical && (DisplaySource()?.SpawnsPets ?? false);

    public override bool AddsPet => !IsCanonical && (DisplaySource()?.AddsPet ?? false);

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
                        new LocString("relics", $"{LocalizationPrefix}.details.title"),
                        GetRelicText("details.description"))
                ];
            }

            RelicModel? copied = DisplaySource();
            string copiedTitle = copied?.Title.GetFormattedText() ?? GetRelicText("details.none");
            LocString description = new("relics", $"{LocalizationPrefix}.details.currentDescription");
            description.Add("CopiedTitle", copiedTitle);

            return [
                new HoverTip(
                    new LocString("relics", $"{LocalizationPrefix}.details.title"),
                    description.GetFormattedText())
            ];
        }
    }

    private static string GetRelicText(string key)
    {
        return new LocString("relics", $"{LocalizationPrefix}.{key}").GetFormattedText();
    }

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/SuspiciousHatRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/SuspiciousHatRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/SuspiciousHatRelic.png"
    );

    public override Task AfterObtained()
    {
        RefreshCopiedRelic();
        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        RelicModel? copied = CopyForGameplayHook();
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
        return CopyForGameplayHook()?.BeforeCombatStartLate() ?? Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        return CopyForGameplayHook()?.AfterRoomEntered(room) ?? Task.CompletedTask;
    }

    public override Task AfterRemoved()
    {
        _copyState.Clear(ReleaseCopy);
        return base.AfterRemoved();
    }

    public override Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
    {
        return CopyForGameplayHook()?.AfterItemPurchased(player, itemPurchased, goldSpent) ?? Task.CompletedTask;
    }

    public override Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        return CopyForGameplayHook()?.AfterPotionUsed(potion, target) ?? Task.CompletedTask;
    }

    public override Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        return CopyForGameplayHook()?.AfterRestSiteHeal(player, isMimicked) ?? Task.CompletedTask;
    }

    public override Task AfterRestSiteSmith(Player player)
    {
        return CopyForGameplayHook()?.AfterRestSiteSmith(player) ?? Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        return CopyForGameplayHook()?.BeforeSideTurnStart(choiceContext, side, participants, combatState) ?? Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        return CopyForGameplayHook()?.AfterPlayerTurnStart(choiceContext, player) ?? Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        return CopyForGameplayHook()?.AfterPlayerTurnStartLate(choiceContext, player) ?? Task.CompletedTask;
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        RelicModel? copied = CopyForGameplayHook();
        if (copied is PhylacteryUnbound && side == CombatSide.Player)
        {
            return OstyCmd.Summon(new ThrowingPlayerChoiceContext(), Owner, copied.DynamicVars["StartOfTurn"].BaseValue, this);
        }

        return copied?.AfterSideTurnStart(side, participants, combatState) ?? Task.CompletedTask;
    }

    public override Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        return CopyForGameplayHook()?.AfterAutoPrePlayPhaseEntered(choiceContext, player) ?? Task.CompletedTask;
    }

    public override Task AfterAutoPrePlayPhaseEnteredLate(PlayerChoiceContext choiceContext, Player player)
    {
        return CopyForGameplayHook()?.AfterAutoPrePlayPhaseEnteredLate(choiceContext, player) ?? Task.CompletedTask;
    }

    public override Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        return CopyForGameplayHook()?.BeforeSideTurnEndVeryEarly(choiceContext, side, participants) ?? Task.CompletedTask;
    }

    public override Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        return CopyForGameplayHook()?.BeforeSideTurnEndEarly(choiceContext, side, participants) ?? Task.CompletedTask;
    }

    public override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        return CopyForGameplayHook()?.BeforeSideTurnEnd(choiceContext, side, participants) ?? Task.CompletedTask;
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        return CopyForGameplayHook()?.AfterSideTurnEnd(choiceContext, side, participants) ?? Task.CompletedTask;
    }

    public override Task AfterEnergyReset(Player player)
    {
        return CopyForGameplayHook()?.AfterEnergyReset(player) ?? Task.CompletedTask;
    }

    public override Task AfterEnergyResetLate(Player player)
    {
        RelicModel? copied = CopyForGameplayHook();
        if (copied is BoundPhylactery
            && player == Owner
            && player.Creature.CombatState?.RoundNumber != 1)
        {
            return OstyCmd.Summon(new ThrowingPlayerChoiceContext(), Owner, copied.DynamicVars.Summon.BaseValue, this);
        }

        return copied?.AfterEnergyResetLate(player) ?? Task.CompletedTask;
    }

    public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        return CopyForGameplayHook()?.BeforeHandDraw(player, choiceContext, combatState) ?? Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        return CopyForGameplayHook()?.AfterCombatEnd(room) ?? Task.CompletedTask;
    }

    public override Task AfterCombatVictoryEarly(CombatRoom room)
    {
        return CopyForGameplayHook()?.AfterCombatVictoryEarly(room) ?? Task.CompletedTask;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        return CopyForGameplayHook()?.AfterCombatVictory(room) ?? Task.CompletedTask;
    }

    public override Task AfterCreatureAddedToCombat(Creature creature)
    {
        return CopyForGameplayHook()?.AfterCreatureAddedToCombat(creature) ?? Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        return CopyForGameplayHook()?.AfterCardEnteredCombat(card) ?? Task.CompletedTask;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        return CopyForGameplayHook()?.BeforeCardPlayed(cardPlay) ?? Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        return CopyForGameplayHook()?.AfterCardPlayed(context, cardPlay) ?? Task.CompletedTask;
    }

    public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return CopyForGameplayHook()?.AfterCardPlayedLate(choiceContext, cardPlay) ?? Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPile, AbstractModel? source)
    {
        return CopyForGameplayHook()?.AfterCardChangedPiles(card, oldPile, source) ?? Task.CompletedTask;
    }

    public override Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        return CopyForGameplayHook()?.AfterCardDiscarded(choiceContext, card) ?? Task.CompletedTask;
    }

    public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        return CopyForGameplayHook()?.AfterCardExhausted(choiceContext, card, causedByEthereal) ?? Task.CompletedTask;
    }

    public override Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
    {
        return CopyForGameplayHook()?.AfterShuffle(choiceContext, shuffler) ?? Task.CompletedTask;
    }

    public override Task AfterHandEmptied(PlayerChoiceContext choiceContext, Player player)
    {
        return CopyForGameplayHook()?.AfterHandEmptied(choiceContext, player) ?? Task.CompletedTask;
    }

    public override Task AfterStarsSpent(int amount, Player spender)
    {
        return CopyForGameplayHook()?.AfterStarsSpent(amount, spender) ?? Task.CompletedTask;
    }

    public override Task AfterTakingExtraTurn(Player player)
    {
        return CopyForGameplayHook()?.AfterTakingExtraTurn(player) ?? Task.CompletedTask;
    }

    public override Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        return CopyForGameplayHook()?.AfterAttack(choiceContext, command) ?? Task.CompletedTask;
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        return CopyForGameplayHook()?.AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource) ?? Task.CompletedTask;
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        return CopyForGameplayHook()?.AfterDamageReceived(choiceContext, target, result, props, dealer, cardSource) ?? Task.CompletedTask;
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        return CopyForGameplayHook()?.AfterCurrentHpChanged(creature, delta) ?? Task.CompletedTask;
    }

    public override Task AfterBlockCleared(Creature creature)
    {
        return CopyForGameplayHook()?.AfterBlockCleared(creature) ?? Task.CompletedTask;
    }

    public override Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
    {
        return CopyForGameplayHook()?.AfterPreventingBlockClear(preventer, creature) ?? Task.CompletedTask;
    }

    public override Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature target,
#if BS_BETA_1130
        CardModel? cardSource,
#endif
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        return CopyForGameplayHook()?.AfterDeath(choiceContext, target,
#if BS_BETA_1130
            cardSource,
#endif
            wasRemovalPrevented, deathAnimLength) ?? Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
#if BS_BETA_1110
        , CardPlay? cardPlay
#endif
        )
    {
#if BS_BETA_1110
        return CopiedRelic()?.ModifyDamageAdditive(target, amount, props, dealer, cardSource, cardPlay) ?? 0;
#else
        return CopiedRelic()?.ModifyDamageAdditive(target, amount, props, dealer, cardSource) ?? 0;
#endif
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
#if BS_BETA_1110
        , CardPlay? cardPlay
#endif
        )
    {
#if BS_BETA_1110
        return CopiedRelic()?.ModifyDamageMultiplicative(target, amount, props, dealer, cardSource, cardPlay) ?? 1;
#else
        return CopiedRelic()?.ModifyDamageMultiplicative(target, amount, props, dealer, cardSource) ?? 1;
#endif
    }

    public override decimal ModifyBlockAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        return CopiedRelic()?.ModifyBlockAdditive(target, amount, props, cardSource, cardPlay) ?? 0;
    }

    public override decimal ModifyBlockMultiplicative(
        Creature? target,
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
        Creature? target,
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
        return CopyForGameplayHook()?.AfterModifyingCardPlayCount(card) ?? Task.CompletedTask;
    }

    public override Task AfterModifyingHpLostBeforeOsty()
    {
        return CopyForGameplayHook()?.AfterModifyingHpLostBeforeOsty() ?? Task.CompletedTask;
    }

    public override Task AfterModifyingHpLostAfterOsty()
    {
        return CopyForGameplayHook()?.AfterModifyingHpLostAfterOsty() ?? Task.CompletedTask;
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

    public override decimal ModifyPowerAmountGivenAdditive(
        PowerModel power,
        Creature giver,
        decimal amount,
        Creature target,
        CardModel? cardSource)
    {
        return CopiedRelic()?.ModifyPowerAmountGivenAdditive(power, giver, amount, target, cardSource) ?? 0m;
    }

    public override decimal ModifyPowerAmountGivenMultiplicative(
        PowerModel power,
        Creature giver,
        decimal amount,
        Creature target,
        CardModel? cardSource)
    {
        return CopiedRelic()?.ModifyPowerAmountGivenMultiplicative(power, giver, amount, target, cardSource) ?? 1m;
    }

    public override Task AfterModifyingPowerAmountGiven(PowerModel power)
    {
        return CopyForGameplayHook()?.AfterModifyingPowerAmountGiven(power) ?? Task.CompletedTask;
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
        return CopyForGameplayHook()?.AfterModifyingPowerAmountReceived(power) ?? Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        return CopyForGameplayHook()?.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource) ?? Task.CompletedTask;
    }

    private RelicModel? FindSource() => Owner?.Relics
            .Where(relic => relic is not SuspiciousHatRelic
                && relic.Rarity != RelicRarity.Ancient
                && IsVanillaRelic(relic))
            .LastOrDefault();

    private RelicModel? CopiedRelic() => _copyState.Copy is { IsMelted: false } copy ? copy : null;

    private RelicModel? DisplaySource() =>
        (_copyState.Copy ?? FindSource()) is { IsMelted: false } source ? source : null;

    private RelicModel? CopyForGameplayHook()
    {
        RefreshCopiedRelic();
        return CopiedRelic();
    }

    internal static void RefreshForRun(IRunState runState)
    {
        foreach (Player player in runState.Players)
        {
            RefreshForPlayer(player);
        }
    }

    internal static void RefreshForPlayer(Player player)
    {
        foreach (SuspiciousHatRelic hat in player.Relics.OfType<SuspiciousHatRelic>().ToArray())
        {
            hat.RefreshCopiedRelic();
        }
    }

    private void RefreshCopiedRelic()
    {
        if (IsCanonical || Owner == null || SuspiciousHatAcquisitionScope.IsPending(Owner))
        {
            return;
        }

        RelicModel? source = FindSource();
        SuspiciousHatCopyIdentity? identity = source == null ? null : new(
            source.Id, source.FloorAddedToDeck, Owner.Relics.ToList().IndexOf(source), Owner.NetId);
        bool changed = _copyState.Reconcile(source, identity, CreateCopy, ReleaseCopy);
        if (source != null && _copyState.Copy is { } copy)
        {
            // Permanent training and purchase restrictions are not independent combat counters.
            if (source is Girya originalGirya && copy is Girya girya && girya.TimesLifted != originalGirya.TimesLifted)
            {
                girya.TimesLifted = originalGirya.TimesLifted;
            }

            if (source is MawBank { HasItemBeenBought: true } && copy is MawBank { HasItemBeenBought: false } bank)
            {
                bank.HasItemBeenBought = true;
            }

#if BS_BETA_1130
            if (source is Wuppy originalPet && copy is Wuppy pet && pet.MaxHp <= 0 && originalPet.MaxHp > 0)
            {
                pet.MaxHp = originalPet.MaxHp;
                pet.CurrentHp = originalPet.CurrentHp;
            }
#endif
            copy.IsMelted = source.IsMelted;
        }

        if (changed)
        {
            InvokeDisplayAmountChanged();
        }
    }

    private RelicModel CreateCopy(RelicModel source, SuspiciousHatCopyIdentity identity, string pendingSave)
    {
        CopiedRelicSave? saved = null;
        if (!string.IsNullOrEmpty(pendingSave))
        {
            try
            {
                saved = DeserializeCopy(pendingSave);
            }
            catch (JsonException exception)
            {
                Entry.Logger.Warn($"Suspicious Hat ignored an invalid saved copy: {exception.Message}");
            }
        }

        SerializableRelic serialized = BuildCopySnapshot(source.ToSerializable(), saved, identity);
        RelicModel copy = source.CanonicalInstance.ToMutable();
        copy.Owner = source.Owner;
        serialized.Props?.Fill(copy);
        copy.FloorAddedToDeck = identity.Floor;
        copy.DisplayAmountChanged += InvokeDisplayAmountChanged;
        return copy;
    }

    private void ReleaseCopy(RelicModel copy) => copy.DisplayAmountChanged -= InvokeDisplayAmountChanged;

    private static string SerializeCopy(RelicModel copy, SuspiciousHatCopyIdentity identity) =>
        JsonSerializer.Serialize(new CopiedRelicSave
        {
            TargetIndex = identity.Index,
            TargetFloor = identity.Floor,
            TargetOwnerNetId = identity.OwnerNetId,
            Relic = copy.ToSerializable()
        }, CopySaveOptions);

    internal static SerializableRelic BuildCopySnapshot(
        SerializableRelic source,
        CopiedRelicSave? saved,
        SuspiciousHatCopyIdentity identity)
    {
        SerializableRelic snapshot = CloneSerializable(MatchesSource(saved, identity) ? saved!.Relic! : source);
        if (source.Props == null)
        {
            return snapshot;
        }

        snapshot.Props ??= CloneSerializable(source).Props;
        if (snapshot.Props == null)
        {
            return snapshot;
        }

        if (source.Id?.Entry == "GIRYA")
        {
            SetInt(snapshot.Props, "TimesLifted", GetInt(source.Props, "TimesLifted"));
        }

        if (source.Id?.Entry == "WUPPY" && GetInt(snapshot.Props, "MaxHp") <= 0)
        {
            // Repair the invalid zero-health copies written by the previous implementation.
            SetInt(snapshot.Props, "MaxHp", GetInt(source.Props, "MaxHp"));
            SetInt(snapshot.Props, "CurrentHp", GetInt(source.Props, "CurrentHp"));
        }

        if (source.Id?.Entry == "MAW_BANK" && source.Props.bools?.Any(p => p.name == "HasItemBeenBought" && p.value) == true)
        {
            snapshot.Props.bools ??= [];
            snapshot.Props.bools.RemoveAll(p => p.name == "HasItemBeenBought");
            snapshot.Props.bools.Add(new("HasItemBeenBought", true));
        }

        return snapshot;
    }

    private static int GetInt(SavedProperties props, string name) =>
        props.ints?.FirstOrDefault(p => p.name == name).value ?? 0;

    private static void SetInt(SavedProperties props, string name, int value)
    {
        props.ints ??= [];
        props.ints.RemoveAll(p => p.name == name);
        props.ints.Add(new(name, value));
    }

    internal static SerializableRelic CloneSerializable(SerializableRelic source) =>
        JsonSerializer.Deserialize<SerializableRelic>(JsonSerializer.Serialize(source, CopySaveOptions), CopySaveOptions)!;

    internal static CopiedRelicSave? DeserializeCopy(string json) =>
        JsonSerializer.Deserialize<CopiedRelicSave>(json, CopySaveOptions);

    internal static bool MatchesSource(CopiedRelicSave? saved, ModelId id, int floor, int index) =>
        MatchesSource(saved, new(id, floor, index));

    internal static bool MatchesSource(CopiedRelicSave? saved, SuspiciousHatCopyIdentity identity) =>
        saved?.Relic?.Id == identity.Id && saved.TargetFloor == identity.Floor && saved.TargetIndex == identity.Index
        && (saved.TargetOwnerNetId == 0 || identity.OwnerNetId == 0 || saved.TargetOwnerNetId == identity.OwnerNetId);

    internal sealed class CopiedRelicSave
    {
        public int TargetIndex { get; set; }
        public int TargetFloor { get; set; }
        public ulong TargetOwnerNetId { get; set; }
        public SerializableRelic? Relic { get; set; }
    }

    private static bool IsVanillaRelic(RelicModel relic)
    {
        return relic.GetType().Namespace == "MegaCrit.Sts2.Core.Models.Relics";
    }
}
#pragma warning restore CS8765, CS8604
