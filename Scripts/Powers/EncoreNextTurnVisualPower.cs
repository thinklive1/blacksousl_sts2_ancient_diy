using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Encore Next Turn Visual power.</summary>
[RegisterPower]
public sealed class EncoreNextTurnVisualPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override bool IsVisibleInternal => Amount > 0;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/StagnantGearRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/StagnantGearRelic.png"
    );
}
