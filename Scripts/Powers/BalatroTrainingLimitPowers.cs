using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Shows the number of poker hands that may still be played.</summary>
[RegisterPower]
public sealed class BalatroTrainingPlayLimitPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override bool IsVisibleInternal => true;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/powers/BalatroPlayLimitPower.png",
        BigIconPath: "res://bs_ancient/assets/images/powers/BalatroPlayLimitPower.png");
}

/// <summary>Shows the number of cards that may still be discarded.</summary>
[RegisterPower]
public sealed class BalatroTrainingDiscardLimitPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override bool IsVisibleInternal => true;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/powers/BalatroDiscardLimitPower.png",
        BigIconPath: "res://bs_ancient/assets/images/powers/BalatroDiscardLimitPower.png");
}
