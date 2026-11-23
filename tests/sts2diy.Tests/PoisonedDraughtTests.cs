using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Sts2Diy.Scripts.Cards;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class PoisonedDraughtTests : IDisposable
{
    private readonly Harmony _harmony = new("sts2diy.tests.poisoned_draught");
    private static List<string> _order = [];
    private static Player? _drawer;
    private static decimal _drawCount;

    public PoisonedDraughtTests()
    {
        _order = [];
        _drawer = null;
        _drawCount = 0;
        MethodInfo play = AccessTools.Method(typeof(PoisonedDraughtCard), "OnPlay");
        Type machine = play.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        _harmony.Patch(AccessTools.Method(machine, "MoveNext"),
            transpiler: new HarmonyMethod(typeof(PoisonedDraughtTests), nameof(ReplaceCommands)));
    }

    public void Dispose() => _harmony.UnpatchAll(_harmony.Id);

    [Theory]
    [InlineData(false, 6)]
    [InlineData(true, 3)]
    public async Task AddsUnamplifiedDoomOnlyToTheOwnerThenDrawsTwoCards(bool upgraded, int doom)
    {
        var owner = CreateCard();
        var other = CreateCard();
        if (upgraded) AccessTools.Method(typeof(PoisonedDraughtCard), "OnUpgrade").Invoke(owner.Card, null);
        Assert.Equal(CardKeyword.Exhaust, Assert.Single(owner.Card.CanonicalKeywords));
        await (Task)AccessTools.Method(typeof(PoisonedDraughtCard), "OnPlay")
            .Invoke(owner.Card, [new ThrowingPlayerChoiceContext(), null])!;
        Assert.Equal(doom, owner.Creature.GetPowerAmount<DoomPower>());
        Assert.Equal(0, other.Creature.GetPowerAmount<DoomPower>());
        Assert.Equal(new[] { "doom", "draw" }, _order);
        Assert.Same(owner.Player, _drawer);
        Assert.Equal(2m, _drawCount);
    }

    [Theory]
    [InlineData("zhs", "饮鸩")]
    [InlineData("eng", "Poisoned Draught")]
    public void LocalizationUsesBothLiveVariablesWithoutHardcodingUpgradeValues(string language, string title)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "sts2diy", "localization", language, "cards.json")));
        const string key = "STS2DIY_CARD_POISONED_DRAUGHT_CARD";
        Assert.Equal(title, json.RootElement.GetProperty(key + ".title").GetString());
        string text = json.RootElement.GetProperty(key + ".description").GetString()!;
        Assert.Contains("{DoomPower:diff()}", text);
        Assert.Contains("{Cards:diff()}", text);
    }

    private static IEnumerable<CodeInstruction> ReplaceCommands(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.operand is MethodInfo method && method.DeclaringType == typeof(PowerCmd)
                && method.Name == "Apply" && method.IsGenericMethod
                && method.GetGenericArguments().SequenceEqual([typeof(DoomPower)]))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(PoisonedDraughtTests), nameof(ApplyDoom));
            }
            else if (instruction.operand is MethodInfo draw && draw.DeclaringType == typeof(CardPileCmd)
                && draw.Name == "Draw")
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(PoisonedDraughtTests), nameof(Draw));
            }
            yield return instruction;
        }
    }

    private static Task<DoomPower?> ApplyDoom(PlayerChoiceContext context, Creature target, decimal amount,
        Creature? applier, CardModel? source, bool silent)
    {
        Assert.Same(applier, target);
        Assert.IsType<PoisonedDraughtCard>(source);
        Assert.Same(source.Owner.Creature, target);
        var doom = (DoomPower)RuntimeHelpers.GetUninitializedObject(typeof(DoomPower));
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(doom, (int)amount);
        ((List<PowerModel>)target.Powers).Add(doom);
        _order.Add("doom");
        return Task.FromResult<DoomPower?>(doom);
    }

    private static Task<IEnumerable<CardModel>> Draw(PlayerChoiceContext context, decimal count, Player player, bool fromHandDraw)
    {
        Assert.False(fromHandDraw);
        _order.Add("draw");
        _drawer = player;
        _drawCount = count;
        return Task.FromResult<IEnumerable<CardModel>>([]);
    }

    private static (PoisonedDraughtCard Card, Creature Creature, Player Player) CreateCard()
    {
        var card = (PoisonedDraughtCard)RuntimeHelpers.GetUninitializedObject(typeof(PoisonedDraughtCard));
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var creature = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(card, true);
        AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
        AccessTools.Field(typeof(Creature), "<Player>k__BackingField").SetValue(creature, player);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(creature, 40);
        AccessTools.Field(typeof(Creature), "_powers").SetValue(creature, new List<PowerModel>());
        var vars = (IEnumerable<DynamicVar>)typeof(PoisonedDraughtCard)
            .GetProperty("CanonicalVars", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(card)!;
        var set = new DynamicVarSet(vars);
        AccessTools.Field(typeof(CardModel), "_dynamicVars").SetValue(card, set);
        set.InitializeWithOwner(card);
        return (card, creature, player);
    }
}
