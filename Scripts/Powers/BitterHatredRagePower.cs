using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Sts2Diy.Scripts.Powers;

/// <summary>Displays Endless Hatred's active damage bonus on Osty without applying it twice.</summary>
[RegisterPower]
public sealed class BitterHatredRagePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile
    {
        get
        {
            PowerModel original = ModelDb.Power<RagePower>();
            return new(IconPath: original.IconPath, BigIconPath: original.ResolvedBigIconPath);
        }
    }
}
