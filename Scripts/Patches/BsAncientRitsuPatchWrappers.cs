using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts;

/// <summary>Applies behavior patches for Mirror San Fairy Reset.</summary>
public class MirrorSanFairyResetPatch : IPatchMethod
{
    public static string PatchId => "mirror_san_fairy_reset";
    public static string Description => "Reset mirror SAN when Fairy in a Bottle triggers.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(FairyInABottle), nameof(FairyInABottle.AfterPreventingDeath), ignoreIfMissing: true)];

    public static void Postfix(Creature creature)
    {
        MirrorSan.Get(creature.Player)?.ResetSan();
    }
}

/// <summary>Applies behavior patches for Mirror San Lizard Tail Reset.</summary>
public class MirrorSanLizardTailResetPatch : IPatchMethod
{
    public static string PatchId => "mirror_san_lizard_tail_reset";
    public static string Description => "Reset mirror SAN when Lizard Tail triggers.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(LizardTail), nameof(LizardTail.AfterPreventingDeath), ignoreIfMissing: true)];

    public static void Postfix(Creature creature)
    {
        MirrorSan.Get(creature.Player)?.ResetSan();
    }
}
