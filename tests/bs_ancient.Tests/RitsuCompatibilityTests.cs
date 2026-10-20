using System.Reflection;
using BlackSouls.Scripts;
using BlackSouls.Scripts.Cards;
using STS2RitsuLib;
using STS2RitsuLib.Content;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Toast;
using STS2RitsuLib.Utils;

namespace BsAncient.Tests;

public sealed class RitsuCompatibilityTests
{
    [Theory]
    [InlineData(typeof(RitsuLibFramework), "STS2-RitsuLib.Runtime")]
    [InlineData(typeof(I18N), "STS2-RitsuLib.Shared")]
    [InlineData(typeof(RitsuToastService), "STS2-RitsuLib.Ui")]
    [InlineData(typeof(ModSettingsText), "STS2-RitsuLib.Settings")]
    public void SplitModulesAndFacadeResolvePublicTypes(Type publicType, string moduleName)
    {
        Assert.Equal(moduleName, publicType.Assembly.GetName().Name);
        Assert.Equal(new Version(0, 6, 9, 0), publicType.Assembly.GetName().Version);
        Assembly facade = Assembly.Load("STS2-RitsuLib");
        Assert.Equal(publicType, facade.GetType(publicType.FullName!, throwOnError: true));
    }

    [Theory]
    [InlineData(typeof(SnowWhiteFavorRelic), "BS_ANCIENT_RELIC_SNOW_WHITE_FAVOR_RELIC")]
    [InlineData(typeof(SleepEnchantment), "BS_ANCIENT_ENCHANTMENT_SLEEP_ENCHANTMENT")]
    [InlineData(typeof(SinbadSlowPower), "BS_ANCIENT_POWER_SINBAD_SLOW_POWER")]
    [InlineData(typeof(MermaidTearCard), "BS_ANCIENT_CARD_MERMAID_TEAR_CARD")]
    [InlineData(typeof(LastAceCard), "BS_ANCIENT_CARD_LAST_ACE_CARD")]
    [InlineData(typeof(RedQueenDanceShoesRelic), "BS_ANCIENT_RELIC_RED_QUEEN_DANCE_SHOES_RELIC")]
    [InlineData(typeof(RedDanceShoesPower), "BS_ANCIENT_POWER_RED_DANCE_SHOES_POWER")]
    public void ContentIdsRemainCompatibleWithExistingLocalization(Type modelType, string expectedId)
    {
        Assert.Equal(expectedId, ModContentRegistry.GetFixedPublicEntry(Entry.ModId, modelType));
    }
}
