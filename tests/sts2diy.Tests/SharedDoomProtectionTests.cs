using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization.Formatters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using SmartFormat;
using Sts2Diy.Scripts;
using Sts2Diy.Scripts.Powers;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class SharedDoomProtectionTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    public void ReaperFormProtectsOnlyItsOwnerWithoutClearingDoom(int amount, bool expectedDoomed)
    {
        var owner = CreateOwner(CombatSide.Player, false);
        var ally = CreateOwner(CombatSide.Player, false);
        var reaper = CreatePower<ReaperFormPower>(owner.Creature);
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(reaper, amount);
        ((List<PowerModel>)owner.Creature.Powers).Add(reaper);
        Harmony harmony = InstallDoomPatch();
        try
        {
            Assert.Equal(expectedDoomed, DoomPower.GetDoomedCreatures([owner.Creature]).Contains(owner.Creature));
            Assert.Contains(ally.Creature, DoomPower.GetDoomedCreatures([ally.Creature]));
            Assert.Equal(20, owner.Doom.Amount);
            ((List<PowerModel>)owner.Creature.Powers).Remove(reaper);
            Assert.Contains(owner.Creature, DoomPower.GetDoomedCreatures([owner.Creature]));
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeDoomCheckProtectsOnlyTheFlaggedCreatureAndKeepsDoomStacks(bool protectedOwner)
    {
        var owner = CreateOwner(CombatSide.Player, protectedOwner);
        var ally = CreateOwner(CombatSide.Player, false);
        var enemy = CreateOwner(CombatSide.Enemy, false);
        Harmony harmony = InstallDoomPatch();
        try
        {
            var doomed = DoomPower.GetDoomedCreatures([owner.Creature, ally.Creature, enemy.Creature]);
            Assert.Equal(!protectedOwner, doomed.Contains(owner.Creature));
            Assert.Contains(ally.Creature, doomed);
            Assert.Contains(enemy.Creature, doomed);
            Assert.Equal(20, owner.Doom.Amount);
            Assert.Equal(10, owner.Creature.CurrentHp);
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    [Fact]
    public async Task ProtectionSurvivesOtherPlayersAndEnemyTurnsButExpiresInTheOwnersLateEndPhase()
    {
        var owner = CreateOwner(CombatSide.Player, true);
        var ally = CreateOwner(CombatSide.Player, false);
        Harmony harmony = InstallDoomPatch();
        try
        {
            var context = new ThrowingPlayerChoiceContext();
            await owner.Transfer.AfterSideTurnEndLate(context, CombatSide.Player, [ally.Creature]);
            Assert.True(owner.Transfer.Sts2Diy_DoomProtection);
            await owner.Transfer.AfterSideTurnEndLate(context, CombatSide.Enemy, [owner.Creature]);
            Assert.True(owner.Transfer.Sts2Diy_DoomProtection);
            await owner.Transfer.AfterSideTurnEnd(context, CombatSide.Player, [owner.Creature]);
            Assert.Empty(DoomPower.GetDoomedCreatures([owner.Creature]));
            await owner.Transfer.AfterSideTurnEndLate(context, CombatSide.Player, [owner.Creature]);
            Assert.False(owner.Transfer.Sts2Diy_DoomProtection);
            Assert.Contains(owner.Creature, DoomPower.GetDoomedCreatures([owner.Creature]));
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    [Fact]
    public void ProtectionIsSavedAndDoesNotOverrideGeneralDeathPrevention()
    {
        var property = typeof(SharedDoomTransferPower).GetProperty("Sts2Diy_DoomProtection")!;
        Assert.NotNull(property.GetCustomAttribute<SavedPropertyAttribute>());
        foreach (string hook in new[] { "ShouldDie", "ShouldDieLate", "AfterPreventingDeath" })
            Assert.Null(typeof(SharedDoomTransferPower).GetMethod(hook,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
    }

    [Theory]
    [InlineData("zhs")]
    [InlineData("eng")]
    public void ProtectionDescriptionsAreConditionalAndHaveTheirVariables(string language)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        string localization = Path.Combine(root.FullName, "sts2diy", "localization", language);
        using JsonDocument cards = JsonDocument.Parse(File.ReadAllText(Path.Combine(localization, "cards.json")));
        string cardText = cards.RootElement.GetProperty("STS2DIY_CARD_SHARED_DOOM_CARD.description").GetString()!;
        Assert.Contains("{IfUpgraded:show:", cardText);
        using JsonDocument powers = JsonDocument.Parse(File.ReadAllText(Path.Combine(localization, "powers.json")));
        string powerText = powers.RootElement.GetProperty("STS2DIY_POWER_SHARED_DOOM_TRANSFER_POWER.description").GetString()!;
        Assert.Contains("{DoomProtection:cond:", powerText);
        var formatter = Smart.CreateDefaultSmartFormat();
        formatter.AddExtensions(new ShowIfUpgradedFormatter());
        foreach (UpgradeDisplay display in Enum.GetValues<UpgradeDisplay>())
        {
            string rendered = formatter.Format(cardText, new Dictionary<string, object>
            {
                ["IfUpgraded"] = new IfUpgradedVar(display)
            });
            Assert.Equal(display != UpgradeDisplay.Normal, rendered.Contains('\n'));
        }
        foreach (bool active in new[] { false, true })
        {
            string rendered = formatter.Format(powerText, new Dictionary<string, object> { ["DoomProtection"] = active });
            Assert.Equal(active, rendered.Contains('\n'));
        }
        var power = CreateOwner(CombatSide.Player, true).Transfer;
        // Test the real description getter without invoking Godot's UI.
        AccessTools.Field(typeof(AbstractModel), "<Id>k__BackingField").SetValue(power,
            new ModelId("POWER", "STS2DIY_POWER_SHARED_DOOM_TRANSFER_POWER"));
        Assert.Equal(true, power.Description.Variables["DoomProtection"]);
    }

    private static Harmony InstallDoomPatch()
    {
        Harmony harmony = new("sts2diy.tests.shared_doom_protection");
        Type patch = typeof(Entry).Assembly.GetType("Sts2Diy.Scripts.Patches.SharedDoomProtectionPatch", true)!;
        harmony.Patch(AccessTools.Method(typeof(DoomPower), "IsOwnerDoomed"),
            postfix: new HarmonyMethod(patch, "Postfix"));
        return harmony;
    }

    private static (Creature Creature, DoomPower Doom, SharedDoomTransferPower Transfer) CreateOwner(CombatSide side, bool protect)
    {
        // Supply only the fields used by native Doom checks, avoiding Godot/model registration.
        var creature = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(creature, 10);
        AccessTools.Field(typeof(Creature), "<Side>k__BackingField").SetValue(creature, side);
        var doom = CreatePower<DoomPower>(creature);
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(doom, 20);
        var transfer = CreatePower<SharedDoomTransferPower>(creature);
        transfer.Sts2Diy_DoomProtection = protect;
        AccessTools.Field(typeof(Creature), "_powers").SetValue(creature, new List<PowerModel> { doom, transfer });
        return (creature, doom, transfer);
    }

    private static T CreatePower<T>(Creature owner) where T : PowerModel
    {
        var power = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(power, true);
        AccessTools.Field(typeof(PowerModel), "_owner").SetValue(power, owner);
        return power;
    }
}
