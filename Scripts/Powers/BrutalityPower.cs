using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Brutality power.</summary>
[RegisterPower]
public sealed class BrutalityPower : ModPowerTemplate
{
    private const string PowerIconPath = "res://bs_ancient/assets/images/enchantment/BrutalityEnchantment.png";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: PowerIconPath,
        BigIconPath: PowerIconPath
    );
}
