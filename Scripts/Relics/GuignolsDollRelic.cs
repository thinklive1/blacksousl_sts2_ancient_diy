using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Guignols Doll relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class GuignolsDollRelic : ModRelicTemplate
{
    private const int HighDamageThreshold = 50;
    private const int KillStartingHpPercent = 20;
    private const int BooDamageThreshold = 10;
    private const int AttackChainThreshold = 6;
    private const int MultiHitThreshold = 5;
    private const int CriticalHpPercent = 30;
    private const int LowEnemyHpPercent = 20;
    private const int RequiredReactions = 5;
    private const int ExecutionThresholdPercent = GuignolsExecutionPowerBase.ExecutionThresholdPercent;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/GuignolsDollRelic.png";

    private int _applause;
    private int _boos;
    private int _attackCardsPlayedThisTurn;
    private int _currentCardDamageHits;
    private int _lifeLostThisTurn;
    private CardModel? _currentDamageCard;
    private bool _dealtDamageThisTurn;
    private bool _firstTurnKillApplaudedThisCombat;
    private bool _finalBlowApplaudedThisCombat;
    private bool _perfectTurnApplaudedThisCombat;
    private bool _attackChainApplaudedThisTurn;
    private bool _multiHitApplaudedThisPlay;
    private bool _deadTurnBooedThisCombat;
    private bool _criticalHpBooedThisCombat;
    private bool _lowEnemyHpBooedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount => BlackSouls_Applause - BlackSouls_Boos;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this),
        HoverTipFactory.FromPower<GuignolsApplauseExecutionPower>(),
        HoverTipFactory.FromPower<GuignolsBooExecutionPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("HighDamage", HighDamageThreshold),
        new DynamicVar("KillStartingHpPercent", KillStartingHpPercent),
        new DynamicVar("BooDamage", BooDamageThreshold),
        new DynamicVar("RequiredReactions", RequiredReactions),
        new DynamicVar("ExecutionThreshold", ExecutionThresholdPercent)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    [SavedProperty]
    public int BlackSouls_Applause
    {
        get => _applause;
        set
        {
            AssertMutable();
            _applause = Math.Max(0, value);
            InvokeDisplayAmountChanged();
        }
    }

    [SavedProperty]
    public int BlackSouls_Boos
    {
        get => _boos;
        set
        {
            AssertMutable();
            _boos = Math.Max(0, value);
            InvokeDisplayAmountChanged();
        }
    }

    public override Task BeforeCombatStart()
    {
        ResetCombatEvaluationState();
        return EnsureExecutionPowers();
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            ResetTurnEvaluationState();
            RefreshExecutionHandGlow();
        }

        return Task.CompletedTask;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner && cardPlay.Card.Type == CardType.Attack)
        {
            _currentDamageCard = cardPlay.Card;
            _currentCardDamageHits = 0;
            _multiHitApplaudedThisPlay = false;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }

        _attackCardsPlayedThisTurn++;
        if (!_attackChainApplaudedThisTurn && _attackCardsPlayedThisTurn >= AttackChainThreshold)
        {
            _attackChainApplaudedThisTurn = true;
            await AddApplause();
        }

        if (IsCurrentDamageCard(cardPlay.Card))
        {
            _currentDamageCard = null;
            _currentCardDamageHits = 0;
            _multiHitApplaudedThisPlay = false;
        }
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Creature.Side || Owner.Creature.IsDead)
        {
            return;
        }

        if (!_perfectTurnApplaudedThisCombat && _dealtDamageThisTurn && _lifeLostThisTurn <= 0)
        {
            _perfectTurnApplaudedThisCombat = true;
            await AddApplause();
        }

        if (!_deadTurnBooedThisCombat && !_dealtDamageThisTurn)
        {
            _deadTurnBooedThisCombat = true;
            await AddBoo();
        }

        if (!_lowEnemyHpBooedThisCombat && HasLowHealthEnemy())
        {
            _lowEnemyHpBooedThisCombat = true;
            await AddBoo();
        }
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (!IsEnemy(target) || !IsOwnerApplauseDamage(dealer, props, cardSource))
        {
            return;
        }

        bool impressiveDamage = result.TotalDamage >= HighDamageThreshold;
        bool impressiveKill = IsImpressiveKill(result, target);
        if (impressiveDamage || impressiveKill)
        {
            await AddApplause();
        }

        if (result.TotalDamage > 0)
        {
            await CountCurrentCardHit(cardSource);
        }

        if (result.TotalDamage > 0)
        {
            _dealtDamageThisTurn = true;
        }

        if (!_firstTurnKillApplaudedThisCombat
            && result.WasTargetKilled
            && Owner.PlayerCombatState?.TurnNumber == 1)
        {
            _firstTurnKillApplaudedThisCombat = true;
            await AddApplause();
        }

        if (!_finalBlowApplaudedThisCombat
            && result.WasTargetKilled
            && IsFinalEnemyKilled(target))
        {
            _finalBlowApplaudedThisCombat = true;
            await AddApplause();
        }

        RefreshExecutionHandGlow();
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner.Creature)
        {
            return;
        }

        if (result.UnblockedDamage > 0)
        {
            _lifeLostThisTurn += result.UnblockedDamage;
        }

        if (result.UnblockedDamage >= BooDamageThreshold)
        {
            await AddBoo();
        }

        if (!_criticalHpBooedThisCombat && IsOwnerAtCriticalHp())
        {
            _criticalHpBooedThisCombat = true;
            await AddBoo();
        }
    }

    private async Task AddApplause()
    {
        BlackSouls_Applause++;
        Flash();
        BsAncientAudio.PlayOneShot(BsAncientAudio.Claps, 0.8f);
        await SyncExecutionPowers();
        RefreshExecutionHandGlow();
    }

    private async Task AddBoo()
    {
        BlackSouls_Boos++;
        Flash();
        BsAncientAudio.PlayOneShot(BsAncientAudio.Boos, 0.8f);
        await SyncExecutionPowers();
        RefreshExecutionHandGlow();
    }

    private int ReactionScore()
    {
        return BlackSouls_Applause - BlackSouls_Boos;
    }

    private bool HasExecutionReward()
    {
        return ReactionScore() >= RequiredReactions;
    }

    private bool HasExecutionPenalty()
    {
        return ReactionScore() <= -RequiredReactions;
    }

    private bool IsOwnerApplauseDamage(Creature? dealer, ValueProp props, CardModel? cardSource)
    {
        if (cardSource != null)
        {
            return cardSource.Type == CardType.Attack
                && props.IsPoweredAttack()
                && IsOwnerDamageDealer(dealer);
        }

        if (dealer == Owner.Osty)
        {
            return props.IsPoweredAttack();
        }

        return IsOwnerOrbDamage(dealer, props);
    }

    private bool IsOwnerDamageDealer(Creature? dealer)
    {
        return dealer == Owner.Creature
            || dealer == Owner.Osty
            || dealer?.PetOwner == Owner;
    }

    private bool IsOwnerOrbDamage(Creature? dealer, ValueProp props)
    {
        return dealer == Owner.Creature
            && props.HasFlag(ValueProp.Unpowered)
            && !props.HasFlag(ValueProp.Move);
    }

    private bool IsEnemy(Creature target)
    {
        return target.Side != Owner.Creature.Side;
    }

    private void ResetCombatEvaluationState()
    {
        _firstTurnKillApplaudedThisCombat = false;
        _finalBlowApplaudedThisCombat = false;
        _perfectTurnApplaudedThisCombat = false;
        _deadTurnBooedThisCombat = false;
        _criticalHpBooedThisCombat = false;
        _lowEnemyHpBooedThisCombat = false;
        ResetTurnEvaluationState();
    }

    private void ResetTurnEvaluationState()
    {
        _attackCardsPlayedThisTurn = 0;
        _lifeLostThisTurn = 0;
        _dealtDamageThisTurn = false;
        _attackChainApplaudedThisTurn = false;
        _currentDamageCard = null;
        _currentCardDamageHits = 0;
        _multiHitApplaudedThisPlay = false;
    }

    private bool IsOwnerAtCriticalHp()
    {
        return Owner.Creature.MaxHp > 0
            && Owner.Creature.CurrentHp * 100 <= Owner.Creature.MaxHp * CriticalHpPercent;
    }

    private bool HasLowHealthEnemy()
    {
        return Owner.Creature.CombatState?.Enemies.Any(enemy =>
            enemy.IsAlive
            && enemy.MaxHp > 0
            && enemy.CurrentHp * 100 < enemy.MaxHp * LowEnemyHpPercent) == true;
    }

    private static bool IsFinalEnemyKilled(Creature target)
    {
        return target.CombatState?.Enemies.All(enemy => !enemy.IsAlive) == true;
    }

    private async Task CountCurrentCardHit(CardModel? cardSource)
    {
        if (_multiHitApplaudedThisPlay || _currentDamageCard == null || !IsCurrentDamageCard(cardSource))
        {
            return;
        }

        _currentCardDamageHits++;
        if (_currentCardDamageHits >= MultiHitThreshold)
        {
            _multiHitApplaudedThisPlay = true;
            await AddApplause();
        }
    }

    private bool IsCurrentDamageCard(CardModel? card)
    {
        return card != null
            && _currentDamageCard != null
            && (card == _currentDamageCard
                || card.DeckVersion == _currentDamageCard
                || card == _currentDamageCard.DeckVersion);
    }

    private async Task EnsureExecutionPowers()
    {
        await SyncExecutionPowers();
    }

    private void RefreshExecutionHandGlow()
    {
        foreach (NHandCardHolder holder in NPlayerHand.Instance?.ActiveHolders ?? [])
        {
            if (holder.CardNode?.Model?.Owner == Owner)
            {
                holder.UpdateCard();
            }
        }
    }

    private async Task SyncExecutionPowers()
    {
        if (HasExecutionReward())
        {
            await EnsureApplauseExecutionPower();
        }
        else if (Owner.Creature.GetPower<GuignolsApplauseExecutionPower>() is { } applausePower)
        {
            await PowerCmd.Remove(applausePower);
        }

        if (HasExecutionPenalty())
        {
            await EnsureBooExecutionPower();
        }
        else if (Owner.Creature.GetPower<GuignolsBooExecutionPower>() is { } booPower)
        {
            await PowerCmd.Remove(booPower);
        }
    }

    private async Task EnsureApplauseExecutionPower()
    {
        if (Owner.Creature.GetPower<GuignolsApplauseExecutionPower>() != null)
        {
            return;
        }

        await PowerCmd.Apply<GuignolsApplauseExecutionPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            ExecutionThresholdPercent,
            Owner.Creature,
            null,
            false);
    }

    private async Task EnsureBooExecutionPower()
    {
        if (Owner.Creature.GetPower<GuignolsBooExecutionPower>() != null)
        {
            return;
        }

        await PowerCmd.Apply<GuignolsBooExecutionPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            ExecutionThresholdPercent,
            Owner.Creature,
            null,
            false);
    }

    private static bool IsImpressiveKill(DamageResult result, Creature target)
    {
        if (!result.WasTargetKilled || target.MaxHp <= 0)
        {
            return false;
        }

        int hpBeforeDamage = target.CurrentHp + result.UnblockedDamage;
        return hpBeforeDamage * 100 > target.MaxHp * KillStartingHpPercent;
    }
}
