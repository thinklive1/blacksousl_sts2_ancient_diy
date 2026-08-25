using System.Text.Json;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Stores and settles the risky combat nodes offered by Lorina's fourth option.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class RoyalChipRelic : ModRelicTemplate
{
    private const int MarkedNodeCount = 12;
    private const string RelicIconPath =
        "res://bs_ancient/assets/images/relics/RoyalChipRelic.png";
    private static readonly RoyalChipConditionKind[] ActiveConditions = [
        RoyalChipConditionKind.BeforeTurnFive,
        RoyalChipConditionKind.HealthLossAtMostTen,
        RoyalChipConditionKind.NoPotions,
        RoyalChipConditionKind.CardsAtMostTwenty,
        RoyalChipConditionKind.UniqueCardNames,
        RoyalChipConditionKind.OverkillKill,
        RoyalChipConditionKind.AttackCardsAtMostEight,
        RoyalChipConditionKind.SkillCardsAtMostTwelve,
        RoyalChipConditionKind.PowerCardsAtMostThree
    ];
    private static readonly RoyalChipRewardKind[] AdditionalRewards = [
        RoyalChipRewardKind.PotionSlot,
        RoyalChipRewardKind.CardRemoval,
        RoyalChipRewardKind.CardCopy,
        RoyalChipRewardKind.SuitEnchant
    ];
    private const int SuitEnchantCardCount = 3;

    private string _gamblesSerialized = string.Empty;
    private List<RoyalChipGambleData>? _gambles;
    private string _activeGambleKey = string.Empty;
    private int _activeGoldStake;
    private string _activeCardEntry = string.Empty;
    private int _activeCardUpgradeLevel;
    private int _activeCardDeckIndex = -1;
    private int _cardsPlayed;
    private int _attackCardsPlayed;
    private int _skillCardsPlayed;
    private int _powerCardsPlayed;
    private int _unblockedDamage;
    private bool _usedPotion;
    private bool _overkillKillTriggered;
    private string _cardNamePlayCountsSerialized = string.Empty;
    private Dictionary<string, int>? _cardNamePlayCounts;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath);

    public override bool IsAllowed(IRunState runState) => false;

    public override Task AfterObtained()
    {
        Configure(Owner.RunState);
        return Task.CompletedTask;
    }

    [SavedProperty]
    public string BlackSouls_Gambles
    {
        get => _gamblesSerialized;
        set
        {
            AssertMutable();
            _gamblesSerialized = value ?? string.Empty;
            _gambles = null;
        }
    }

    [SavedProperty]
    public string BlackSouls_ActiveGambleKey
    {
        get => _activeGambleKey;
        set
        {
            AssertMutable();
            _activeGambleKey = value ?? string.Empty;
        }
    }

    [SavedProperty]
    public int BlackSouls_ActiveGoldStake
    {
        get => _activeGoldStake;
        set
        {
            AssertMutable();
            _activeGoldStake = Math.Max(0, value);
        }
    }

    [SavedProperty]
    public string BlackSouls_ActiveCardEntry
    {
        get => _activeCardEntry;
        set
        {
            AssertMutable();
            _activeCardEntry = value ?? string.Empty;
        }
    }

    [SavedProperty]
    public int BlackSouls_ActiveCardUpgradeLevel
    {
        get => _activeCardUpgradeLevel;
        set
        {
            AssertMutable();
            _activeCardUpgradeLevel = Math.Max(0, value);
        }
    }

    [SavedProperty]
    public int BlackSouls_ActiveCardDeckIndex
    {
        get => _activeCardDeckIndex;
        set
        {
            AssertMutable();
            _activeCardDeckIndex = value;
        }
    }

    [SavedProperty]
    public int BlackSouls_CardsPlayed
    {
        get => _cardsPlayed;
        set
        {
            AssertMutable();
            _cardsPlayed = Math.Max(0, value);
        }
    }

    [SavedProperty]
    public int BlackSouls_AttackCardsPlayed
    {
        get => _attackCardsPlayed;
        set
        {
            AssertMutable();
            _attackCardsPlayed = Math.Max(0, value);
        }
    }

    [SavedProperty]
    public int BlackSouls_SkillCardsPlayed
    {
        get => _skillCardsPlayed;
        set
        {
            AssertMutable();
            _skillCardsPlayed = Math.Max(0, value);
        }
    }

    [SavedProperty]
    public int BlackSouls_PowerCardsPlayed
    {
        get => _powerCardsPlayed;
        set
        {
            AssertMutable();
            _powerCardsPlayed = Math.Max(0, value);
        }
    }

    [SavedProperty]
    public int BlackSouls_UnblockedDamage
    {
        get => _unblockedDamage;
        set
        {
            AssertMutable();
            _unblockedDamage = Math.Max(0, value);
        }
    }

    [SavedProperty]
    public bool BlackSouls_UsedPotion
    {
        get => _usedPotion;
        set
        {
            AssertMutable();
            _usedPotion = value;
        }
    }

    [SavedProperty]
    public bool BlackSouls_OverkillKillTriggered
    {
        get => _overkillKillTriggered;
        set
        {
            AssertMutable();
            _overkillKillTriggered = value;
        }
    }

    [SavedProperty]
    public string BlackSouls_CardNamePlayCounts
    {
        get => _cardNamePlayCountsSerialized;
        set
        {
            AssertMutable();
            _cardNamePlayCountsSerialized = value ?? string.Empty;
            _cardNamePlayCounts = null;
        }
    }

    public bool TryGetPendingGamble(MapCoord coord, out RoyalChipGambleData gamble)
    {
        gamble = Gambles.FirstOrDefault(candidate =>
            candidate.Status == RoyalChipGambleStatus.Pending
            && candidate.ActIndex == Owner.RunState.CurrentActIndex
            && candidate.Col == coord.col
            && candidate.Row == coord.row)!;
        return gamble != null;
    }

    public static string FormatWager(RoyalChipGambleData gamble) =>
        new LocString("relics", $"BS_ANCIENT_RELIC_ROYAL_CHIP_WAGER.{gamble.Wager}").GetFormattedText();

    public static string FormatPenalty(RoyalChipGambleData gamble) =>
        new LocString("relics", $"BS_ANCIENT_RELIC_ROYAL_CHIP_PENALTY.{gamble.Wager}").GetFormattedText();

    public static string FormatCondition(RoyalChipGambleData gamble) =>
        new LocString("relics", $"BS_ANCIENT_RELIC_ROYAL_CHIP_CONDITION.{gamble.Condition}").GetFormattedText();

    public static string FormatReward(RoyalChipGambleData gamble) =>
        new LocString("relics", $"BS_ANCIENT_RELIC_ROYAL_CHIP_REWARD.{gamble.Reward}").GetFormattedText();

    public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
    {
        if (actIndex == runState.CurrentActIndex)
        {
            AddMarkedRooms(map);
        }

        return map;
    }

    public override async Task BeforeCombatStart()
    {
        // Combat-start hooks can be replayed while a saved combat is restored.
        if (BlackSouls_ActiveGambleKey.Length > 0)
        {
            if (GetActiveGamble() is { } restoredGamble
                && !Owner.Creature.Powers.OfType<RoyalChipConditionPowerBase>().Any())
            {
                await ApplyConditionPower(restoredGamble);
            }
            else
            {
                RoyalChipConditionPowerBase.Refresh(Owner);
            }

            return;
        }

        RoyalChipGambleData? gamble = GetCurrentPendingGamble();
        if (gamble == null)
        {
            return;
        }

        EnsureWagerIsAvailable(gamble);
        BlackSouls_ActiveGambleKey = gamble.Key;
        BlackSouls_ActiveGoldStake = 0;
        BlackSouls_ActiveCardEntry = string.Empty;
        BlackSouls_ActiveCardUpgradeLevel = 0;
        BlackSouls_ActiveCardDeckIndex = -1;
        BlackSouls_CardsPlayed = 0;
        BlackSouls_AttackCardsPlayed = 0;
        BlackSouls_SkillCardsPlayed = 0;
        BlackSouls_PowerCardsPlayed = 0;
        BlackSouls_UnblockedDamage = 0;
        BlackSouls_UsedPotion = false;
        BlackSouls_OverkillKillTriggered = false;
        BlackSouls_CardNamePlayCounts = string.Empty;

        switch (gamble.Wager)
        {
            case RoyalChipWagerKind.Gold:
                BlackSouls_ActiveGoldStake = Owner.Gold / 2;
                if (BlackSouls_ActiveGoldStake > 0)
                {
                    await PlayerCmd.LoseGold(BlackSouls_ActiveGoldStake, Owner);
                }

                break;
            case RoyalChipWagerKind.Card:
                PrepareCardWager();
                break;
            case RoyalChipWagerKind.Relic:
                PrepareRelicWager(gamble);
                SaveGambles();
                break;
        }

        await ApplyConditionPower(gamble);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsActiveFor(cardPlay.Card.Owner))
        {
            BlackSouls_CardsPlayed++;
            RecordPlayedCard(cardPlay.Card);
            RoyalChipConditionPowerBase.Refresh(Owner);
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
        if (BlackSouls_ActiveGambleKey.Length == 0
            || target.Side != CombatSide.Enemy
            || !IsOwnerDamageDealer(dealer)
            || !result.WasTargetKilled
            || result.OverkillDamage < 10)
        {
            return Task.CompletedTask;
        }

        BlackSouls_OverkillKillTriggered = true;
        RoyalChipConditionPowerBase.Refresh(Owner);
        return Task.CompletedTask;
    }

    public override Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner && IsActiveFor(Owner))
        {
            BlackSouls_UsedPotion = true;
            RoyalChipConditionPowerBase.Refresh(Owner);
        }

        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (IsActiveFor(target.Player) && result.UnblockedDamage > 0)
        {
            BlackSouls_UnblockedDamage += (int)Math.Ceiling((decimal)result.UnblockedDamage);
            RoyalChipConditionPowerBase.Refresh(Owner);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (GetActiveGamble() is { } gamble)
        {
            await ResolveGamble(gamble, IsConditionMet(gamble));
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        // A victory also emits AfterCombatEnd before/around AfterCombatVictory.
        // Do not settle it as a failure before the victory hook can evaluate the condition.
        if (GetActiveGamble() is { } gamble
            && room.CombatState?.Enemies.All(enemy => !enemy.IsAlive) != true)
        {
            await ResolveGamble(gamble, success: false);
        }
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner && BlackSouls_ActiveGambleKey.Length > 0)
        {
            RoyalChipConditionPowerBase.Refresh(player);
        }

        return Task.CompletedTask;
    }

    private void Configure(IRunState runState)
    {
        if (Gambles.Count > 0)
        {
            AddMarkedRooms(runState.Map);
            return;
        }

        MapPoint? currentPoint = runState.CurrentMapPoint;
        int minRow = currentPoint?.coord.row + 1 ?? 1;
        Rng rng = new((uint)((int)runState.Rng.Seed
            + StringHelper.GetDeterministicHashCode(nameof(RoyalChipRelic))
            + runState.CurrentActIndex));

        IEnumerable<MapPoint> futurePoints = currentPoint == null
            ? runState.Map.GetAllMapPoints().Where(point => point.coord.row >= minRow)
            : GetReachableFuturePoints(currentPoint);
        List<MapPoint> candidates = futurePoints
            .Where(point => point.PointType is MapPointType.Monster or MapPointType.Elite)
            .ToList();

        foreach (MapPoint point in candidates.UnstableShuffle(rng).Take(MarkedNodeCount))
        {
            List<RoyalChipWagerKind> availableWagers = GetAvailableWagers(Owner);
            if (availableWagers.Count == 0)
            {
                break;
            }

            RoyalChipWagerKind wager = availableWagers[rng.NextInt(availableWagers.Count)];
            RoyalChipGambleData gamble = new()
            {
                ActIndex = runState.CurrentActIndex,
                Col = point.coord.col,
                Row = point.coord.row,
                Wager = wager,
                Reward = GetRewardForWager(wager, rng, Owner),
                Condition = ActiveConditions[rng.NextInt(ActiveConditions.Length)],
                Status = RoyalChipGambleStatus.Pending
            };
            Gambles.Add(gamble);
        }

        SaveGambles();
        AddMarkedRooms(runState.Map);
    }

    private List<RoyalChipWagerKind> GetAvailableWagers(Player player)
    {
        int upgradedRemovableCards = player.Deck.Cards.Count(card => card.IsUpgraded && card.IsRemovable);
        int nonAncientRelics = player.Relics.Count(relic =>
            relic.Rarity != RelicRarity.Ancient && relic is not RoyalChipRelic);
        return Enum.GetValues<RoyalChipWagerKind>()
            .Where(wager => IsWagerAvailable(wager, player.Gold, upgradedRemovableCards, nonAncientRelics))
            .ToList();
    }

    internal static bool IsWagerAvailable(
        RoyalChipWagerKind wager,
        int gold,
        int upgradedRemovableCards,
        int nonAncientRelics)
    {
        return wager switch
        {
            RoyalChipWagerKind.Gold => gold >= 2,
            RoyalChipWagerKind.MaxHp => true,
            RoyalChipWagerKind.Card => upgradedRemovableCards > 0,
            RoyalChipWagerKind.Relic => nonAncientRelics > 0,
            _ => false
        };
    }

    private void EnsureWagerIsAvailable(RoyalChipGambleData gamble)
    {
        List<RoyalChipWagerKind> available = GetAvailableWagers(Owner);
        if (available.Contains(gamble.Wager))
        {
            return;
        }

        gamble.Wager = available[Owner.RunState.Rng.Niche.NextInt(available.Count)];
        gamble.RelicEntry = string.Empty;
        SaveGambles();
    }

    private static RoyalChipRewardKind GetRewardForWager(
        RoyalChipWagerKind wager,
        Rng rng,
        Player player)
    {
        List<RoyalChipRewardKind> rewards = [GetDefaultReward(wager), .. AdditionalRewards];
        if (!player.Deck.Cards.Any(card => card.IsRemovable))
        {
            rewards.Remove(RoyalChipRewardKind.CardRemoval);
        }

        return rewards[rng.NextInt(rewards.Count)];
    }

    private static RoyalChipRewardKind GetDefaultReward(RoyalChipWagerKind wager) =>
        wager switch
        {
            RoyalChipWagerKind.Gold => RoyalChipRewardKind.Gold,
            RoyalChipWagerKind.MaxHp => RoyalChipRewardKind.MaxHp,
            RoyalChipWagerKind.Card => RoyalChipRewardKind.Card,
            RoyalChipWagerKind.Relic => RoyalChipRewardKind.Relic,
            _ => RoyalChipRewardKind.Gold
        };

    private void PrepareCardWager()
    {
        List<(CardModel Card, int Index)> candidates = Owner.Deck.Cards
            .Select((card, index) => (Card: card, Index: index))
            .Where(candidate => candidate.Card.IsUpgraded && candidate.Card.IsRemovable)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        (CardModel card, int index) = candidates[
            Owner.RunState.Rng.Niche.NextInt(candidates.Count)];
        BlackSouls_ActiveCardEntry = card.Id.Entry;
        BlackSouls_ActiveCardUpgradeLevel = card.CurrentUpgradeLevel;
        BlackSouls_ActiveCardDeckIndex = index;
    }

    private void PrepareRelicWager(RoyalChipGambleData gamble)
    {
        List<RelicModel> candidates = Owner.Relics
            .Where(relic => relic.Rarity != RelicRarity.Ancient && relic is not RoyalChipRelic)
            .ToList();
        gamble.RelicEntry = candidates.Count == 0
            ? string.Empty
            : candidates[Owner.RunState.Rng.Niche.NextInt(candidates.Count)].Id.Entry;
    }

    private bool IsConditionMet(RoyalChipGambleData gamble)
    {
        return IsConditionMet(
            gamble.Condition,
            Owner.PlayerCombatState?.TurnNumber ?? int.MaxValue,
            BlackSouls_UnblockedDamage,
            BlackSouls_UsedPotion,
            BlackSouls_CardsPlayed,
            MaxCardNamePlayCount,
            BlackSouls_OverkillKillTriggered,
            BlackSouls_AttackCardsPlayed,
            BlackSouls_SkillCardsPlayed,
            BlackSouls_PowerCardsPlayed);
    }

    internal int GetActiveConditionProgress()
    {
        RoyalChipGambleData? gamble = GetActiveGamble();
        if (gamble == null)
        {
            return 0;
        }

        return gamble.Condition switch
        {
            RoyalChipConditionKind.BeforeTurnFive => Owner.PlayerCombatState?.TurnNumber ?? 0,
            RoyalChipConditionKind.HealthLossAtMostTen => BlackSouls_UnblockedDamage,
            RoyalChipConditionKind.NoPotions => BlackSouls_UsedPotion ? 1 : 0,
            RoyalChipConditionKind.CardsAtMostTwenty => BlackSouls_CardsPlayed,
            RoyalChipConditionKind.UniqueCardNames => MaxCardNamePlayCount,
            RoyalChipConditionKind.OverkillKill => BlackSouls_OverkillKillTriggered ? 1 : 0,
            RoyalChipConditionKind.AttackCardsAtMostEight => BlackSouls_AttackCardsPlayed,
            RoyalChipConditionKind.SkillCardsAtMostTwelve => BlackSouls_SkillCardsPlayed,
            RoyalChipConditionKind.PowerCardsAtMostThree => BlackSouls_PowerCardsPlayed,
            _ => 0
        };
    }

    private async Task ApplyConditionPower(RoyalChipGambleData gamble)
    {
        ThrowingPlayerChoiceContext choiceContext = new();
        switch (gamble.Condition)
        {
            case RoyalChipConditionKind.BeforeTurnFive:
                await PowerCmd.Apply<RoyalChipBeforeTurnFivePower>(
                    choiceContext, Owner.Creature, 1, Owner.Creature, null, true);
                break;
            case RoyalChipConditionKind.HealthLossAtMostTen:
                await PowerCmd.Apply<RoyalChipHealthLossPower>(
                    choiceContext, Owner.Creature, 1, Owner.Creature, null, true);
                break;
            case RoyalChipConditionKind.NoPotions:
                await PowerCmd.Apply<RoyalChipNoPotionPower>(
                    choiceContext, Owner.Creature, 1, Owner.Creature, null, true);
                break;
            case RoyalChipConditionKind.CardsAtMostTwenty:
                await PowerCmd.Apply<RoyalChipCardLimitPower>(
                    choiceContext, Owner.Creature, 1, Owner.Creature, null, true);
                break;
            case RoyalChipConditionKind.UniqueCardNames:
                await PowerCmd.Apply<RoyalChipUniqueCardNamesPower>(
                    choiceContext, Owner.Creature, 1, Owner.Creature, null, true);
                break;
            case RoyalChipConditionKind.OverkillKill:
                await PowerCmd.Apply<RoyalChipOverkillKillPower>(
                    choiceContext, Owner.Creature, 1, Owner.Creature, null, true);
                break;
            case RoyalChipConditionKind.AttackCardsAtMostEight:
                await PowerCmd.Apply<RoyalChipAttackLimitPower>(
                    choiceContext, Owner.Creature, 1, Owner.Creature, null, true);
                break;
            case RoyalChipConditionKind.SkillCardsAtMostTwelve:
                await PowerCmd.Apply<RoyalChipSkillLimitPower>(
                    choiceContext, Owner.Creature, 1, Owner.Creature, null, true);
                break;
            case RoyalChipConditionKind.PowerCardsAtMostThree:
                await PowerCmd.Apply<RoyalChipAbilityLimitPower>(
                    choiceContext, Owner.Creature, 1, Owner.Creature, null, true);
                break;
        }

        RoyalChipConditionPowerBase.Refresh(Owner);
    }

    internal static bool IsConditionMet(
        RoyalChipConditionKind condition,
        int turnNumber,
        int unblockedDamage,
        bool usedPotion,
        int cardsPlayed,
        int maxCardNamePlayCount = 0,
        bool overkillKillTriggered = false,
        int attackCardsPlayed = 0,
        int skillCardsPlayed = 0,
        int powerCardsPlayed = 0)
    {
        return condition switch
        {
            RoyalChipConditionKind.BeforeTurnFive => turnNumber <= 4,
            RoyalChipConditionKind.HealthLossAtMostTen => unblockedDamage <= 10,
            RoyalChipConditionKind.NoPotions => !usedPotion,
            RoyalChipConditionKind.CardsAtMostTwenty => cardsPlayed <= 20,
            RoyalChipConditionKind.UniqueCardNames => maxCardNamePlayCount < 2,
            RoyalChipConditionKind.OverkillKill => overkillKillTriggered,
            RoyalChipConditionKind.AttackCardsAtMostEight => attackCardsPlayed <= 8,
            RoyalChipConditionKind.SkillCardsAtMostTwelve => skillCardsPlayed <= 12,
            RoyalChipConditionKind.PowerCardsAtMostThree => powerCardsPlayed <= 3,
            _ => false
        };
    }

    private async Task ResolveGamble(RoyalChipGambleData gamble, bool success)
    {
        if (gamble.Status != RoyalChipGambleStatus.Pending)
        {
            ClearActiveState();
            RemoveMarkedRoom(gamble);
            return;
        }

        if (success)
        {
            EnsureRewardIsAvailable(gamble);
        }

        gamble.Status = success ? RoyalChipGambleStatus.Success : RoyalChipGambleStatus.Failure;
        SaveGambles();
        try
        {
            if (success)
            {
                await ApplyReward(gamble);
            }
            else
            {
                await ApplyPenalty(gamble);
            }
        }
        finally
        {
            ClearActiveState();
            RemoveMarkedRoom(gamble);
        }
    }

    private void EnsureRewardIsAvailable(RoyalChipGambleData gamble)
    {
        if (CanApplyReward(gamble.Reward))
        {
            return;
        }

        List<RoyalChipRewardKind> available = Enum.GetValues<RoyalChipRewardKind>()
            .Where(CanApplyReward)
            .ToList();
        gamble.Reward = available[Owner.RunState.Rng.Niche.NextInt(available.Count)];
    }

    private bool CanApplyReward(RoyalChipRewardKind reward)
    {
        return IsRewardAvailable(
            reward,
            BlackSouls_ActiveGoldStake,
            Owner.Deck.Cards.Count(card => card.IsUpgradable),
            Owner.Deck.Cards.Count(card => card.IsRemovable),
            Owner.Deck.Cards.Count,
            Owner.Deck.Cards.Count(PlayingCardSuitEnchantment.CanReceiveOrRerollSuit));
    }

    internal static bool IsRewardAvailable(
        RoyalChipRewardKind reward,
        int goldStake,
        int upgradableCards,
        int removableCards,
        int deckSize,
        int suitCandidates = 0)
    {
        return reward switch
        {
            RoyalChipRewardKind.Gold => goldStake > 0,
            RoyalChipRewardKind.Card => upgradableCards >= 2,
            RoyalChipRewardKind.CardRemoval => removableCards > 0,
            RoyalChipRewardKind.CardCopy => deckSize > 0,
            RoyalChipRewardKind.SuitEnchant => suitCandidates >= SuitEnchantCardCount,
            _ => true
        };
    }

    private async Task ApplyReward(RoyalChipGambleData gamble)
    {
        switch (gamble.Reward)
        {
            case RoyalChipRewardKind.Gold:
                if (BlackSouls_ActiveGoldStake > 0)
                {
                    await PlayerCmd.GainGold(BlackSouls_ActiveGoldStake * 2, Owner);
                }

                break;
            case RoyalChipRewardKind.MaxHp:
                await CreatureCmd.GainMaxHp(Owner.Creature, 6);
                break;
            case RoyalChipRewardKind.Card:
                await UpgradeCards(2);
                break;
            case RoyalChipRewardKind.Relic:
                await ObtainCommonRelics(2);
                break;
            case RoyalChipRewardKind.PotionSlot:
                Owner.AddToMaxPotionCount(1);
                Owner.GetRelic<RoyalChipRelic>()?.Flash();
                break;
            case RoyalChipRewardKind.CardRemoval:
                await RemoveOneCard();
                break;
            case RoyalChipRewardKind.CardCopy:
                await CopyOneCard();
                break;
            case RoyalChipRewardKind.SuitEnchant:
                await EnchantRandomSuits();
                break;
        }
    }

    private async Task EnchantRandomSuits()
    {
        List<CardModel> candidates = Owner.Deck.Cards
            .Where(PlayingCardSuitEnchantment.CanReceiveOrRerollSuit)
            .ToList();
        if (candidates.Count < SuitEnchantCardCount)
        {
            return;
        }

        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(
            new BlockingPlayerChoiceContext(),
            candidates,
            Owner,
            new CardSelectorPrefs(
                CardSelectorPrefs.EnchantSelectionPrompt,
                SuitEnchantCardCount,
                SuitEnchantCardCount)
            {
                RequireManualConfirmation = true
            });

        foreach (CardModel card in selected)
        {
            if (!PlayingCardSuitEnchantment.TryEnchantRandomSuit(card, 1, 13))
            {
                continue;
            }

            NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
            if (vfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
            }
        }
    }

    private async Task ApplyPenalty(RoyalChipGambleData gamble)
    {
        switch (gamble.Wager)
        {
            case RoyalChipWagerKind.MaxHp:
                await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, 3, false);
                break;
            case RoyalChipWagerKind.Card:
                CardModel? card = FindStakedCard();
                if (card != null)
                {
                    await CardPileCmd.RemoveFromDeck(card);
                }

                break;
            case RoyalChipWagerKind.Relic:
                RelicModel? relic = Owner.Relics.FirstOrDefault(candidate =>
                    candidate.Id.Entry == gamble.RelicEntry);
                if (relic != null)
                {
                    await RelicCmd.Remove(relic);
                }

                break;
        }
    }

    private async Task UpgradeCards(int count)
    {
        for (int index = 0; index < count; index++)
        {
            IEnumerable<CardModel> selected = await CardSelectCmd.FromDeckForUpgrade(
                Owner,
                new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1));
            CardModel? card = selected.FirstOrDefault();
            if (card == null)
            {
                break;
            }

            CardCmd.Upgrade(card);
        }
    }

    private async Task RemoveOneCard()
    {
        CardModel? card = (await CardSelectCmd.FromDeckForRemoval(
                Owner,
                new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1)))
            .FirstOrDefault();
        if (card != null)
        {
            await CardPileCmd.RemoveFromDeck(card);
        }
    }

    private async Task CopyOneCard()
    {
        CardModel? card = (await CardSelectCmd.FromDeckGeneric(
                Owner,
                new CardSelectorPrefs(
                    new LocString("relics", "BS_ANCIENT_RELIC_ROYAL_CHIP_COPY_CARD_PROMPT"),
                    1)))
            .FirstOrDefault();
        if (card == null)
        {
            return;
        }

        CardModel copy = Owner.RunState.CloneCard(card);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.Add(copy, PileType.Deck, CardPilePosition.Bottom, this, false),
            2f);
    }

    private async Task ObtainCommonRelics(int count)
    {
        for (int index = 0; index < count; index++)
        {
            RelicModel relic = RelicFactory.PullNextRelicFromFront(
                Owner,
                RelicRarity.Common,
                candidate => candidate.IsAllowed(Owner.RunState));
            await RelicCmd.Obtain(relic.ToMutable(), Owner);
        }
    }

    private RoyalChipGambleData? GetCurrentPendingGamble()
    {
        MapPoint? point = Owner.RunState.CurrentMapPoint;
        return point == null
            ? null
            : Gambles.FirstOrDefault(gamble =>
                gamble.Status == RoyalChipGambleStatus.Pending
                && gamble.ActIndex == Owner.RunState.CurrentActIndex
                && gamble.Col == point.coord.col
                && gamble.Row == point.coord.row);
    }

    private RoyalChipGambleData? GetActiveGamble()
    {
        return Gambles.FirstOrDefault(gamble => gamble.Key == BlackSouls_ActiveGambleKey);
    }

    private bool IsActiveFor(Player? player)
    {
        return player == Owner && BlackSouls_ActiveGambleKey.Length > 0;
    }

    private void RecordPlayedCard(CardModel card)
    {
        switch (card.Type)
        {
            case CardType.Attack:
                BlackSouls_AttackCardsPlayed++;
                break;
            case CardType.Skill:
                BlackSouls_SkillCardsPlayed++;
                break;
            case CardType.Power:
                BlackSouls_PowerCardsPlayed++;
                break;
        }

        Dictionary<string, int> counts = CardNamePlayCounts;
        counts[card.Id.Entry] = counts.GetValueOrDefault(card.Id.Entry) + 1;
        BlackSouls_CardNamePlayCounts = JsonSerializer.Serialize(counts);
    }

    private bool IsOwnerDamageDealer(Creature? dealer)
    {
        return dealer == Owner.Creature
            || dealer == Owner.Osty
            || dealer?.Player == Owner
            || dealer?.PetOwner == Owner;
    }

    private CardModel? FindStakedCard()
    {
        if (string.IsNullOrEmpty(BlackSouls_ActiveCardEntry))
        {
            return null;
        }

        if (BlackSouls_ActiveCardDeckIndex >= 0
            && BlackSouls_ActiveCardDeckIndex < Owner.Deck.Cards.Count)
        {
            CardModel indexedCard = Owner.Deck.Cards[BlackSouls_ActiveCardDeckIndex];
            if (indexedCard.Id.Entry == BlackSouls_ActiveCardEntry)
            {
                return indexedCard;
            }
        }

        return Owner.Deck.Cards.FirstOrDefault(card =>
            card.Id.Entry == BlackSouls_ActiveCardEntry
            && card.CurrentUpgradeLevel >= BlackSouls_ActiveCardUpgradeLevel);
    }

    private Dictionary<string, int> CardNamePlayCounts =>
        _cardNamePlayCounts ??= DeserializeCardNamePlayCounts(BlackSouls_CardNamePlayCounts);

    private int MaxCardNamePlayCount =>
        CardNamePlayCounts.Count == 0 ? 0 : CardNamePlayCounts.Values.Max();

    private List<RoyalChipGambleData> Gambles =>
        _gambles ??= DeserializeGambles(BlackSouls_Gambles);

    private void SaveGambles()
    {
        BlackSouls_Gambles = JsonSerializer.Serialize(Gambles);
    }

    private void ClearActiveState()
    {
        BlackSouls_ActiveGambleKey = string.Empty;
        BlackSouls_ActiveGoldStake = 0;
        BlackSouls_ActiveCardEntry = string.Empty;
        BlackSouls_ActiveCardUpgradeLevel = 0;
        BlackSouls_ActiveCardDeckIndex = -1;
        BlackSouls_CardsPlayed = 0;
        BlackSouls_AttackCardsPlayed = 0;
        BlackSouls_SkillCardsPlayed = 0;
        BlackSouls_PowerCardsPlayed = 0;
        BlackSouls_UnblockedDamage = 0;
        BlackSouls_UsedPotion = false;
        BlackSouls_OverkillKillTriggered = false;
        BlackSouls_CardNamePlayCounts = string.Empty;
    }

    private static IEnumerable<MapPoint> GetReachableFuturePoints(MapPoint currentPoint)
    {
        HashSet<MapPoint> visited = [];
        Queue<MapPoint> pending = new(currentPoint.Children);
        while (pending.Count > 0)
        {
            MapPoint point = pending.Dequeue();
            if (!visited.Add(point))
            {
                continue;
            }

            foreach (MapPoint child in point.Children)
            {
                pending.Enqueue(child);
            }
        }

        return visited;
    }

    private void AddMarkedRooms(ActMap map)
    {
        foreach (RoyalChipGambleData gamble in Gambles.Where(candidate =>
                     candidate.Status == RoyalChipGambleStatus.Pending
                     && candidate.ActIndex == Owner.RunState.CurrentActIndex))
        {
            MapPoint? point = map.GetPoint(new MapCoord { col = gamble.Col, row = gamble.Row });
            if (point != null && !point.Quests.Contains(this))
            {
                point.AddQuest(this);
            }
        }
    }

    private void RemoveMarkedRoom(RoyalChipGambleData gamble)
    {
        MapPoint? point = Owner.RunState.Map.GetPoint(new MapCoord { col = gamble.Col, row = gamble.Row });
        point?.RemoveQuest(this);
    }

    private static List<RoyalChipGambleData> DeserializeGambles(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<RoyalChipGambleData>>(serialized) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static Dictionary<string, int> DeserializeCardNamePlayCounts(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(serialized) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

/// <summary>Describes one fixed Royal Chip node.</summary>
public sealed class RoyalChipGambleData
{
    public int ActIndex { get; set; }
    public int Col { get; set; }
    public int Row { get; set; }
    public RoyalChipWagerKind Wager { get; set; }
    public RoyalChipRewardKind Reward { get; set; }
    public RoyalChipConditionKind Condition { get; set; }
    public RoyalChipGambleStatus Status { get; set; }
    public string RelicEntry { get; set; } = string.Empty;

    public string Key => $"{ActIndex}:{Col}:{Row}";
}

public enum RoyalChipWagerKind
{
    Gold,
    MaxHp,
    Card,
    Relic
}

public enum RoyalChipRewardKind
{
    Gold,
    MaxHp,
    Card,
    Relic,
    PotionSlot,
    CardRemoval,
    CardCopy,
    SuitEnchant
}

public enum RoyalChipConditionKind
{
    BeforeTurnFive,
    HealthLossAtMostTen,
    NoPotions,
    CardsAtMostTwenty,
    UniqueCardNames,
    OverkillKill,
    AttackCardsAtMostEight,
    SkillCardsAtMostTwelve,
    PowerCardsAtMostThree
}

public enum RoyalChipGambleStatus
{
    Pending,
    Success,
    Failure
}
