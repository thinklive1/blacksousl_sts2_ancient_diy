using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Sts2Diy.Scripts.Powers;

/// <summary>Blocks the owner's Summon effects and counts down at each participating owner turn end.</summary>
[RegisterPower]
public sealed class NoSummonPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://sts2diy/assets/images/powers/NoSummonPower.png",
        BigIconPath: "res://sts2diy/assets/images/powers/NoSummonPower.png");

    public override decimal ModifySummonAmount(Player summoner, decimal amount, AbstractModel? source) =>
        Amount > 0 && summoner == Owner.Player ? 0m : amount;

    public override Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Amount <= 0 || side != Owner.Side || !participants.Contains(Owner))
            return Task.CompletedTask;
        return PowerCmd.Decrement(this);
    }
}
