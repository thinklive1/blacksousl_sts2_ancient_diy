using BlackSouls.Scripts.Cards;
using BlackSouls.Scripts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BsAncient.Tests;

public sealed class NumberedOrderTests
{
    [Theory]
    [InlineData(42, 0)]
    [InlineData(43, 1)]
    [InlineData(44, 2)]
    [InlineData(100, 58)]
    public void CostTracksNumber(int number, int expected) =>
        Assert.Equal(expected, NumberedOrderCard.GetOrderCost(number));

    [Fact]
    public void OnlyPlayedOrdersAdvanceAndOnlyOnce()
    {
        var card = ModelStateIsolationTests.CreateMutable<NumberedOrderCard>();
        Assert.False(card.AdvanceIfPending());
        card.BlackSouls_PendingAdvance = true;
        Assert.True(card.AdvanceIfPending());
        Assert.Equal(43, card.BlackSouls_OrderNumber);
        Assert.False(card.AdvanceIfPending());
        card.BlackSouls_PendingAdvance = true;
        Assert.True(card.AdvanceIfPending());
        Assert.Equal(44, card.BlackSouls_OrderNumber);
    }

    [Fact]
    public void AdvancingPreservesUpgradeAndKeywords()
    {
        var card = ModelStateIsolationTests.CreateMutable<NumberedOrderCard>();
        card.UpgradeInternal();
        card.FinalizeUpgradeInternal();
        card.BlackSouls_PendingAdvance = true;
        card.AdvanceIfPending();
        Assert.True(card.IsUpgraded);
        Assert.False(card.IsUpgradable);
        Assert.Contains(CardKeyword.Innate, card.Keywords);
        Assert.Contains(CardKeyword.Retain, card.Keywords);
        Assert.Equal(CardType.Power, card.Type);
        Assert.Equal(CardRarity.Ancient, card.Rarity);
    }

    [Theory]
    [InlineData(nameof(NumberedOrderCard.BlackSouls_OrderNumber))]
    [InlineData(nameof(NumberedOrderCard.BlackSouls_PendingAdvance))]
    public void ProgressIsSaved(string name) =>
        Assert.True(Attribute.IsDefined(typeof(NumberedOrderCard).GetProperty(name)!, typeof(SavedPropertyAttribute)));

    [Fact]
    public void CopiesDoNotAdvanceEachOther()
    {
        var first = ModelStateIsolationTests.CreateMutable<NumberedOrderCard>();
        var second = (NumberedOrderCard)first.MutableClone();
        first.BlackSouls_PendingAdvance = true;
        first.AdvanceIfPending();
        Assert.Equal(42, second.BlackSouls_OrderNumber);
        Assert.False(second.BlackSouls_PendingAdvance);
    }

    [Fact]
    public void SavedProgressRestoresCostAndPendingAdvance()
    {
        var original = ModelStateIsolationTests.CreateMutable<NumberedOrderCard>();
        original.BlackSouls_OrderNumber = 45;
        original.BlackSouls_PendingAdvance = true;
        var restored = ModelStateIsolationTests.CreateMutable<NumberedOrderCard>();
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(NumberedOrderCard));
        var saved = SavedProperties.From(original);
        Assert.NotNull(saved);
        saved.Fill(restored);
        Assert.Equal(45, restored.BlackSouls_OrderNumber);
        Assert.Equal(3, restored.EnergyCost.GetWithModifiers(CostModifiers.Local));
        Assert.True(restored.AdvanceIfPending());
        Assert.Equal(46, restored.BlackSouls_OrderNumber);
    }

    [Fact]
    public void DowngradeCleanupRestoresNumberedCost()
    {
        var card = ModelStateIsolationTests.CreateMutable<NumberedOrderCard>();
        card.BlackSouls_OrderNumber = 45;
        // Exercise cleanup without bootstrapping Godot's global ModelDb for a full downgrade.
        card.EnergyCost.ResetForDowngrade();
        typeof(NumberedOrderCard).GetMethod("AfterDowngraded",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(card, null);
        Assert.Equal(3, card.EnergyCost.GetWithModifiers(CostModifiers.Local));
    }

    [Fact]
    public void StunFollowUpParticipatesInSerialization() =>
        Assert.True(Attribute.IsDefined(
            typeof(OrderDetentionPower).GetProperty(nameof(OrderDetentionPower.BlackSouls_ResumeMoveId))!,
            typeof(SavedPropertyAttribute)));
}
