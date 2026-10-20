using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Adds one card per draw while the owner keeps shuffling between turns.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class RedQueenDanceShoesRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/RedQueenDanceShoesRelic.png";
    private RedQueenDanceShoesState _state = RedQueenDanceShoesState.NewCombat;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath, IconOutlinePath: RelicIconPath, BigIconPath: RelicIconPath);

    public override bool IsAllowed(IRunState runState) => false;

    [SavedProperty]
    public bool BlackSouls_BonusEnabled
    {
        get => _state.BonusEnabled;
        set
        {
            AssertMutable();
            _state = _state with { BonusEnabled = value };
            UpdateStatus();
        }
    }

    [SavedProperty]
    public bool BlackSouls_HasStartedTurn
    {
        get => _state.HasStartedTurn;
        set
        {
            AssertMutable();
            _state = _state with { HasStartedTurn = value };
        }
    }

    [SavedProperty]
    public bool BlackSouls_ShuffledThisTurn
    {
        get => _state.ShuffledThisTurn;
        set
        {
            AssertMutable();
            _state = _state with { ShuffledThisTurn = value };
        }
    }

    public override Task BeforeCombatStart()
    {
        _state = RedQueenDanceShoesState.NewCombat;
        UpdateStatus();
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == Owner.Creature.Side && participants.Contains(Owner.Creature))
        {
            _state = _state.BeginTurn();
            UpdateStatus();
            await UpdatePowerIndicator(choiceContext);
        }
    }

    public override Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
    {
        if (shuffler == Owner && _state.HasStartedTurn)
        {
            _state = _state with { ShuffledThisTurn = true };
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _state = RedQueenDanceShoesState.NewCombat;
        UpdateStatus();
        return Task.CompletedTask;
    }

    internal decimal ModifyDrawCount(decimal count) => _state.ModifyDrawCount(count);

    private async Task UpdatePowerIndicator(PlayerChoiceContext choiceContext)
    {
        int amount = RedDanceShoesPower.GetIndicatorAmount(_state.BonusEnabled);
        RedDanceShoesPower? power = Owner.Creature.GetPower<RedDanceShoesPower>();
        power ??= await PowerCmd.Apply<RedDanceShoesPower>(
            choiceContext, Owner.Creature, amount, Owner.Creature, null, silent: true);

        // This is a display counter; update it directly without triggering duration reductions.
        power?.SetAmount(amount, silent: true);
    }

    private void UpdateStatus() => Status = _state.BonusEnabled ? RelicStatus.Normal : RelicStatus.Disabled;
}

/// <summary>Value-only turn state remains independent when relics are cloned or saved.</summary>
internal readonly record struct RedQueenDanceShoesState(
    bool HasStartedTurn, bool BonusEnabled, bool ShuffledThisTurn)
{
    public static RedQueenDanceShoesState NewCombat => new(false, true, false);

    public RedQueenDanceShoesState BeginTurn() => new(true, !HasStartedTurn || ShuffledThisTurn, false);

    public decimal ModifyDrawCount(decimal count) => BonusEnabled && count > 0m ? count + 1m : count;
}
