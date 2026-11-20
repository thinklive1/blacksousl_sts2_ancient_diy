using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Sts2Diy.Scripts.Powers;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class BitterHatredRageTests : IDisposable
{
    private readonly Harmony _harmony = new("sts2diy.tests.hatred_rage");
    private static int _applications;

    public BitterHatredRageTests()
    {
        _applications = 0;
        MethodInfo sync = AccessTools.Method(typeof(BitterHatredPower), "SyncRageDisplay");
        Type machine = sync.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        // Replace command boundaries rather than patching CLR-shared generic Apply<T> bodies.
        _harmony.Patch(AccessTools.Method(machine, "MoveNext"),
            transpiler: new HarmonyMethod(typeof(BitterHatredRageTests), nameof(ReplaceCommands)));
        _harmony.Patch(AccessTools.Method(typeof(BitterHatredPower), "AfterRemoved"),
            transpiler: new HarmonyMethod(typeof(BitterHatredRageTests), nameof(ReplaceCommands)));
    }

    public void Dispose() => _harmony.UnpatchAll(_harmony.Id);

    [Theory]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(125)]
    public async Task ActivationShowsTheBonusOnOnlyTheOwnersOstyWithoutMultiplyingDamageTwice(int bonus)
    {
        var owner = CreatePlayer(bonus);
        var other = CreatePlayer(50);
        var context = new ThrowingPlayerChoiceContext();
        Assert.Same(owner.Osty, owner.Player.Osty);
        Assert.True(owner.Osty.IsAlive);
        Assert.Equal(bonus, owner.Power.Amount);
        await owner.Power.AfterDeath(context, owner.Osty, null, false, 0);
        Assert.True(owner.Power.Sts2Diy_Pending);
        Assert.Null(owner.Osty.GetPower<BitterHatredRagePower>());
        await owner.Power.AfterPlayerTurnStart(context, other.Player);
        Assert.False(owner.Power.Sts2Diy_Active);
        await owner.Power.AfterPlayerTurnStart(context, owner.Player);
        Assert.True(owner.Power.Sts2Diy_Active);
        Assert.Equal(1, _applications);
        Assert.Equal(bonus, owner.Osty.GetPower<BitterHatredRagePower>()!.Amount);
        Assert.Null(other.Osty.GetPower<BitterHatredRagePower>());
        decimal multiplier = owner.Power.ModifyDamageMultiplicative(null, 10, ValueProp.Move, owner.Osty, null, null);
        Assert.Equal(1m + bonus / 100m, multiplier);
        Assert.Null(typeof(BitterHatredRagePower).GetMethod("ModifyDamageMultiplicative",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
    }

    [Fact]
    public async Task SummoningAfterActivationRestoresTheMarkerAndRepeatedSummonsDoNotStackIt()
    {
        var owner = CreatePlayer(75);
        var other = CreatePlayer(50);
        var context = new ThrowingPlayerChoiceContext();
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(owner.Osty, 0);
        owner.Power.Sts2Diy_Pending = true;
        await owner.Power.AfterPlayerTurnStart(context, owner.Player);
        Assert.True(owner.Power.Sts2Diy_Active);
        Assert.Null(owner.Osty.GetPower<BitterHatredRagePower>());
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(owner.Osty, 10);
        await owner.Power.AfterSummon(context, other.Player, 2);
        Assert.Null(owner.Osty.GetPower<BitterHatredRagePower>());
        await owner.Power.AfterSummon(context, owner.Player, 2);
        await owner.Power.AfterSummon(context, owner.Player, 2);
        Assert.Equal(75, owner.Osty.GetPower<BitterHatredRagePower>()!.Amount);
        Assert.Equal(1, _applications);
    }

    [Fact]
    public async Task MarkerUpdatesWhenTheActiveAbilityStacksAndOnlyExpiresWithItsOwnersTurn()
    {
        var owner = CreatePlayer(50);
        var other = CreatePlayer(50);
        var context = new ThrowingPlayerChoiceContext();
        owner.Power.Sts2Diy_Pending = true;
        await owner.Power.AfterPlayerTurnStart(context, owner.Player);
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(owner.Power, 125);
        await owner.Power.AfterPowerAmountChanged(context, owner.Power, 75, owner.Creature, null);
        Assert.Equal(125, owner.Osty.GetPower<BitterHatredRagePower>()!.Amount);
        await owner.Power.AfterSideTurnEnd(context, CombatSide.Player, [other.Creature]);
        await owner.Power.AfterSideTurnEnd(context, CombatSide.Enemy, [owner.Creature]);
        Assert.True(owner.Power.Sts2Diy_Active);
        Assert.NotNull(owner.Osty.GetPower<BitterHatredRagePower>());
        await owner.Power.AfterSideTurnEnd(context, CombatSide.Player, [owner.Creature]);
        Assert.False(owner.Power.Sts2Diy_Active);
        Assert.Null(owner.Osty.GetPower<BitterHatredRagePower>());
    }

    [Fact]
    public async Task RemovingTheAbilityAlsoRemovesItsMarker()
    {
        var owner = CreatePlayer(50);
        owner.Power.Sts2Diy_Pending = true;
        await owner.Power.AfterPlayerTurnStart(new ThrowingPlayerChoiceContext(), owner.Player);
        await owner.Power.AfterRemoved(owner.Creature);
        Assert.Null(owner.Osty.GetPower<BitterHatredRagePower>());
    }

    [Fact]
    public void MarkerUsesBothOriginalRageIcons()
    {
        var getter = PatchProcessor.GetOriginalInstructions(typeof(BitterHatredRagePower).GetProperty("AssetProfile")!.GetMethod!);
        Assert.Contains(getter, i => i.operand is MethodInfo m && m.DeclaringType == typeof(ModelDb)
            && m.IsGenericMethod && m.GetGenericArguments().SequenceEqual([typeof(RagePower)]));
        Assert.Contains(getter, i => i.operand is MethodInfo m && m.Name == "get_IconPath");
        Assert.Contains(getter, i => i.operand is MethodInfo m && m.Name == "get_ResolvedBigIconPath");
    }

    [Theory]
    [InlineData("zhs", "愤怒")]
    [InlineData("eng", "Rage")]
    public void MarkerHasLocalizedTitleAndLivePercentage(string language, string title)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        using JsonDocument json = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,
            "sts2diy", "localization", language, "powers.json")));
        const string key = "STS2DIY_POWER_BITTER_HATRED_RAGE_POWER";
        Assert.Equal(title, json.RootElement.GetProperty(key + ".title").GetString());
        Assert.Contains("{Amount}", json.RootElement.GetProperty(key + ".description").GetString());
    }

    private static IEnumerable<CodeInstruction> ReplaceCommands(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.operand is MethodInfo method && method.DeclaringType == typeof(PowerCmd))
            {
                string replacement = method.Name == "Apply" ? nameof(ApplyMarker) : nameof(RemoveMarker);
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(BitterHatredRageTests), replacement);
            }
            yield return instruction;
        }
    }

    private static Task<BitterHatredRagePower?> ApplyMarker(PlayerChoiceContext context, Creature target,
        decimal amount, Creature? applier, CardModel? cardSource, bool silent)
    {
        var marker = target.GetPower<BitterHatredRagePower>();
        if (marker == null)
        {
            marker = CreatePower<BitterHatredRagePower>(target);
            ((List<PowerModel>)target.Powers).Add(marker);
        }
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(marker, marker.Amount + (int)amount);
        _applications++;
        return Task.FromResult<BitterHatredRagePower?>(marker);
    }

    private static Task RemoveMarker(PowerModel? power)
    {
        if (power is BitterHatredRagePower) ((List<PowerModel>)power.Owner.Powers).Remove(power);
        return Task.CompletedTask;
    }

    private static (Player Player, Creature Creature, Creature Osty, BitterHatredPower Power) CreatePlayer(int bonus)
    {
        // Minimal managed fixtures avoid Godot and native power VFX; commands are intercepted above.
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        Creature creature = CreateCreature();
        AccessTools.Field(typeof(Creature), "<Player>k__BackingField").SetValue(creature, player);
        Creature osty = CreateCreature();
        AccessTools.Field(typeof(Creature), "<Monster>k__BackingField").SetValue(osty,
            RuntimeHelpers.GetUninitializedObject(typeof(Osty)));
        var combat = (PlayerCombatState)RuntimeHelpers.GetUninitializedObject(typeof(PlayerCombatState));
        AccessTools.Field(typeof(PlayerCombatState), "_pets").SetValue(combat, new List<Creature> { osty });
        AccessTools.Property(typeof(Player), "PlayerCombatState").SetValue(player, combat);
        var power = CreatePower<BitterHatredPower>(creature);
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(power, bonus);
        return (player, creature, osty, power);
    }

    private static Creature CreateCreature()
    {
        var creature = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(creature, 10);
        AccessTools.Field(typeof(Creature), "_maxHp").SetValue(creature, 10);
        AccessTools.Field(typeof(Creature), "<Side>k__BackingField").SetValue(creature, CombatSide.Player);
        AccessTools.Field(typeof(Creature), "_powers").SetValue(creature, new List<PowerModel>());
        return creature;
    }

    private static T CreatePower<T>(Creature owner) where T : PowerModel
    {
        var power = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(power, true);
        AccessTools.Field(typeof(PowerModel), "_owner").SetValue(power, owner);
        return power;
    }
}
