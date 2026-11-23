using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Sts2Diy.Scripts.Cards;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class NegativeEnergyAbsorptionTests : IDisposable
{
    private readonly Harmony _harmony = new("sts2diy.tests.absorption");
    private static readonly List<(Creature Target, decimal Amount)> Absorbed = [];
    private static decimal _block;

    public NegativeEnergyAbsorptionTests()
    {
        Absorbed.Clear();
        _block = 0;
        var machine = AccessTools.Method(typeof(NegativeEnergyAbsorptionCard), "OnPlay")
            .GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        _harmony.Patch(AccessTools.Method(machine, "MoveNext"),
            transpiler: new HarmonyMethod(typeof(NegativeEnergyAbsorptionTests), nameof(ReplaceCommands)));
        _harmony.Patch(AccessTools.PropertyGetter(typeof(Creature), nameof(Creature.IsHittable)),
            prefix: new HarmonyMethod(typeof(NegativeEnergyAbsorptionTests), nameof(Hittable)));
    }

    public void Dispose() => _harmony.UnpatchAll(_harmony.Id);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(5, 2)]
    [InlineData(10, 5)]
    [InlineData(15, 7)]
    public async Task AbsorbsOnlyTheChosenEnemyAndRoundsDown(int doom, int amount)
    {
        var fixture = Fixture(doom);
        await Play(fixture.Card, fixture.Enemy);
        Assert.Equal(doom - amount, fixture.Enemy.GetPowerAmount<DoomPower>());
        Assert.Equal(8 + amount, fixture.Player.Creature.GetPowerAmount<DoomPower>());
        Assert.Equal(30, fixture.OtherEnemy.GetPowerAmount<DoomPower>());
        Assert.Equal((decimal)amount, _block);
        Assert.Equal(amount > 0 ? 1 : 0, Absorbed.Count);
        if (amount > 0) Assert.Equal((fixture.Player.Creature, (decimal)amount), Absorbed[0]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AlliesAndDeadEnemiesCannotBeAbsorbed(bool ally)
    {
        var fixture = Fixture(10);
        if (ally) AccessTools.Field(typeof(Creature), "<Side>k__BackingField").SetValue(fixture.Enemy, CombatSide.Player);
        else AccessTools.Field(typeof(Creature), "_currentHp").SetValue(fixture.Enemy, 0);
        await Play(fixture.Card, fixture.Enemy);
        Assert.Equal(10, fixture.Enemy.GetPowerAmount<DoomPower>());
        Assert.Equal(8, fixture.Player.Creature.GetPowerAmount<DoomPower>());
        Assert.Empty(Absorbed);
        Assert.Equal(0m, _block);
    }

    [Fact]
    public void UpgradeStillRemovesExhaust()
    {
        var fixture = Fixture(10);
        Assert.Contains(CardKeyword.Exhaust, fixture.Card.CanonicalKeywords);
        AccessTools.Field(typeof(CardModel), "_isUpgrading").SetValue(fixture.Card, true);
        AccessTools.Method(typeof(NegativeEnergyAbsorptionCard), "OnUpgrade").Invoke(fixture.Card, null);
        Assert.DoesNotContain(CardKeyword.Exhaust, fixture.Card.GetKeywordsWithSources(KeywordSources.Local));
    }

    private static IEnumerable<CodeInstruction> ReplaceCommands(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.operand is MethodInfo method && method.DeclaringType == typeof(PowerCmd))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(NegativeEnergyAbsorptionTests),
                    method.Name == nameof(PowerCmd.Apply) ? nameof(ApplyDoom) : nameof(RemoveDoom));
            }
            else if (instruction.operand is MethodInfo command && command.DeclaringType == typeof(CreatureCmd)
                && command.Name == nameof(CreatureCmd.GainBlock))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(NegativeEnergyAbsorptionTests), nameof(GainBlock));
            }
            yield return instruction;
        }
    }

    private static bool Hittable(Creature __instance, ref bool __result)
    {
        __result = __instance.IsAlive;
        return false;
    }

    private static Task<int> RemoveDoom(PlayerChoiceContext context, PowerModel power, decimal amount,
        Creature? applier, CardModel? source, bool silent)
    {
        Assert.IsType<NegativeEnergyAbsorptionCard>(source);
        Assert.Same(source.Owner.Creature, applier);
        Assert.Equal(CombatSide.Enemy, power.Owner.Side);
        int remaining = power.Amount + (int)amount;
        Assert.True(remaining >= 0);
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(power, remaining);
        return Task.FromResult(remaining);
    }

    private static Task<DoomPower?> ApplyDoom(PlayerChoiceContext context, Creature target, decimal amount,
        Creature? applier, CardModel? source, bool silent)
    {
        Assert.IsType<NegativeEnergyAbsorptionCard>(source);
        Assert.Same(source.Owner.Creature, target);
        Assert.Same(target, applier);
        Absorbed.Add((target, amount));
        DoomPower doom = target.GetPower<DoomPower>()!;
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(doom, doom.Amount + (int)amount);
        return Task.FromResult<DoomPower?>(doom);
    }

    private static Task<decimal> GainBlock(Creature creature, decimal amount, ValueProp props, CardPlay? play, bool fast)
    {
        Assert.Equal(ValueProp.Unpowered, props);
        Assert.Same(play!.Card.Owner.Creature, creature);
        _block += amount;
        return Task.FromResult(amount);
    }

    private static Task Play(NegativeEnergyAbsorptionCard card, Creature target)
    {
        var play = (CardPlay)RuntimeHelpers.GetUninitializedObject(typeof(CardPlay));
        AccessTools.Field(typeof(CardPlay), "<Card>k__BackingField").SetValue(play, card);
        AccessTools.Field(typeof(CardPlay), "<Target>k__BackingField").SetValue(play, target);
        return (Task)AccessTools.Method(typeof(NegativeEnergyAbsorptionCard), "OnPlay")
            .Invoke(card, [new ThrowingPlayerChoiceContext(), play])!;
    }

    private static (Player Player, Creature Enemy, Creature OtherEnemy, NegativeEnergyAbsorptionCard Card) Fixture(int doom)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        Creature owner = NewCreature(CombatSide.Player, 8);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, owner);
        AccessTools.Field(typeof(Creature), "<Player>k__BackingField").SetValue(owner, player);
        AccessTools.Field(typeof(Player), "_runPiles").SetValue(player, Array.Empty<CardPile>());
        var card = (NegativeEnergyAbsorptionCard)RuntimeHelpers.GetUninitializedObject(typeof(NegativeEnergyAbsorptionCard));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(card, true);
        AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
        AccessTools.Field(typeof(CardModel), "_keywords").SetValue(card, new HashSet<CardKeyword>(card.CanonicalKeywords));
        return (player, NewCreature(CombatSide.Enemy, doom), NewCreature(CombatSide.Enemy, 30), card);
    }

    private static Creature NewCreature(CombatSide side, int amount)
    {
        var creature = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(creature, 50);
        AccessTools.Field(typeof(Creature), "<Side>k__BackingField").SetValue(creature, side);
        var doom = (DoomPower)RuntimeHelpers.GetUninitializedObject(typeof(DoomPower));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(doom, true);
        AccessTools.Field(typeof(PowerModel), "_owner").SetValue(doom, creature);
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(doom, amount);
        AccessTools.Field(typeof(Creature), "_powers").SetValue(creature, new List<PowerModel> { doom });
        return creature;
    }
}
