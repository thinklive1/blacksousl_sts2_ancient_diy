using System.Reflection;
using System.Runtime.CompilerServices;
using BlackSouls.Scripts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace BsAncient.Tests;

public sealed class ExecutionEnchantmentTests
{
    private static readonly List<PowerModel> RemovedPowers = [];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DamageHookAttributesExecutionToTheCardWhetherTheDealerIsPlayerOrOsty(bool ostyAttack)
    {
        var (enchantment, card, playerCreature, osty) = CreateOwnedEnchantment();
        var enemy = CreateCreature(0);
        AccessTools.Field(typeof(Creature), "<Side>k__BackingField").SetValue(enemy, CombatSide.Enemy);
        var illusion = CreatePower<IllusionPower>();
        AccessTools.Field(typeof(Creature), "_powers").SetValue(enemy, new List<PowerModel> { illusion });
        var result = new DamageResult(enemy, ValueProp.Move) { WasTargetKilled = true, UnblockedDamage = 21 };
        var context = new ThrowingPlayerChoiceContext();
        await enchantment.AfterDamageGiven(context, ostyAttack ? osty : playerCreature, result,
            ValueProp.Move, enemy, card);
        Assert.Contains(enemy, Targets(enchantment));

        Harmony harmony = new("bs_ancient.tests.execution_source");
        try
        {
            harmony.Patch(AccessTools.Method(typeof(PowerCmd), nameof(PowerCmd.Remove), [typeof(PowerModel)]),
                prefix: new HarmonyMethod(typeof(ExecutionEnchantmentTests), nameof(CaptureRemove)));
            await enchantment.BeforeDeath(enemy);
            Assert.Same(illusion, Assert.Single(RemovedPowers));
            Assert.Empty(Targets(enchantment));
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
            RemovedPowers.Clear();
        }
    }

    [Fact]
    public async Task OtherCardsAndAlliedTargetsCannotAcquireAnExecutionMark()
    {
        var (enchantment, card, playerCreature, osty) = CreateOwnedEnchantment();
        var enemy = CreateCreature(0);
        AccessTools.Field(typeof(Creature), "<Side>k__BackingField").SetValue(enemy, CombatSide.Enemy);
        var otherCard = (Rattle)RuntimeHelpers.GetUninitializedObject(typeof(Rattle));
        var context = new ThrowingPlayerChoiceContext();
        var result = new DamageResult(enemy, ValueProp.Move) { WasTargetKilled = true };
        await enchantment.AfterDamageGiven(context, osty, result, ValueProp.Move, enemy, otherCard);
        await enchantment.AfterDamageGiven(context, osty, result, ValueProp.Move, enemy, null);
        await enchantment.AfterDamageGiven(context, osty, result, ValueProp.Move, playerCreature, card);
        Assert.Empty(Targets(enchantment));
    }

