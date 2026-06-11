using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterPower]
public sealed class RedQueenDiceBigSuccessPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png"
    );

    public override Task AfterCombatEnd(CombatRoom room)
    {
        Player? player = Owner.Player;
        if (player == null)
        {
            return Task.CompletedTask;
        }

        for (int i = 0; i < Amount; i++)
        {
            room.AddExtraReward(player, new RedQueenBigSuccessCardReward(player));
            room.AddExtraReward(player, new CardRemovalReward(player));
        }

        return Task.CompletedTask;
    }
}
