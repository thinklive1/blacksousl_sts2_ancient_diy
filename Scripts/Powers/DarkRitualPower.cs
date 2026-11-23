using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Sts2Diy.Scripts.Powers;

/// <summary>Each played copy pays its own maximum-HP cost before summoning.</summary>
[RegisterPower]
public sealed class DarkRitualPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://sts2diy/assets/images/powers/DarkRitualPower.png",
        BigIconPath: "res://sts2diy/assets/images/powers/DarkRitualPower.png");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player || Owner.IsDead || Amount <= 0 || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        Flash();
#if STS2DIY_STABLE_1110
        await CreatureCmd.LoseMaxHp(context, Owner, 1m, isFromCard: false);
#else
        await CreatureCmd.LoseMaxHp(context, Owner, 1m, cardSource: null);
#endif
        // HP loss can end combat through damage reactions; do not summon into an ended fight.
        if (Owner.IsAlive && !CombatManager.Instance.IsOverOrEnding)
        {
            await OstyCmd.Summon(context, player, Amount, this);
        }
    }
}
