using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Sts2Diy.Scripts.Cards;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class BorrowedBodyTests : IDisposable
{
    private readonly Harmony _harmony = new("sts2diy.tests.borrowed_body");
    private static List<(Creature Target, decimal Amount)> _applications = [];
    private static List<decimal> _removals = [];
    private static int _selfDelta;
    private static List<Creature> _intangibles = [];

    public BorrowedBodyTests()
    {
        _applications = [];
        _removals = [];
        _selfDelta = 0;
        _intangibles = [];
        MethodInfo play = AccessTools.Method(typeof(BorrowedBodyCard), "OnPlay");
        Type machine = play.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        _harmony.Patch(AccessTools.Method(machine, "MoveNext"),
            transpiler: new HarmonyMethod(typeof(BorrowedBodyTests), nameof(ReplaceCommands)));
        // Creature hook dispatch is outside this fixture; retain native live-enemy filtering.
        _harmony.Patch(AccessTools.PropertyGetter(typeof(Creature), nameof(Creature.IsHittable)),
            prefix: new HarmonyMethod(typeof(BorrowedBodyTests), nameof(Hittable)));
    }

    public void Dispose() => _harmony.UnpatchAll(_harmony.Id);

    [Theory]
    [InlineData(19, 20, false)]
    [InlineData(20, 20, true)]
    [InlineData(21, 20, true)]
    [InlineData(0, 20, false)]
    [InlineData(1, 1, true)]
    [InlineData(20, 0, false)]
    public void PlayConditionIncludesEqualityAndAlsoRestrictsEveryAutoplayType(int doom, int hp, bool expected)
    {
        var owner = CreateFixture(doom, hp);
        Assert.Equal(expected, IsPlayable(owner.Card));
        foreach (AutoPlayType type in Enum.GetValues<AutoPlayType>())
        {
            Assert.Equal(expected, owner.Card.ShouldPlay(owner.Card, type));
            var other = (PoisonedDraughtCard)RuntimeHelpers.GetUninitializedObject(typeof(PoisonedDraughtCard));
            Assert.True(owner.Card.ShouldPlay(other, type));
        }
    }

    [Fact]
    public async Task EqualityTransfersTheFullUnamplifiedAmountToEveryLiveEnemyAndClearsOnlyOwnersDoom()
    {
        var owner = CreateFixture(20, 20);
        await Play(owner.Card);
        Assert.Equal(2, _applications.Count);
        Assert.Equal(new[] { owner.EnemyA, owner.EnemyB }, _applications.Select(a => a.Target));
        Assert.All(_applications, application => Assert.Equal(20m, application.Amount));
        Assert.Equal(20, owner.EnemyA.GetPowerAmount<DoomPower>());
        Assert.Equal(20, owner.EnemyB.GetPowerAmount<DoomPower>());
        Assert.Equal(0, owner.Creature.GetPowerAmount<DoomPower>());
        Assert.Equal(50, owner.Ally.GetPowerAmount<DoomPower>());
        Assert.Equal(-20m, Assert.Single(_removals));
        Assert.Same(owner.Creature, Assert.Single(_intangibles));
        Assert.False(IsPlayable(owner.Card));
        await Play(owner.Card);
        Assert.Equal(2, _applications.Count);
        Assert.Single(_intangibles);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(-10, 0)]
    public async Task ReactionsDoNotRemoveNewlyGainedDoomOrProduceNegativeStacks(int delta, int expected)
    {
        var owner = CreateFixture(20, 20);
        _selfDelta = delta;
        await Play(owner.Card);
        Assert.Equal(expected, owner.Creature.GetPowerAmount<DoomPower>());
        Assert.All(_applications, application => Assert.Equal(20m, application.Amount));
        Assert.Equal(-(decimal)Math.Min(20, 20 + delta), Assert.Single(_removals));
    }

    [Fact]
    public async Task InvalidConditionAndNoEnemyNeverClearOwnersDoom()
    {
        var invalid = CreateFixture(19, 20);
        await Play(invalid.Card);
        Assert.Equal(19, invalid.Creature.GetPowerAmount<DoomPower>());
        var empty = CreateFixture(20, 20);
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(empty.EnemyA, 0);
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(empty.EnemyB, 0);
        await Play(empty.Card);
        Assert.Equal(20, empty.Creature.GetPowerAmount<DoomPower>());
        Assert.Empty(_applications);
        Assert.Empty(_removals);
        Assert.Empty(_intangibles);
    }

    [Fact]
    public void UpgradeAddsRetainWithoutRemovingExhaust()
    {
        var owner = CreateFixture(20, 20);
        Assert.Equal(CardKeyword.Exhaust, Assert.Single(owner.Card.CanonicalKeywords));
        AccessTools.Field(typeof(CardModel), "_isUpgrading").SetValue(owner.Card, true);
        AccessTools.Method(typeof(BorrowedBodyCard), "OnUpgrade").Invoke(owner.Card, null);
        var keywords = owner.Card.GetKeywordsWithSources(KeywordSources.Local);
        Assert.Contains(CardKeyword.Exhaust, keywords);
        Assert.Contains(CardKeyword.Retain, keywords);
    }

    [Theory]
    [InlineData("zhs", "借尸还魂", "大于等于")]
    [InlineData("eng", "Borrowed Body", "at least")]
    public void BothLanguagesStateTheInclusiveCondition(string language, string title, string condition)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "sts2diy", "localization", language, "cards.json")));
        const string key = "STS2DIY_CARD_BORROWED_BODY_CARD";
        Assert.Equal(title, json.RootElement.GetProperty(key + ".title").GetString());
        Assert.Contains(condition, json.RootElement.GetProperty(key + ".description").GetString());
    }

    private static bool IsPlayable(BorrowedBodyCard card) => (bool)typeof(BorrowedBodyCard)
        .GetProperty("IsPlayable", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(card)!;

    private static Task Play(BorrowedBodyCard card) => (Task)AccessTools.Method(typeof(BorrowedBodyCard), "OnPlay")
        .Invoke(card, [new ThrowingPlayerChoiceContext(), null])!;

    private static IEnumerable<CodeInstruction> ReplaceCommands(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.operand is MethodInfo method && method.DeclaringType == typeof(PowerCmd))
            {
                string replacement = method.Name != "Apply" ? nameof(RemoveDoom)
                    : method.GetGenericArguments().SequenceEqual([typeof(IntangiblePower)])
                        ? nameof(ApplyIntangible) : nameof(ApplyDoom);
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(BorrowedBodyTests), replacement);
            }
            yield return instruction;
        }
    }

    private static bool Hittable(Creature __instance, ref bool __result)
    {
        __result = __instance.IsAlive;
        return false;
    }

    private static Task<IntangiblePower?> ApplyIntangible(PlayerChoiceContext context, Creature target,
        decimal amount, Creature? applier, CardModel? source, bool silent)
    {
        Assert.IsType<BorrowedBodyCard>(source);
        Assert.Same(source.Owner.Creature, target);
        Assert.Same(target, applier);
        Assert.Equal(1m, amount);
        Assert.True(target.IsAlive);
        _intangibles.Add(target);
        return Task.FromResult<IntangiblePower?>(null);
    }

    private static Task<IReadOnlyList<DoomPower>> ApplyDoom(PlayerChoiceContext context, IEnumerable<Creature> targets,
        decimal amount, Creature? applier, CardModel? source, bool silent)
    {
        Assert.IsType<BorrowedBodyCard>(source);
        Assert.Same(source.Owner.Creature, applier);
        List<DoomPower> applied = [];
        foreach (Creature target in targets)
        {
            Assert.Equal(CombatSide.Enemy, target.Side);
            _applications.Add((target, amount));
            applied.Add(AddDoom(target, (int)amount));
        }
        DoomPower own = applier!.GetPower<DoomPower>()!;
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(own, Math.Max(0, own.Amount + _selfDelta));
        return Task.FromResult<IReadOnlyList<DoomPower>>(applied);
    }

    private static Task<int> RemoveDoom(PlayerChoiceContext context, PowerModel power, decimal amount,
        Creature? applier, CardModel? source, bool silent)
    {
        Assert.Same(applier, power.Owner);
        _removals.Add(amount);
        int remaining = power.Amount + (int)amount;
        Assert.True(remaining >= 0);
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(power, remaining);
        if (remaining == 0) ((List<PowerModel>)power.Owner.Powers).Remove(power);
        return Task.FromResult(remaining);
    }

    private static (BorrowedBodyCard Card, Creature Creature, Creature Ally, Creature EnemyA, Creature EnemyB)
        CreateFixture(int doom, int hp)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        Creature creature = CreateCreature(CombatSide.Player, hp);
        AccessTools.Field(typeof(Creature), "<Player>k__BackingField").SetValue(creature, player);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
        AccessTools.Field(typeof(Player), "_runPiles").SetValue(player, Array.Empty<CardPile>());
        AddDoom(creature, doom);
        Creature ally = CreateCreature(CombatSide.Player, 40);
        AddDoom(ally, 50);
        Creature enemyA = CreateCreature(CombatSide.Enemy, 40);
        Creature enemyB = CreateCreature(CombatSide.Enemy, 40);
        var combat = (CombatState)RuntimeHelpers.GetUninitializedObject(typeof(CombatState));
        AccessTools.Field(typeof(CombatState), "_allies").SetValue(combat, new List<Creature> { creature, ally });
        AccessTools.Field(typeof(CombatState), "_enemies").SetValue(combat, new List<Creature> { enemyA, enemyB });
        creature.CombatState = combat;
        var card = (BorrowedBodyCard)RuntimeHelpers.GetUninitializedObject(typeof(BorrowedBodyCard));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(card, true);
        AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
        AccessTools.Field(typeof(CardModel), "_keywords").SetValue(card, new HashSet<CardKeyword>(card.CanonicalKeywords));
        return (card, creature, ally, enemyA, enemyB);
    }

    private static Creature CreateCreature(CombatSide side, int hp)
    {
        var creature = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(creature, hp);
        AccessTools.Field(typeof(Creature), "<Side>k__BackingField").SetValue(creature, side);
        AccessTools.Field(typeof(Creature), "_powers").SetValue(creature, new List<PowerModel>());
        return creature;
    }

    private static DoomPower AddDoom(Creature creature, int amount)
    {
        var doom = (DoomPower)RuntimeHelpers.GetUninitializedObject(typeof(DoomPower));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(doom, true);
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(doom, amount);
        AccessTools.Field(typeof(PowerModel), "_owner").SetValue(doom, creature);
        ((List<PowerModel>)creature.Powers).Add(doom);
        return doom;
    }
}