    private static (ExecutionEnchantment Enchantment, CardModel Card, Creature Player, Creature Osty) CreateOwnedEnchantment()
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var creature = CreateCreature(50);
        var osty = CreateCreature(10);
        AccessTools.Field(typeof(Creature), "<Side>k__BackingField").SetValue(creature, CombatSide.Player);
        AccessTools.Field(typeof(Creature), "<Side>k__BackingField").SetValue(osty, CombatSide.Player);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
        var card = (Rattle)RuntimeHelpers.GetUninitializedObject(typeof(Rattle));
        AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(card, true);
        var enchantment = new ExecutionEnchantment();
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(enchantment, true);
        enchantment.Card = card;
        return (enchantment, card, creature, osty);
    }

    [Fact]
    public async Task MarkedDeathsRemoveRevivalThroughTheNativeCommandBeforeDeathContinues()
    {
        var enchantment = new ExecutionEnchantment();
        var first = CreateCreature(0);
        var second = CreateCreature(0);
        var illusion = CreatePower<IllusionPower>();
        var reattach = CreatePower<ReattachPower>();
        var adaptable = CreatePower<AdaptablePower>();
        AccessTools.Field(typeof(Creature), "_powers").SetValue(first, new List<PowerModel> { illusion, reattach });
        AccessTools.Field(typeof(Creature), "_powers").SetValue(second, new List<PowerModel> { adaptable });
        var context = new ThrowingPlayerChoiceContext();
        await enchantment.HandleSuccessfulKill(context, first);
        await enchantment.HandleSuccessfulKill(context, second);
        Assert.Empty(RemovedPowers);

        // Replace only the Godot-facing command boundary, exercising the real hook and async removal loop.
        Harmony harmony = new("bs_ancient.tests.execution_removal");
        try
        {
            harmony.Patch(AccessTools.Method(typeof(PowerCmd), nameof(PowerCmd.Remove), [typeof(PowerModel)]),
                prefix: new HarmonyMethod(typeof(ExecutionEnchantmentTests), nameof(CaptureRemove)));
            await enchantment.BeforeDeath(first);
            await enchantment.BeforeDeath(second);
            Assert.Equal(new PowerModel[] { illusion, reattach, adaptable }, RemovedPowers);
            await enchantment.BeforeDeath(first);
            Assert.Equal(3, RemovedPowers.Count);
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
            RemovedPowers.Clear();
        }
    }

    private static bool CaptureRemove(PowerModel? power, ref Task __result)
    {
        Assert.NotNull(power);
        RemovedPowers.Add(power);
        __result = Task.CompletedTask;
        return false;
    }

    [Fact]
    public async Task OnePlayMarksEveryKilledTargetWithoutSuppressingSubsequentKills()
    {
        var enchantment = new ExecutionEnchantment();
        var first = CreateCreature(0);
        var second = CreateCreature(0);
        var context = new ThrowingPlayerChoiceContext();
        await enchantment.HandleSuccessfulKill(context, first);
        await enchantment.HandleSuccessfulKill(context, second);
        await enchantment.HandleSuccessfulKill(context, first);
        Assert.Equal(2, Targets(enchantment).Count);
        Assert.Contains(first, Targets(enchantment));
        Assert.Contains(second, Targets(enchantment));
        Assert.True((bool)AccessTools.Field(typeof(ExecutionEnchantment), "_returnedThisPlay").GetValue(enchantment)!);
    }

    [Fact]
    public async Task LivingAndUnrelatedCreaturesDoNotLoseRevivalPowers()
    {
        var enchantment = new ExecutionEnchantment();
        var living = CreateCreature(10);
        await enchantment.HandleSuccessfulKill(new ThrowingPlayerChoiceContext(), living);
        Assert.Empty(Targets(enchantment));
        await enchantment.BeforeDeath(CreateCreature(0));
        Assert.Empty(Targets(enchantment));
    }

    [Fact]
    public async Task DeathPreparationConsumesEachMarkedTargetOnce()
    {
        var enchantment = new ExecutionEnchantment();
        var target = CreateCreature(0);
        await enchantment.HandleSuccessfulKill(new ThrowingPlayerChoiceContext(), target);
        await enchantment.BeforeDeath(target);
        Assert.Empty(Targets(enchantment));
        await enchantment.BeforeDeath(target);
    }

    [Fact]
    public void OnlyNativeRevivalPowersAreSelectedWithoutRemovingMinionOrOtherBuffs()
    {
        PowerModel[] revival = [CreatePower<ReattachPower>(), CreatePower<AdaptablePower>(), CreatePower<IllusionPower>()];
        var target = CreateCreature(0);
        AccessTools.Field(typeof(Creature), "_powers").SetValue(target,
            new List<PowerModel>(revival) { CreatePower<MinionPower>(), CreatePower<StrengthPower>() });
        Assert.Equal(revival, ExecutionEnchantment.GetRevivalPowers(target));
    }

    [Fact]
    public void NoLateOnPlayRemovalOrNestedForceKillCanRun()
    {
        Assert.Null(typeof(ExecutionEnchantment).GetMethod("OnPlay",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
        MethodInfo mark = typeof(ExecutionEnchantment).GetMethod(nameof(ExecutionEnchantment.HandleSuccessfulKill))!;
        Assert.Null(mark.GetCustomAttribute<AsyncStateMachineAttribute>());
        Assert.DoesNotContain(PatchProcessor.GetOriginalInstructions(mark), i => i.operand is MethodInfo m
            && (m.Name is "Kill" or "Remove" or "DisableRevivalPowers"));
        Assert.Contains(PatchProcessor.GetOriginalInstructions(typeof(ExecutionEnchantment)
            .GetMethod(nameof(ExecutionEnchantment.BeforeDeath))!), i => i.operand is MethodInfo m
            && m.Name == "DisableRevivalPowers");
    }

    private static HashSet<Creature> Targets(ExecutionEnchantment enchantment) =>
        (HashSet<Creature>)AccessTools.Field(typeof(ExecutionEnchantment), "_executionTargetsThisPlay").GetValue(enchantment)!;

    private static T CreatePower<T>() where T : PowerModel => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    private static Creature CreateCreature(int hp)
    {
        var creature = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(creature, hp);
        AccessTools.Field(typeof(Creature), "_powers").SetValue(creature, new List<PowerModel>());
        return creature;
    }
}
