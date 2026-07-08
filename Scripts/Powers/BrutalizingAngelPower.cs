using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Brutalizing Angel power.</summary>
[RegisterPower]
public sealed class BrutalizingAngelPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override bool IsVisibleInternal => Amount > 0;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/powers/BrutalizingAngelPower.png",
        BigIconPath: "res://bs_ancient/assets/images/powers/BrutalizingAngelPower.png"
    );
}
