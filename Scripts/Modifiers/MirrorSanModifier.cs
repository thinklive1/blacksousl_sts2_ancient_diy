using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

public sealed class MirrorSanModifier : ModModifierTemplate
{
    public const int InitialSan = 50;
    private const int SanLossPerAttackHit = 5;
    private const int SanGainPerCombatVictory = 20;
    private const int CombatVictorySanCap = 80;
    private const int RationalThreshold = 100;
    private const int SenThreshold = -100;
    private const decimal SenDamageTakenMultiplier = 1.5m;

    private int _san = InitialSan;
    private bool _rationalActive;
    private bool _wrigglingShadowGranted;
    private int _jackReflectionPlayCount;
    private int _senTransformCountThisCombat;

    public override ModifierAssetProfile AssetProfile => new("res://bs_ancient/assets/images/powers/SanLowPower.png");

    [SavedProperty]
    public int BlackSouls_San
    {
        get => _san;
        set
        {
            AssertMutable();
            _san = value;
        }
    }

    [SavedProperty]
    public bool BlackSouls_RationalActive
    {
        get => _rationalActive;
        set
        {
            AssertMutable();
            _rationalActive = value;
        }
    }

    public bool IsSenActive => BlackSouls_San <= SenThreshold;

    [SavedProperty]
    public int BlackSouls_JackReflectionPlayCount
    {
        get => _jackReflectionPlayCount;
        set
        {
            AssertMutable();
            _jackReflectionPlayCount = value;
        }
    }

    [SavedProperty]
    public bool BlackSouls_WrigglingShadowGranted
    {
        get => _wrigglingShadowGranted;
        set
        {
            AssertMutable();
            _wrigglingShadowGranted = value;
        }
    }

    public void Configure()
    {
        AssertMutable();
        BlackSouls_San = InitialSan;
        BlackSouls_RationalActive = false;
        BlackSouls_WrigglingShadowGranted = false;
        BlackSouls_JackReflectionPlayCount = 0;
        TryRefreshAllCounters();
    }

    public bool RegisterJackReflectionPlay()
    {
        AssertMutable();
        BlackSouls_JackReflectionPlayCount++;

        if (BlackSouls_JackReflectionPlayCount < TwoSidedVirtuePower.PlaysPerTransform)
        {
            return false;
        }

        BlackSouls_JackReflectionPlayCount = 0;
        return true;
    }

    public int GetJackReflectionPlaysRemaining()
    {
        return Math.Max(0, TwoSidedVirtuePower.PlaysPerTransform - BlackSouls_JackReflectionPlayCount);
    }

