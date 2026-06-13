using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts;

public class HungerEvilQiAfflictPatch : IPatchMethod
{
    public static string PatchId => "hunger_evil_qi_afflict";
    public static string Description => "Allow Hunger Afflict to overwrite Evil Qi.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(HungerPower), "Afflict", ignoreIfMissing: true)];

    public static bool Prefix(HungerPower __instance, CardModel card, ref Task __result) =>
        HungerEvilQiOverwritePatch.AfflictPrefix(__instance, card, ref __result);
}

public class HungerEvilQiAfterCardEnteredCombatPatch : IPatchMethod
{
    public static string PatchId => "hunger_evil_qi_after_card_entered_combat";
    public static string Description => "Allow Hunger card-entered-combat affliction to overwrite Evil Qi.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(HungerPower), nameof(HungerPower.AfterCardEnteredCombat), ignoreIfMissing: true)];

    public static bool Prefix(HungerPower __instance, CardModel card, ref Task __result) =>
        HungerEvilQiOverwritePatch.AfterCardEnteredCombatPrefix(__instance, card, ref __result);
}

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

public class DeprecatedAncientTextureFallbackPatch : IPatchMethod
{
    private const string MapIconPath = "res://bs_ancient/assets/images/map/grand_guignol.png";
    private const string MapIconOutlinePath = "res://bs_ancient/assets/images/map/grand_guignol_outline.png";

    public static string PatchId => "deprecated_ancient_texture_fallback";
    public static string Description => "Use existing Grand Guignol textures when a deprecated ancient appears in a saved run.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [
            new(typeof(DeprecatedAncientEvent), nameof(DeprecatedAncientEvent.MapIcon), null, true, HarmonyLib.MethodType.Getter),
            new(typeof(DeprecatedAncientEvent), nameof(DeprecatedAncientEvent.MapIconOutline), null, true, HarmonyLib.MethodType.Getter),
            new(typeof(DeprecatedAncientEvent), nameof(DeprecatedAncientEvent.RunHistoryIcon), null, true, HarmonyLib.MethodType.Getter),
            new(typeof(DeprecatedAncientEvent), nameof(DeprecatedAncientEvent.RunHistoryIconOutline), null, true, HarmonyLib.MethodType.Getter),
        ];

    public static bool Prefix(System.Reflection.MethodBase __originalMethod, ref Godot.Texture2D __result)
    {
        string path = __originalMethod.Name.Contains("Outline", StringComparison.Ordinal)
            ? MapIconOutlinePath
            : MapIconPath;
        __result = Godot.ResourceLoader.Load<Godot.Texture2D>(path);
        return false;
    }
}

public class DeprecatedAncientMapNodeAssetPathsFallbackPatch : IPatchMethod
{
    private const string MapIconPath = "res://bs_ancient/assets/images/map/grand_guignol.png";
    private const string MapIconOutlinePath = "res://bs_ancient/assets/images/map/grand_guignol_outline.png";

    public static string PatchId => "deprecated_ancient_map_node_asset_paths_fallback";
    public static string Description => "Use existing Grand Guignol asset paths when a deprecated ancient appears in a saved run.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(DeprecatedAncientEvent), nameof(DeprecatedAncientEvent.MapNodeAssetPaths), null, true, HarmonyLib.MethodType.Getter)];

    public static bool Prefix(ref IEnumerable<string> __result)
    {
        __result = [MapIconPath, MapIconOutlinePath];
        return false;
    }
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
