using System.Reflection;
using System.Runtime.CompilerServices;
using BlackSouls.Scripts;
using BlackSouls.Scripts.Cards;
using BlackSouls.Scripts.Services;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace BsAncient.Tests;

public sealed class CombatGuardRegressionTests
{
    [Fact]
    public void UnicornReflectionIsNotAnAttackAndDoesNotSuppressDamageHooks()
    {
        Assert.False(UnicornRoyalCrestRelic.CounterattackProps.IsPoweredAttack());
        Assert.False(UnicornRoyalCrestRelic.CounterattackProps.HasFlag(ValueProp.Move));
        foreach (string patch in new[]
        {
            "UnicornHookBeforeDamageReceivedPatch",
            "UnicornHookAfterDamageReceivedPatch",
            "UnicornThornsBeforeDamageReceivedPatch",
            "UnicornPersonalHiveAfterDamageReceivedPatch"
        })
        {
            Assert.Null(typeof(Entry).Assembly.GetType("BlackSouls.Scripts." + patch));
        }
    }

    [Fact]
    public void NestedImmediatePlayDoesNotRemoveOuterGuard()
    {
        LastAceCard card = new();
        using (JudgmentOrderCombatService.AllowImmediatePlay(card))
        {
            using (JudgmentOrderCombatService.AllowImmediatePlay(card))
            {
                Assert.True(JudgmentOrderCombatService.IsImmediatePlay(card));
            }

            Assert.True(JudgmentOrderCombatService.IsImmediatePlay(card));
        }

        Assert.False(JudgmentOrderCombatService.IsImmediatePlay(card));
    }

    [Fact]
    public void DisposedImmediatePlayCannotClearANewerGuard()
    {
        LastAceCard card = new();
        IDisposable previous = JudgmentOrderCombatService.AllowImmediatePlay(card);
        previous.Dispose();

        using (JudgmentOrderCombatService.AllowImmediatePlay(card))
        {
            previous.Dispose();
            Assert.True(JudgmentOrderCombatService.IsImmediatePlay(card));
        }
    }

    [Fact]
    public void ImmediatePlayGuardIsClearedAfterAnException()
    {
        LastAceCard card = new();
        Action failInsideGuard = () =>
        {
            using (JudgmentOrderCombatService.AllowImmediatePlay(card))
            {
                throw new InvalidOperationException();
            }
        };
        Assert.Throws<InvalidOperationException>(failInsideGuard);
        Assert.False(JudgmentOrderCombatService.IsImmediatePlay(card));
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(0, false)]
    [InlineData(0, true)]
    public void SleepRefreshPreservesOriginalAmountAndAwakening(int remaining, bool awakened)
    {
        SleepEnchantment sleep = ModelStateIsolationTests.CreateMutable<SleepEnchantment>();
        sleep.BlackSouls_OriginalAmount = 5;
        sleep.BlackSouls_Awakened = awakened;
        sleep.Amount = remaining;

        typeof(SleepEnchantment).GetMethod("OnEnchant", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(sleep, null);

        Assert.Equal(5, sleep.BlackSouls_OriginalAmount);
        Assert.Equal(awakened, sleep.BlackSouls_Awakened);
        Assert.Equal(remaining, sleep.Amount);
        Assert.Equal(5m, sleep.DynamicVars["OriginalAmount"].BaseValue);
        Assert.Equal(6m, sleep.DynamicVars["PlayableDraw"].BaseValue);
    }

    [Fact]
    public async Task SleepBlocksTheFirstDrawsAndWakesOnTheNextDraw()
    {
        LastAceCard card = ModelStateIsolationTests.CreateMutable<LastAceCard>();
        SleepEnchantment sleep = ModelStateIsolationTests.CreateMutable<SleepEnchantment>();
        sleep.ApplyInternal(card, 2);
        typeof(SleepEnchantment).GetMethod("OnEnchant", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(sleep, null);
        ThrowingPlayerChoiceContext context = new();

        await sleep.AfterCardDrawn(context, card, true);
        Assert.False(sleep.BlackSouls_Awakened);
        await sleep.AfterCardDrawn(context, card, true);
        Assert.Equal(0, sleep.Amount);
        Assert.False(sleep.BlackSouls_Awakened);
        await sleep.AfterCardDrawn(context, card, true);
        Assert.True(sleep.BlackSouls_Awakened);
        Assert.Equal(3, sleep.ModifyCardPlayCount(card, null, 1));
    }

    [Fact]
    public async Task ArmamentResetsBeforeOwnersHandDrawOnly()
    {
        LastAceCard card = ModelStateIsolationTests.CreateMutable<LastAceCard>();
        Player owner = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        Player other = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        card.Owner = owner;
        ArmamentEnchantment armament = ModelStateIsolationTests.CreateMutable<ArmamentEnchantment>();
        armament.ApplyInternal(card, 1);
        FieldInfo used = typeof(ArmamentEnchantment).GetField("_autoPlayedThisTurn", BindingFlags.Instance | BindingFlags.NonPublic)!;
        used.SetValue(armament, true);
        ThrowingPlayerChoiceContext context = new();

        await armament.BeforeHandDraw(other, context, null!);
        Assert.True((bool)used.GetValue(armament)!);
        await armament.BeforeHandDraw(owner, context, null!);
        Assert.False((bool)used.GetValue(armament)!);
    }

    [Fact]
    public void ArmamentCopyDoesNotInheritAnActiveAutoPlayGuard()
    {
        ArmamentEnchantment original = ModelStateIsolationTests.CreateMutable<ArmamentEnchantment>();
        FieldInfo active = typeof(ArmamentEnchantment).GetField("_isAutoPlaying", BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo used = typeof(ArmamentEnchantment).GetField("_autoPlayedThisTurn", BindingFlags.Instance | BindingFlags.NonPublic)!;
        active.SetValue(original, true);
        used.SetValue(original, true);

        ArmamentEnchantment copy = (ArmamentEnchantment)original.MutableClone();
        Assert.False((bool)active.GetValue(copy)!);
        Assert.False((bool)used.GetValue(copy)!);
        Assert.True((bool)active.GetValue(original)!);
        Assert.True((bool)used.GetValue(original)!);
    }

    [Fact]
    public void CroquetRejectsAdditionalPlayConditionsConsistently()
    {
        StrikeIronclad strike = new();
        GrandFinale finale = new();
        for (int i = 0; i < 2; i++)
        {
            Assert.False(CroquetMalletCombatService.HasAdditionalPlayCondition(strike));
            Assert.True(CroquetMalletCombatService.HasAdditionalPlayCondition(finale));
        }
    }
}
