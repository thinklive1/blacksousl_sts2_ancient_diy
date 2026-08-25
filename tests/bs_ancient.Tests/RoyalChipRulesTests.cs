using BlackSouls.Scripts;

namespace BlackSouls.Tests;

public sealed class RoyalChipRulesTests
{
    [Fact]
    public void BeforeTurnFiveRequiresVictoryByTurnFour()
    {
        Assert.True(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.BeforeTurnFive, 4, 0, false, 0));
        Assert.False(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.BeforeTurnFive, 5, 0, false, 0));
    }

    [Fact]
    public void ThresholdConditionsUseInclusiveLimits()
    {
        Assert.True(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.HealthLossAtMostTen, 0, 10, false, 0));
        Assert.True(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.CardsAtMostTwenty, 0, 0, false, 20));
        Assert.False(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.HealthLossAtMostTen, 0, 11, false, 0));
        Assert.False(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.CardsAtMostTwenty, 0, 0, false, 21));
    }

    [Fact]
    public void BooleanConditionsReflectCombatEvents()
    {
        Assert.True(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.NoPotions, 0, 0, false, 0));
        Assert.False(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.NoPotions, 0, 0, true, 0));
    }

    [Fact]
    public void NewCardAndOverkillConditionsUseTheirLimits()
    {
        Assert.True(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.UniqueCardNames, 0, 0, false, 0, 1));
        Assert.False(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.UniqueCardNames, 0, 0, false, 0, 2));
        Assert.True(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.OverkillKill, 0, 0, false, 0, 0, true));
        Assert.True(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.AttackCardsAtMostEight, 0, 0, false, 0, 0, false, 8));
        Assert.False(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.AttackCardsAtMostEight, 0, 0, false, 0, 0, false, 9));
        Assert.True(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.SkillCardsAtMostTwelve, 0, 0, false, 0, 0, false, 0, 12));
        Assert.True(RoyalChipRelic.IsConditionMet(
            RoyalChipConditionKind.PowerCardsAtMostThree, 0, 0, false, 0, 0, false, 0, 0, 3));
    }

    [Fact]
    public void WagersRequireARealStakeAtCombatStart()
    {
        Assert.False(RoyalChipRelic.IsWagerAvailable(RoyalChipWagerKind.Gold, 1, 0, 0));
        Assert.True(RoyalChipRelic.IsWagerAvailable(RoyalChipWagerKind.Gold, 2, 0, 0));
        Assert.False(RoyalChipRelic.IsWagerAvailable(RoyalChipWagerKind.Card, 0, 0, 0));
        Assert.True(RoyalChipRelic.IsWagerAvailable(RoyalChipWagerKind.Card, 0, 1, 0));
        Assert.False(RoyalChipRelic.IsWagerAvailable(RoyalChipWagerKind.Relic, 0, 0, 0));
        Assert.True(RoyalChipRelic.IsWagerAvailable(RoyalChipWagerKind.Relic, 0, 0, 1));
        Assert.True(RoyalChipRelic.IsWagerAvailable(RoyalChipWagerKind.MaxHp, 0, 0, 0));
    }

    [Fact]
    public void InteractiveRewardsRequireEnoughCurrentCandidates()
    {
        Assert.False(RoyalChipRelic.IsRewardAvailable(RoyalChipRewardKind.Gold, 0, 0, 0, 0));
        Assert.True(RoyalChipRelic.IsRewardAvailable(RoyalChipRewardKind.Gold, 1, 0, 0, 0));
        Assert.False(RoyalChipRelic.IsRewardAvailable(RoyalChipRewardKind.Card, 0, 1, 0, 1));
        Assert.True(RoyalChipRelic.IsRewardAvailable(RoyalChipRewardKind.Card, 0, 2, 0, 2));
        Assert.False(RoyalChipRelic.IsRewardAvailable(RoyalChipRewardKind.CardRemoval, 0, 0, 0, 1));
        Assert.True(RoyalChipRelic.IsRewardAvailable(RoyalChipRewardKind.CardRemoval, 0, 0, 1, 1));
        Assert.False(RoyalChipRelic.IsRewardAvailable(RoyalChipRewardKind.CardCopy, 0, 0, 0, 0));
        Assert.True(RoyalChipRelic.IsRewardAvailable(RoyalChipRewardKind.CardCopy, 0, 0, 0, 1));
        Assert.False(RoyalChipRelic.IsRewardAvailable(RoyalChipRewardKind.SuitEnchant, 0, 0, 0, 10, 2));
        Assert.True(RoyalChipRelic.IsRewardAvailable(RoyalChipRewardKind.SuitEnchant, 0, 0, 0, 10, 3));
    }
}
