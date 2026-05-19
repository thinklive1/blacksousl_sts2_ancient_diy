using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterPower]
public class HlanithWineExtraTurnPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/HlanithWineRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/HlanithWineRelic.png"
    );

    public override bool ShouldTakeExtraTurn(Player player)
    {
        return Owner.Player == player && Amount > 0;
    }

    public override Task AfterTakingExtraTurn(Player player)
    {
        if (Owner.Player != player)
        {
            return Task.CompletedTask;
        }

        Flash();
        return PowerCmd.Decrement(this);
    }
}