    public async Task ChangeSan(Player player, int amount)
    {
        int oldSan = BlackSouls_San;
        BlackSouls_San += amount;
        HandMirrorRelicBase.RefreshAllCounters(player.RunState);

        if (!BlackSouls_WrigglingShadowGranted && oldSan > 0 && BlackSouls_San <= 0)
        {
            BlackSouls_WrigglingShadowGranted = true;
            CardModel card = player.RunState.CreateCard<WrigglingShadowCard>(player);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck, MegaCrit.Sts2.Core.Entities.Cards.CardPilePosition.Top, this, false), 2f);
        }
    }

    public void ResetSan()
    {
        BlackSouls_San = 0;
        BlackSouls_RationalActive = false;
        TryRefreshAllCounters();
    }

    private void TryRefreshAllCounters()
    {
        try
        {
            HandMirrorRelicBase.RefreshAllCounters(RunState);
        }
        catch (InvalidOperationException)
        {
            // During save deserialization the modifier can receive saved values before it is attached to a run.
        }
    }

    public override Task BeforeCombatStart()
    {
        _senTransformCountThisCombat = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target.Player == null || result.UnblockedDamage <= 0 || !props.IsPoweredAttack())
        {
            return;
        }

        await ChangeSan(target.Player, -SanLossPerAttackHit);
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target?.Player == null || !IsSenActive)
        {
            if (dealer?.Player != null && IsSenActive)
            {
                return 1.5m;
            }

            return 1m;
        }

        return SenDamageTakenMultiplier;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        foreach (Player player in RunState.Players.Where(player => player.Creature.Side == side))
        {
            await CheckSanState(player);
        }
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (!IsSenActive || card.Owner == null)
        {
            return;
        }

        CardModel? transformed = CreateSenTransformCard(card.Owner);
        if (transformed == null)
        {
            return;
        }

        CardCmd.Upgrade(transformed);
        await CardCmd.Transform(card, transformed);
        _senTransformCountThisCombat++;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _senTransformCountThisCombat = 0;
        return Task.CompletedTask;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        if (BlackSouls_San < CombatVictorySanCap)
        {
            BlackSouls_San = Math.Min(CombatVictorySanCap, BlackSouls_San + SanGainPerCombatVictory);
            HandMirrorRelicBase.RefreshAllCounters(RunState);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (!BlackSouls_RationalActive
            || !IsStatusCard(card)
            || card.Pile is not { IsCombatPile: true }
            || card.Pile.Type == PileType.Exhaust)
        {
            return;
        }

        await ExhaustStatusCard(card);
    }

    internal static bool IsStatusCard(CardModel card)
    {
        return card.Type == CardType.Status
            || card.Rarity == CardRarity.Status;
    }

    private async Task CheckSanState(Player player)
    {
        if (BlackSouls_San >= RationalThreshold)
        {
            if (!BlackSouls_RationalActive)
            {
                BlackSouls_RationalActive = true;
                await ApplyRationalEntry(player);
            }

            await ExhaustStatusCardsInCombat(player);
            await SyncSanVisualPowers(player);
            return;
        }

        if (BlackSouls_RationalActive)
        {
            BlackSouls_RationalActive = false;
            await ApplyRationalExit(player);
        }

        await SyncSanVisualPowers(player);
    }

    private static async Task ExhaustStatusCardsInCombat(Player player)
    {
        if (player.PlayerCombatState == null)
        {
            return;
        }

        CardModel[] statusCards = player.PlayerCombatState.AllCards
            .Where(card => IsStatusCard(card)
                && card.Pile is { IsCombatPile: true }
                && card.Pile.Type != PileType.Exhaust)
            .ToArray();
        if (statusCards.Length == 0)
        {
            return;
        }

        foreach (CardModel statusCard in statusCards)
        {
            await ExhaustStatusCard(statusCard);
        }
    }

    private static Task ExhaustStatusCard(CardModel card)
    {
        return CardPileCmd.Add(card, PileType.Exhaust, CardPilePosition.Bottom);
    }

    private async Task SyncSanVisualPowers(Player player)
    {
        await SyncPower<SanHighPower>(player, BlackSouls_San >= RationalThreshold);
        await SyncPower<SanLowPower>(player, BlackSouls_San <= SenThreshold);
    }

    private static async Task SyncPower<TPower>(Player player, bool shouldHave) where TPower : PowerModel
    {
        TPower? existing = player.Creature.Powers.OfType<TPower>().FirstOrDefault();
        if (shouldHave)
        {
            if (existing == null)
            {
                await PowerCmd.Apply<TPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), player.Creature, 1, player.Creature, null, false);
            }

            return;
        }

        if (existing != null)
        {
            await PowerCmd.Remove(existing);
        }
    }

    private static async Task ApplyRationalEntry(Player player)
    {
        ICombatState? combatState = player.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        foreach (Creature enemy in combatState.Enemies.Where(enemy => enemy.IsAlive))
        {
            await PowerCmd.Apply<VulnerablePower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), enemy, 1, player.Creature, null, false);
            await PowerCmd.Apply<WeakPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), enemy, 1, player.Creature, null, false);
        }
    }

    private static async Task ApplyRationalExit(Player player)
    {
        if (player.PlayerCombatState?.Energy > 0)
        {
            await PlayerCmd.LoseEnergy(1, player);
        }

        await PowerCmd.Apply<WeakPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), player.Creature, 1, player.Creature, null, false);
    }

    private CardModel? CreateSenTransformCard(Player player)
    {
        ICombatState? combatState = player.Creature.CombatState;
        if (combatState == null)
        {
            return null;
        }

        IEnumerable<CardModel> pool = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Concat(ModelDb.CardPool<CurseCardPool>().GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            .Concat(ModelDb.CardPool<StatusCardPool>().GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            .Where(card => card.Rarity is not (CardRarity.Ancient or CardRarity.Event or CardRarity.Token)
                && card.Type is not CardType.Quest);

        CardModel[] candidates = pool.Distinct().ToArray();
        if (candidates.Length == 0)
        {
            return null;
        }

        CardModel? canonical = player.RunState.Rng.CombatCardGeneration.NextItem(candidates);
        if (canonical == null)
        {
            return null;
        }

        return combatState.CreateCard(canonical, player);
    }
}

public static class MirrorSan
{
    public static MirrorSanModifier Ensure(Player player)
    {
        MirrorSanModifier? existing = player.RunState.Modifiers.OfType<MirrorSanModifier>().FirstOrDefault();
        if (existing != null)
        {
            return existing;
        }

        MirrorSanModifier modifier = (MirrorSanModifier)ModelDb.Modifier<MirrorSanModifier>().ToMutable();
        modifier.OnRunLoaded((RunState)player.RunState);
        modifier.Configure();
        ((RunState)player.RunState).AddModifierDebug(modifier);
        return modifier;
    }

    public static MirrorSanModifier? Get(Player? player)
    {
        return player?.RunState.Modifiers.OfType<MirrorSanModifier>().FirstOrDefault();
    }

    public static int GetValue(Player? player)
    {
        return Get(player)?.BlackSouls_San ?? MirrorSanModifier.InitialSan;
    }

    public static Task Change(Player? player, int amount)
    {
        if (player == null)
        {
            return Task.CompletedTask;
        }

        return Ensure(player).ChangeSan(player, amount);
    }
}
