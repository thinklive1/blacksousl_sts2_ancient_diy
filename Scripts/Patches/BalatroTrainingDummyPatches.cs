using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts.Patches;

public sealed class BalatroBeforeHandDrawPatch : IPatchMethod
{
    public static string PatchId => "balatro_training_initialize_deck";
    public static string Description => "Replace the combat deck for the poker dummy encounter.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => [new(typeof(Hook), nameof(Hook.BeforeHandDraw))];

    public static void Postfix(ICombatState combatState, Player player, ref Task __result) =>
        __result = Continue(__result, combatState, player);

    private static async Task Continue(Task original, ICombatState combatState, Player player)
    {
        await original;
        await BalatroTrainingCombatService.InitializeDeck(combatState, player);
    }
}

public sealed class BalatroModifyHandDrawPatch : IPatchMethod
{
    public static string PatchId => "balatro_training_hand_size";
    public static string Description => "Keep the poker dummy hand at ten cards.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => [new(typeof(Hook), nameof(Hook.ModifyHandDraw))];

    public static void Postfix(ICombatState combatState, Player player, ref decimal __result) =>
        __result = BalatroTrainingCombatService.ModifyHandDraw(combatState, player, __result);
}

public sealed class BalatroCombatUiPatch : IPatchMethod
{
    public static string PatchId => "balatro_training_combat_controls";
    public static string Description => "Add Play Hand and Discard controls to the poker dummy encounter.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => [new(typeof(NCombatUi), nameof(NCombatUi.Activate))];

    public static void Postfix(NCombatUi __instance, CombatState state) =>
        BalatroTrainingCombatService.AttachControls(__instance, state);
}

public sealed class BalatroDummyTimeLimitPatch : IPatchMethod
{
    public static string PatchId => "balatro_training_timeout";
    public static string Description => "Record timeout when the vanilla dummy timer expires.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(BattlewornDummyTimeLimitPower), nameof(BattlewornDummyTimeLimitPower.AfterSideTurnEnd))];

    public static void Prefix(
        BattlewornDummyTimeLimitPower __instance,
        IEnumerable<Creature> participants)
    {
        if (__instance.Amount <= 1
            && participants.Contains(__instance.Owner)
            && __instance.Owner.CombatState?.Encounter is BalatroTrainingDummyEncounter encounter)
        {
            encounter.RanOutOfTime = true;
        }
    }
}

public sealed class BalatroDirectCardPlayPatch : IPatchMethod
{
    public static string PatchId => "balatro_training_disable_direct_card_play";
    public static string Description => "Require poker-table cards to be resolved through the encounter controls.";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => [new(typeof(Hook), nameof(Hook.ShouldPlay))];

    public static bool Prefix(
        ICombatState combatState,
        CardModel card,
        AutoPlayType autoPlayType,
        ref bool __result,
        ref AbstractModel? preventer)
    {
        if (!BalatroTrainingCombatService.IsActive(combatState)
            || autoPlayType != AutoPlayType.None
            || card.Enchantment is not PlayingCardSuitEnchantment)
        {
            return true;
        }

        preventer = card;
        __result = false;
        return false;
    }
}
