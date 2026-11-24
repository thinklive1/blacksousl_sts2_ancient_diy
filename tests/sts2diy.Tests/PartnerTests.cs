using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Sts2Diy.Scripts.Cards;
using Sts2Diy.Scripts.Powers;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class PartnerTests : IDisposable
{
    private readonly Harmony _harmony = new("sts2diy.tests.partner");
    private static int _spent;

    public PartnerTests()
    {
        _spent = 0;
        _harmony.Patch(AccessTools.Method(typeof(PowerCmd), nameof(PowerCmd.Decrement), [typeof(PowerModel)]),
            prefix: new HarmonyMethod(typeof(PartnerTests), nameof(Decrement)));
    }

    public void Dispose() => _harmony.UnpatchAll(_harmony.Id);

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(300)]
    public async Task OneHitKeepsOstyDamageButCancelsAllOverflowAndSpendsExactlyOneCharge(int damage)
    {
        var owner = CreatePlayer();
        var context = new ThrowingPlayerChoiceContext();
        await owner.Guard.BeforeDamageReceived(context, owner.Creature, damage, ValueProp.Move, owner.Enemy, null);
        Assert.Equal(damage, owner.Guard.ModifyHpLostAfterOstyLate(owner.Osty, damage, ValueProp.Move, owner.Enemy, null));
        // Native redirection makes Osty lose HP first, so the overflow must still be blocked after he dies.
        SetHp(owner.Osty, 0);
        Assert.Equal(0m, owner.Guard.ModifyHpLostAfterOstyLate(owner.Creature, damage, ValueProp.Move, owner.Enemy, null));
        await owner.Guard.AfterModifyingHpLostAfterOsty();
        await owner.Guard.AfterDamageReceived(context, owner.Creature,
            new DamageResult(owner.Creature, ValueProp.Move), ValueProp.Move, owner.Enemy, null);
        Assert.Equal(1, _spent);
        Assert.Equal(0, owner.Guard.Amount);
        Assert.Equal(damage, owner.Guard.ModifyHpLostAfterOstyLate(owner.Creature, damage, ValueProp.Move, owner.Enemy, null));
    }

    [Fact]
    public async Task SmallOrFullyBlockedHitsAlsoConsumeTheProtectionOnlyOnce()
    {
        var owner = CreatePlayer();
        var context = new ThrowingPlayerChoiceContext();
        await owner.Guard.BeforeDamageReceived(context, owner.Creature, 2m, ValueProp.Move, owner.Enemy, null);
        Assert.Equal(0m, owner.Guard.ModifyHpLostAfterOstyLate(owner.Creature, 0m, ValueProp.Move, owner.Enemy, null));
        await owner.Guard.AfterDamageReceived(context, owner.Osty,
            new DamageResult(owner.Osty, ValueProp.Move), ValueProp.Move, owner.Enemy, null);
        Assert.Equal(0, _spent);
        await owner.Guard.AfterDamageReceived(context, owner.Creature,
            new DamageResult(owner.Creature, ValueProp.Move) { WasFullyBlocked = true }, ValueProp.Move, owner.Enemy, null);
        Assert.Equal(1, _spent);
    }

    [Fact]
    public async Task AnotherPlayerHpLossZeroDamageAndAMissingOstyCannotSpendProtection()
    {
        var owner = CreatePlayer();
        var other = CreatePlayer();
        var context = new ThrowingPlayerChoiceContext();
        await owner.Guard.BeforeDamageReceived(context, other.Creature, 40, ValueProp.Move, owner.Enemy, null);
        await owner.Guard.BeforeDamageReceived(context, owner.Creature, 40, ValueProp.Unpowered, owner.Enemy, null);
        await owner.Guard.BeforeDamageReceived(context, owner.Creature, 0, ValueProp.Move, owner.Enemy, null);
        SetHp(owner.Osty, 0);
        await owner.Guard.BeforeDamageReceived(context, owner.Creature, 40, ValueProp.Move, owner.Enemy, null);
        Assert.Equal(40m, owner.Guard.ModifyHpLostAfterOstyLate(owner.Creature, 40, ValueProp.Move, owner.Enemy, null));
        await owner.Guard.AfterDamageReceived(context, owner.Creature,
            new DamageResult(owner.Creature, ValueProp.Move), ValueProp.Move, owner.Enemy, null);
        Assert.Equal(0, _spent);
        Assert.Equal(1, owner.Guard.Amount);
    }

    [Fact]
    public async Task ReservedHitDoesNotProtectAnotherPlayerOrUnrelatedDamage()
    {
        var owner = CreatePlayer();
        var other = CreatePlayer();
        await owner.Guard.BeforeDamageReceived(new ThrowingPlayerChoiceContext(), owner.Creature, 20,
            ValueProp.Move, owner.Enemy, null);
        Assert.Equal(20m, owner.Guard.ModifyHpLostAfterOstyLate(other.Creature, 20, ValueProp.Move, owner.Enemy, null));
        Assert.Equal(20m, owner.Guard.ModifyHpLostAfterOstyLate(owner.Creature, 20, ValueProp.Unpowered, owner.Enemy, null));
        Assert.Equal(20m, owner.Guard.ModifyHpLostAfterOstyLate(owner.Creature, 20, ValueProp.Move, other.Enemy, null));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task SummonLockIsImmediateAndCountsTheApplicationTurn(int turns)
    {
        var owner = CreatePlayer();
        var other = CreatePlayer();
        SetAmount(owner.Lock, turns);
        var context = new ThrowingPlayerChoiceContext();
        Assert.Equal(0m, owner.Lock.ModifySummonAmount(owner.Player, 10m, null));
        Assert.Equal(10m, owner.Lock.ModifySummonAmount(other.Player, 10m, null));
        for (int i = turns; i > 0; i--)
        {
            await owner.Lock.AfterPlayerTurnStart(context, other.Player);
            await owner.Lock.AfterSideTurnEnd(context, CombatSide.Player, [other.Creature]);
            Assert.Equal(i, owner.Lock.Amount);
            await owner.Lock.AfterPlayerTurnStart(context, owner.Player);
            await owner.Lock.AfterSideTurnEnd(context, CombatSide.Enemy, [owner.Enemy]);
            Assert.Equal(0m, owner.Lock.ModifySummonAmount(owner.Player, 8m, owner.Card));
            await owner.Lock.AfterSideTurnEnd(context, CombatSide.Player, [owner.Creature]);
            Assert.Equal(i - 1, owner.Lock.Amount);
        }
        Assert.Equal(8m, owner.Lock.ModifySummonAmount(owner.Player, 8m, owner.Card));
    }

    [Fact]
    public async Task MultipleProtectionChargesAreSpentOnSeparateHitsWithoutDoubleSpending()
    {
        var owner = CreatePlayer();
        SetAmount(owner.Guard, 2);
        var context = new ThrowingPlayerChoiceContext();
        for (int hit = 0; hit < 2; hit++)
        {
            await owner.Guard.BeforeDamageReceived(context, owner.Creature, 30, ValueProp.Move, owner.Enemy, null);
            Assert.Equal(0m, owner.Guard.ModifyHpLostAfterOstyLate(owner.Creature, 20, ValueProp.Move, owner.Enemy, null));
            await owner.Guard.AfterModifyingHpLostAfterOsty();
            await owner.Guard.AfterModifyingHpLostAfterOsty();
            await owner.Guard.AfterDamageReceived(context, owner.Creature,
                new DamageResult(owner.Creature, ValueProp.Move), ValueProp.Move, owner.Enemy, null);
            Assert.Equal(hit + 1, _spent);
        }
        Assert.Equal(0, owner.Guard.Amount);
    }

    [Fact]
    public async Task RestoredRemainingAmountCountsDownWithoutAnExtraPhaseFlag()
    {
        var owner = CreatePlayer();
        var restored = CreatePlayer();
        var context = new ThrowingPlayerChoiceContext();
        SetAmount(restored.Lock, owner.Lock.Amount);
        Assert.Equal(0m, restored.Lock.ModifySummonAmount(restored.Player, 8, null));
        await restored.Lock.AfterSideTurnEnd(context, CombatSide.Player, []);
        Assert.Equal(2, restored.Lock.Amount);
        await restored.Lock.AfterSideTurnEnd(context, CombatSide.Player, [restored.Creature]);
        Assert.Equal(1, restored.Lock.Amount);
        await restored.Lock.AfterSideTurnEnd(context, CombatSide.Player, [restored.Creature]);
        Assert.Equal(0, restored.Lock.Amount);
        Assert.Equal(2, owner.Lock.Amount);
        await restored.Lock.AfterSideTurnEnd(context, CombatSide.Player, [restored.Creature]);
        Assert.Equal(0, restored.Lock.Amount);
    }

    [Fact]
    public void CardRequiresALivingOstyAndUpgradeOnlyShortensThePenalty()
    {
        var owner = CreatePlayer();
        Assert.True(owner.Card.ShouldPlay(owner.Card, AutoPlayType.Default));
        Assert.Equal(2m, owner.Card.DynamicVars["Turns"].BaseValue);
        var constructor = PatchProcessor.GetOriginalInstructions(typeof(PartnerCard).GetConstructor(Type.EmptyTypes)!);
        Assert.Contains(constructor, instruction => instruction.LoadsConstant((int)CardType.Skill));
        Assert.Contains(constructor, instruction => instruction.LoadsConstant((int)CardRarity.Uncommon));
        Assert.Equal(1, owner.Card.EnergyCost.Canonical);
        typeof(PartnerCard).GetMethod("OnUpgrade", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(owner.Card, null);
        Assert.Equal(1m, owner.Card.DynamicVars["Turns"].BaseValue);
        SetHp(owner.Osty, 0);
        Assert.False(owner.Card.ShouldPlay(owner.Card, AutoPlayType.Default));
    }

    [Fact]
    public void GuardModifiersAreReadOnly()
    {
        var owner = CreatePlayer();
        Assert.Equal(20m, owner.Guard.ModifyHpLostAfterOstyLate(owner.Creature, 20, ValueProp.Move, owner.Enemy, null));
        Assert.Equal(20m, owner.Guard.ModifyHpLostAfterOstyLate(owner.Creature, 20, ValueProp.Move, owner.Enemy, null));
        Assert.Equal(1, owner.Guard.Amount);
        Assert.Equal(0, _spent);
    }

    [Theory]
    [InlineData("zhs")]
    [InlineData("eng")]
    public void CardAndBothStatusesHaveLocalizedDescriptions(string language)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        string dir = Path.Combine(root.FullName, "sts2diy", "localization", language);
        using var cards = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "cards.json")));
        Assert.Contains("{Turns:diff()}", cards.RootElement.GetProperty("STS2DIY_CARD_PARTNER_CARD.description").GetString());
        using var powers = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "powers.json")));
        foreach (string key in new[] { "STS2DIY_POWER_PARTNER_GUARD_POWER", "STS2DIY_POWER_NO_SUMMON_POWER" })
        {
            Assert.False(string.IsNullOrWhiteSpace(powers.RootElement.GetProperty(key + ".title").GetString()));
            Assert.Contains("{Amount}", powers.RootElement.GetProperty(key + ".description").GetString());
        }
    }

    private static bool Decrement(PowerModel power, ref Task __result)
    {
        SetAmount(power, Math.Max(0, power.Amount - 1));
        _spent++;
        __result = Task.CompletedTask;
        return false;
    }

    private static void SetAmount(PowerModel power, int amount) =>
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(power, amount);
    private static void SetHp(Creature creature, int hp) =>
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(creature, hp);

    private static T Power<T>(Creature owner, int amount) where T : PowerModel
    {
        var power = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(power, true);
        AccessTools.Field(typeof(PowerModel), "_owner").SetValue(power, owner);
        SetAmount(power, amount);
        return power;
    }

    private static (Player Player, Creature Creature, Creature Osty, Creature Enemy,
        PartnerGuardPower Guard, NoSummonPower Lock, PartnerCard Card) CreatePlayer()
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var creature = Creature(CombatSide.Player);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
        AccessTools.Field(typeof(Creature), "<Player>k__BackingField").SetValue(creature, player);
        var osty = Creature(CombatSide.Player);
        osty.PetOwner = player;
        AccessTools.Field(typeof(Creature), "<Monster>k__BackingField").SetValue(osty,
            RuntimeHelpers.GetUninitializedObject(typeof(Osty)));
        var combat = (PlayerCombatState)RuntimeHelpers.GetUninitializedObject(typeof(PlayerCombatState));
        AccessTools.Field(typeof(PlayerCombatState), "_pets").SetValue(combat, new List<Creature> { osty });
        AccessTools.Property(typeof(Player), "PlayerCombatState").SetValue(player, combat);
        var card = (PartnerCard)RuntimeHelpers.GetUninitializedObject(typeof(PartnerCard));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(card, true);
        AccessTools.Field(typeof(CardModel), "_owner").SetValue(card, player);
        AccessTools.Field(typeof(CardModel), "_energyCost").SetValue(card, new CardEnergyCost(card, 1, false));
        var vars = new DynamicVarSet((IEnumerable<DynamicVar>)typeof(PartnerCard)
            .GetProperty("CanonicalVars", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(card)!);
        AccessTools.Field(typeof(CardModel), "_dynamicVars").SetValue(card, vars);
        vars.InitializeWithOwner(card);
        return (player, creature, osty, Creature(CombatSide.Enemy),
            Power<PartnerGuardPower>(creature, 1), Power<NoSummonPower>(creature, 2), card);
    }

    private static Creature Creature(CombatSide side)
    {
        var creature = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        AccessTools.Field(typeof(Creature), "<Side>k__BackingField").SetValue(creature, side);
        AccessTools.Field(typeof(Creature), "_powers").SetValue(creature, new List<PowerModel>());
        SetHp(creature, 10);
        return creature;
    }
}
