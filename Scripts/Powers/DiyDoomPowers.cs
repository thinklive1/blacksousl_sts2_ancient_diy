using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils.Persistence;

namespace Sts2Diy.Scripts.Powers;

/// <summary>Tracks Doom applications and grants energy for every four applications.</summary>
[RegisterPower]
public sealed class CalamityRaimentPower : ModPowerTemplate
{
    private int _doomApplications;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => _doomApplications;

    public override PowerAssetProfile AssetProfile
    {
        get
        {
            PowerModel original = ModelDb.Power<ShroudPower>();
            return new(IconPath: original.IconPath, BigIconPath: original.ResolvedBigIconPath);
        }
    }

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            AddDescriptionVariables(description, IsMutable ? Sts2Diy_DoomApplications : 0, IsMutable ? Amount : 1);
            return description;
        }
    }

    internal static (int Progress, int Energy) AdvanceProgress(int previousProgress, int stacks)
    {
        int applications = Math.Clamp(previousProgress, 0, 3) + 1;
        return (applications % 4, applications == 4 ? Math.Max(0, stacks) : 0);
    }

    internal static void AddDescriptionVariables(LocString description, int progress, int stacks)
    {
        description.Add("DoomApplications", progress);
        description.Add("EnergyPerTrigger", stacks);
    }

    [SavedProperty]
    public int Sts2Diy_DoomApplications
    {
        get => _doomApplications;
        set { AssertMutable(); _doomApplications = value; }
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (applier != Owner || power is not DoomPower || amount <= 0 || Owner.Player == null)
        {
            return;
        }

        var next = AdvanceProgress(Sts2Diy_DoomApplications, Amount);
        // Commit the remainder before granting energy, since energy gain can invoke more hooks.
        Sts2Diy_DoomApplications = next.Progress;
        if (next.Energy > 0)
        {
            Flash();
            await PlayerCmd.GainEnergy(next.Energy, Owner.Player);
        }

        InvokeDisplayAmountChanged();
    }
}

/// <summary>Provides the one-turn damage reactions granted by Death and Life's Pact.</summary>
[RegisterPower]
public sealed class DeathLifePactPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://sts2diy/assets/images/powers/DeathLifePactPower.png",
        BigIconPath: "res://sts2diy/assets/images/powers/DeathLifePactPower.png");

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            AddDescriptionVariables(description, IsMutable ? Amount : 2);
            return description;
        }
    }

    internal static decimal GetOstyBlock(int summonAmount) => Math.Max(0, summonAmount) * 2m;

    internal static void AddDescriptionVariables(LocString description, int summonAmount)
    {
        description.Add("SummonAmount", summonAmount);
        description.Add("BlockAmount", GetOstyBlock(summonAmount));
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (result.TotalDamage <= 0 || Owner.Player == null || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        if (dealer == Owner)
        {
            Flash();
            await OstyCmd.Summon(context, Owner.Player, Amount, this);
        }
        else if (dealer == Owner.Player.Osty)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner, GetOstyBlock(Amount), ValueProp.Unpowered, null);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side && participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}

/// <summary>Moves the owner's Doom to all enemies at the next player-turn start.</summary>
[RegisterPower]
public sealed class SharedDoomTransferPower : ModPowerTemplate
{
    private bool _doomProtection;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://sts2diy/assets/images/powers/SharedDoomTransferPower.png",
        BigIconPath: "res://sts2diy/assets/images/powers/SharedDoomTransferPower.png");

    [SavedProperty]
    public bool Sts2Diy_DoomProtection
    {
        get => _doomProtection;
        set { AssertMutable(); _doomProtection = value; }
    }

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            description.Add("DoomProtection", IsMutable && Sts2Diy_DoomProtection);
            return description;
        }
    }

    public override Task AfterSideTurnEndLate(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        // Player Doom resolves in AfterSideTurnEnd, before this late cleanup phase.
        if (side == Owner.Side && participants.Contains(Owner))
        {
            Sts2Diy_DoomProtection = false;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        Sts2Diy_DoomProtection = false;
        int doom = Owner.GetPowerAmount<DoomPower>();
        if (doom > 0)
        {
            ICombatState? combatState = Owner.CombatState;
            if (combatState != null)
            {
                await PowerCmd.Apply<DoomPower>(context, combatState.HittableEnemies, doom, Owner, null);
            }
            DoomPower? ownDoom = Owner.GetPower<DoomPower>();
            if (ownDoom != null)
            {
                await PowerCmd.ModifyAmount(context, ownDoom, -doom, Owner, null);
            }
        }

        await PowerCmd.Remove(this);
    }
}

