using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts;

public class UnicornHookBeforeDamageReceivedPatch : IPatchMethod
{
    public static string PatchId => "unicorn_hook_before_damage_received";
    public static string Description => "Skip generic before-damage receive hooks during Unicorn counterattack.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Hook), nameof(Hook.BeforeDamageReceived), ignoreIfMissing: true)];

    public static bool Prefix(ref Task __result) =>
        UnicornCounterattackReceiveEffectPatch.HookBeforeDamageReceivedPrefix(ref __result);
}

public class UnicornHookAfterDamageReceivedPatch : IPatchMethod
{
    public static string PatchId => "unicorn_hook_after_damage_received";
    public static string Description => "Skip generic after-damage receive hooks during Unicorn counterattack.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(Hook), nameof(Hook.AfterDamageReceived), ignoreIfMissing: true)];

    public static bool Prefix(ref Task __result) =>
        UnicornCounterattackReceiveEffectPatch.HookAfterDamageReceivedPrefix(ref __result);
}

public class UnicornThornsBeforeDamageReceivedPatch : IPatchMethod
{
    public static string PatchId => "unicorn_thorns_before_damage_received";
    public static string Description => "Skip Thorns during Unicorn counterattack.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(ThornsPower), nameof(ThornsPower.BeforeDamageReceived), ignoreIfMissing: true)];

    public static bool Prefix() =>
        UnicornCounterattackReceiveEffectPatch.ThornsBeforeDamageReceivedPrefix();
}

public class UnicornPersonalHiveAfterDamageReceivedPatch : IPatchMethod
{
    public static string PatchId => "unicorn_personal_hive_after_damage_received";
    public static string Description => "Skip Personal Hive during Unicorn counterattack.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(PersonalHivePower), nameof(PersonalHivePower.AfterDamageReceived), ignoreIfMissing: true)];

    public static bool Prefix() =>
        UnicornCounterattackReceiveEffectPatch.PersonalHiveAfterDamageReceivedPrefix();
}

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
