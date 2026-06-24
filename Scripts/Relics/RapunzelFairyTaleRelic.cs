using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class RapunzelFairyTaleRelic : ModRelicTemplate
{
    private const int ProtectedTurns = 2;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private int _ownerTurnCount;
    private bool _blockedPowerLoss;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Turns", ProtectedTurns)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override Task BeforeCombatStart()
    {
        _ownerTurnCount = 0;
        _blockedPowerLoss = false;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _ownerTurnCount = 0;
        _blockedPowerLoss = false;
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            _ownerTurnCount++;
        }

        return Task.CompletedTask;
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (!ShouldBlockPowerLoss(canonicalPower, target, amount))
        {
            return false;
        }

        modifiedAmount = 0m;
        _blockedPowerLoss = true;
        return true;
    }

    public override Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        if (_blockedPowerLoss)
        {
            _blockedPowerLoss = false;
            Flash();
        }

        return Task.CompletedTask;
    }

    internal static bool TryPreventDirectPowerAmountLoss(PowerModel power, ref int amount)
    {
        RapunzelFairyTaleRelic? relic = GetActiveRelicFor(power);
        if (relic == null || !relic.ShouldBlockDirectPowerLoss(power, amount))
        {
            return false;
        }

        amount = power.Amount;
        relic.Flash();
        return true;
    }

    internal static bool TryPreventDirectPowerRemoval(PowerModel? power)
    {
        if (power == null)
        {
            return false;
        }

        RapunzelFairyTaleRelic? relic = GetActiveRelicFor(power);
        if (relic == null || !relic.ShouldProtectPower(power))
        {
            return false;
        }

        relic.Flash();
        return true;
    }

    private static RapunzelFairyTaleRelic? GetActiveRelicFor(PowerModel power)
    {
        return power.Owner?.Player?.GetRelic<RapunzelFairyTaleRelic>();
    }

    private bool ShouldBlockPowerLoss(PowerModel power, Creature target, decimal amount)
    {
        return target == Owner.Creature
            && amount < 0m
            && _ownerTurnCount is > 0 and <= ProtectedTurns
            && power.IsVisible;
    }

    private bool ShouldBlockDirectPowerLoss(PowerModel power, int nextAmount)
    {
        return nextAmount < power.Amount && ShouldProtectPower(power);
    }

    private bool ShouldProtectPower(PowerModel power)
    {
        return power.Owner == Owner.Creature
            && _ownerTurnCount is > 0 and <= ProtectedTurns
            && power.IsVisible;
    }
}
