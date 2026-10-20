using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Displays whether the Dance Shoes draw bonus is active this turn.</summary>
[RegisterPower]
public sealed class RedDanceShoesPower : ModPowerTemplate
{
    private const string PowerIconPath = "res://bs_ancient/assets/images/powers/RedDanceShoesPower.png";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => Amount == GetIndicatorAmount(true) ? 1 : 0;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: PowerIconPath, BigIconPath: PowerIconPath);

    // Keep both states above zero so the game retains the inactive indicator.
    internal static int GetIndicatorAmount(bool enabled) => enabled ? 2 : 1;
}
