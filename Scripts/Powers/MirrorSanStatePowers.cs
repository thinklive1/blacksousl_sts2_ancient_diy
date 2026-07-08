using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the San High power.</summary>
[RegisterPower]
public sealed class SanHighPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/powers/SanHighPower.png",
        BigIconPath: "res://bs_ancient/assets/images/powers/SanHighPower.png"
    );
}

/// <summary>Implements the San Low power.</summary>
[RegisterPower]
public sealed class SanLowPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/powers/SanLowPower.png",
        BigIconPath: "res://bs_ancient/assets/images/powers/SanLowPower.png"
    );
}