/// <summary>Multiplies consecutive Osty death bonuses, resetting to one stack after a deathless turn.</summary>
[RegisterPower]
public sealed class BitterHatredPower : ModPowerTemplate
{
    private bool _pending;
    private bool _active;
    private int _pendingDeaths;
    private int _activeDeaths;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://sts2diy/assets/images/powers/BitterHatredPower.png",
        BigIconPath: "res://sts2diy/assets/images/powers/BitterHatredPower.png");

    [SavedProperty]
    public bool Sts2Diy_Pending
    {
        get => Sts2Diy_PendingDeaths > 0;
        set { AssertMutable(); _pending = value; }
    }

    [SavedProperty]
    public bool Sts2Diy_Active
    {
        get => Sts2Diy_ActiveDeaths > 0;
        set { AssertMutable(); _active = value; }
    }

    [SavedProperty]
    public int Sts2Diy_PendingDeaths
    {
        get => Math.Max(_pendingDeaths, _pending ? 1 : 0);
        set { AssertMutable(); _pendingDeaths = Math.Max(0, value); _pending = value > 0; }
    }

    [SavedProperty]
    public int Sts2Diy_ActiveDeaths
    {
        get => Math.Max(_activeDeaths, _active ? 1 : 0);
        set { AssertMutable(); _activeDeaths = Math.Max(0, value); _active = value > 0; }
    }

    internal int ActiveBonusPercent => (int)decimal.Floor(
        (GetDamageMultiplier(Amount, Sts2Diy_ActiveDeaths) - 1m) * 100m);

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            description.Add("BonusPercent", IsMutable ? ActiveBonusPercent : 0m);
            return description;
        }
    }

    internal static decimal GetDamageMultiplier(int bonus, int deaths)
    {
        // Bound extreme stacks to the native integer status-counter range and avoid decimal overflow.
        const decimal limit = 1m + int.MaxValue / 100m;
        decimal factor = 1m + Math.Max(0, bonus) / 100m;
        decimal result = 1m;
        for (int remaining = Math.Max(0, deaths); remaining > 0; remaining >>= 1)
        {
            if ((remaining & 1) != 0) result = Math.Min(limit, result * factor);
            factor = Math.Min(limit, factor * factor);
        }
        return result;
    }

    public override Task AfterDeath(PlayerChoiceContext context, Creature creature,
#if !STS2DIY_STABLE_1110
        CardModel? cardSource,
#endif
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (!wasRemovalPrevented && creature == Owner.Player?.Osty)
        {
            Sts2Diy_PendingDeaths = (int)Math.Min(int.MaxValue, (long)Sts2Diy_PendingDeaths + 1);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        if (Sts2Diy_Pending)
        {
            Sts2Diy_ActiveDeaths = (int)Math.Min(int.MaxValue,
                (long)Sts2Diy_ActiveDeaths + Sts2Diy_PendingDeaths);
            Sts2Diy_PendingDeaths = 0;
            Flash();
            await SyncRageDisplay(context);
        }
        else if (Sts2Diy_ActiveDeaths > 1)
        {
            Sts2Diy_ActiveDeaths = 1;
            await SyncRageDisplay(context);
        }
    }

    public override Task AfterSummon(PlayerChoiceContext context, Player summoner, decimal amount) =>
        Sts2Diy_Active && summoner == Owner.Player ? SyncRageDisplay(context) : Task.CompletedTask;

    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource) =>
        Sts2Diy_Active && power == this ? SyncRageDisplay(context) : Task.CompletedTask;

    private async Task SyncRageDisplay(PlayerChoiceContext context)
    {
        if (Owner.Player?.Osty is not { IsAlive: true } osty)
        {
            return;
        }

        BitterHatredRagePower? rage = osty.GetPower<BitterHatredRagePower>();
        if (!Sts2Diy_Active)
        {
            if (rage != null) await PowerCmd.Remove(rage);
            return;
        }

        // This marker is visual only; the player's power remains the sole damage modifier.
        int difference = ActiveBonusPercent - (rage?.Amount ?? 0);
        if (difference != 0)
        {
            await PowerCmd.Apply<BitterHatredRagePower>(context, osty, difference, Owner, null);
        }
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        return Sts2Diy_Active && dealer == Owner.Player?.Osty && props.IsPoweredAttack()
            ? GetDamageMultiplier(Amount, Sts2Diy_ActiveDeaths)
            : 1m;
    }

    public override Task AfterRemoved(Creature owner) =>
        owner.Player?.Osty?.GetPower<BitterHatredRagePower>() is { } rage
            ? PowerCmd.Remove(rage) : Task.CompletedTask;
}
