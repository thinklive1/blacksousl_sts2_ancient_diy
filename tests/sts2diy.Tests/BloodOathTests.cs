using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Sts2Diy.Scripts.Cards;
using Sts2Diy.Scripts.Powers;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class BloodOathTests : IDisposable
{
    private readonly Harmony _harmony = new("sts2diy.tests.blood_oath");
    private static List<(Creature Target, decimal Amount, int Pending)> _heals = [];
    private static List<decimal> _blocks = [];
    private static List<(decimal Amount, ValueProp Props)> _hpLosses = [];

    public BloodOathTests()
    {
        _heals = [];
        _blocks = [];
        _hpLosses = [];
        _harmony.Patch(AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Heal)),
            prefix: new HarmonyMethod(typeof(BloodOathTests), nameof(CaptureHeal)));
        _harmony.Patch(AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
                [typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardModel), typeof(CardPlay)]),
            prefix: new HarmonyMethod(typeof(BloodOathTests), nameof(CaptureHpLoss)));
        _harmony.Patch(AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.GainBlock),
                [typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardPlay), typeof(bool)]),
            prefix: new HarmonyMethod(typeof(BloodOathTests), nameof(CaptureBlock)));
    }

    public void Dispose() => _harmony.UnpatchAll(_harmony.Id);

    [Theory]
    [InlineData(0, 20, 0, 0)]
    [InlineData(12, 8, 12, 1)]
    [InlineData(25, 0, 20, 0)]
    public async Task PlayingUsesCurrentDoomWithoutConsumingItOrChangingMaxHp(int doom, int hp, int refund, int blockCalls)
    {
        var owner = CreatePlayer();
        var power = (DoomPower)RuntimeHelpers.GetUninitializedObject(typeof(DoomPower));
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(power, doom);
        ((List<PowerModel>)owner.Creature.Powers).Add(power);
        await (Task)AccessTools.Method(typeof(BloodOathCard), "OnPlay")
            .Invoke(owner.Card, [new ThrowingPlayerChoiceContext(), null])!;
        Assert.Equal(doom, power.Amount);
        Assert.Equal(40, owner.Creature.MaxHp);
        Assert.Equal(hp, owner.Creature.CurrentHp);
        Assert.Equal(refund, owner.Power.Sts2Diy_HpToRestore);
        Assert.Equal(blockCalls, _blocks.Count);
        if (blockCalls > 0) Assert.Equal((decimal)doom, Assert.Single(_blocks));
        if (doom == 0) Assert.Empty(_hpLosses);
        else Assert.Equal(((decimal)doom, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move), Assert.Single(_hpLosses));
    }

    [Fact]
    public async Task MultiplePlaysAccumulateOnlyActualHpLossAndRepayOnce()
    {
        var owner = CreatePlayer();
        var other = CreatePlayer();
        await Record(owner.Power, owner.Creature, owner.Card, 12);
        await Record(owner.Power, owner.Creature, owner.Card, 7, 100);
        await Record(owner.Power, owner.Creature, owner.Card, 0);
        await Record(owner.Power, owner.Creature, other.Card, 10);
        await Record(owner.Power, other.Creature, owner.Card, 10);
        await Record(owner.Power, owner.Creature, other.Power, 10);
        Assert.Equal(19, owner.Power.DisplayAmount);
        Assert.Equal(0, other.Power.DisplayAmount);

        await owner.Power.AfterCombatEnd(null!);
        Assert.Equal((owner.Creature, 19m, 0), Assert.Single(_heals));
        Assert.Equal(0, owner.Power.Sts2Diy_HpToRestore);
        await owner.Power.AfterCombatEnd(null!);
        Assert.Single(_heals);
    }

    [Fact]
    public async Task DeathDoesNotTurnRecoveryIntoResurrection()
    {
        var owner = CreatePlayer();
        await Record(owner.Power, owner.Creature, owner.Card, 10);
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(owner.Creature, 0);
        await owner.Power.AfterCombatEnd(null!);
        Assert.Empty(_heals);
        Assert.Equal(0, owner.Power.Sts2Diy_HpToRestore);
    }

    [Fact]
    public async Task SavedDebtIsPerPlayerAndRestoredDebtCanBeRepaid()
    {
        Assert.NotNull(typeof(BloodOathRecoveryPower).GetProperty("Sts2Diy_HpToRestore")!
            .GetCustomAttribute<SavedPropertyAttribute>());
        var owner = CreatePlayer();
        var restored = CreatePlayer();
        await Record(owner.Power, owner.Creature, owner.Card, 13);
        restored.Power.Sts2Diy_HpToRestore = owner.Power.Sts2Diy_HpToRestore;
        await restored.Power.AfterCombatEnd(null!);
        Assert.Same(restored.Creature, Assert.Single(_heals).Target);
        Assert.Equal(13m, _heals[0].Amount);
        Assert.Equal(13, owner.Power.DisplayAmount);
    }

    [Fact]
    public void PlayInstallsTheLedgerBeforeBloodlettingStyleHpLossAndGivesUnpoweredBlock()
    {
        MethodInfo play = AccessTools.Method(typeof(BloodOathCard), "OnPlay");
        var body = PatchProcessor.GetOriginalInstructions(AsyncBody(play)).ToList();
        int ledger = body.FindIndex(i => i.operand is MethodInfo m && m.Name == "Apply"
            && m.IsGenericMethod && m.GetGenericArguments().SequenceEqual([typeof(BloodOathRecoveryPower)]));
        int damage = body.FindIndex(i => i.operand is MethodInfo m && m.DeclaringType == typeof(CreatureCmd)
            && m.Name == nameof(CreatureCmd.Damage));
        int block = body.FindIndex(i => i.operand is MethodInfo m && m.DeclaringType == typeof(CreatureCmd)
            && m.Name == nameof(CreatureCmd.GainBlock));
        Assert.True(ledger >= 0 && damage > ledger && block > damage);
        Assert.Contains(body, i => i.operand is MethodInfo m && m.Name == "get_IsAlive");
        Assert.DoesNotContain(body, i => i.operand is MethodInfo m
            && m.Name is "SetCurrentHp" or "LoseMaxHp" or "ModifyAmount");
        Assert.Contains(body, i => i.LoadsConstant((int)(ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move)));
        Assert.Contains(body, i => i.LoadsConstant((int)ValueProp.Unpowered));
        var upgrade = PatchProcessor.GetOriginalInstructions(AccessTools.Method(typeof(BloodOathCard), "OnUpgrade"));
        Assert.Contains(upgrade, i => i.operand is MethodInfo m && m.Name == "UpgradeBy");
        Assert.Contains(upgrade, i => i.LoadsConstant(-1));
    }

    [Theory]
    [InlineData("zhs", "血誓")]
    [InlineData("eng", "Blood Oath")]
    public void LocalizedDescriptionsIncludeRecoveryAndItsLiveSavedValue(string language, string name)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        string folder = Path.Combine(root.FullName, "sts2diy", "localization", language);
        using var cards = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "cards.json")));
        using var powers = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "powers.json")));
        Assert.Equal(name, cards.RootElement.GetProperty("STS2DIY_CARD_BLOOD_OATH_CARD.title").GetString());
        Assert.Contains(language == "zhs" ? "战斗结束后恢复" : "At the end of combat, recover",
            cards.RootElement.GetProperty("STS2DIY_CARD_BLOOD_OATH_CARD.description").GetString());
        Assert.Contains("{HpToRestore}", powers.RootElement
            .GetProperty("STS2DIY_POWER_BLOOD_OATH_RECOVERY_POWER.description").GetString());
        // Native dumb tooltips overwrite Amount; the saved debt uses a separate variable.
        var getter = PatchProcessor.GetOriginalInstructions(typeof(BloodOathRecoveryPower).GetProperty("Description")!.GetMethod!);
        Assert.Contains(getter, i => Equals(i.operand, "HpToRestore"));
    }

    private static Task Record(BloodOathRecoveryPower power, Creature receiver, AbstractModel source, int lost, int overkill = 0) =>
        power.AfterDamageReceived(new ThrowingPlayerChoiceContext(), receiver,
            new DamageResult(receiver, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move)
            { UnblockedDamage = lost, OverkillDamage = overkill }, ValueProp.Unpowered, receiver, source as CardModel);

    private static bool CaptureHeal(Creature creature, decimal amount, ref Task __result)
    {
        _heals.Add((creature, amount, creature.GetPower<BloodOathRecoveryPower>()!.Sts2Diy_HpToRestore));
        __result = Task.CompletedTask;
        return false;
    }

    private static bool CaptureHpLoss(PlayerChoiceContext choiceContext, Creature target, decimal amount,
        ValueProp props, CardModel cardSource, ref Task<IEnumerable<DamageResult>> __result)
    {
        _hpLosses.Add((amount, props));
        int lost = Math.Min(target.CurrentHp, (int)amount);
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(target, target.CurrentHp - lost);
        var result = new DamageResult(target, props) { UnblockedDamage = lost, OverkillDamage = (int)amount - lost };
        target.GetPower<BloodOathRecoveryPower>()!
            .AfterDamageReceived(choiceContext, target, result, props, target, cardSource).GetAwaiter().GetResult();
        __result = Task.FromResult<IEnumerable<DamageResult>>([result]);
        return false;
    }

    private static bool CaptureBlock(Creature creature, decimal amount, ValueProp props, ref Task<decimal> __result)
    {
        Assert.Equal(ValueProp.Unpowered, props);
        _blocks.Add(amount);
        __result = Task.FromResult(amount);
        return false;
    }

    private static (Creature Creature, BloodOathCard Card, BloodOathRecoveryPower Power) CreatePlayer()
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var creature = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        AccessTools.Field(typeof(Creature), "<Player>k__BackingField").SetValue(creature, player);
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(creature, 20);
        AccessTools.Field(typeof(Creature), "_maxHp").SetValue(creature, 40);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
        var card = (BloodOathCard)RuntimeHelpers.GetUninitializedObject(typeof(BloodOathCard));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(card, true);
        AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
        var power = (BloodOathRecoveryPower)RuntimeHelpers.GetUninitializedObject(typeof(BloodOathRecoveryPower));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(power, true);
        AccessTools.Field(typeof(PowerModel), "_owner").SetValue(power, creature);
        AccessTools.Field(typeof(Creature), "_powers").SetValue(creature, new List<PowerModel> { power });
        return (creature, card, power);
    }

    private static MethodInfo AsyncBody(MethodInfo method) => method
        .GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType
        .GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic)!;
}
