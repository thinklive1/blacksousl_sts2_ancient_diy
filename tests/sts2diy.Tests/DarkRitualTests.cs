using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Sts2Diy.Scripts.Cards;
using Sts2Diy.Scripts.Powers;
using Xunit;

namespace Sts2Diy.Tests;

public sealed class DarkRitualTests : IDisposable
{
    private readonly Harmony _harmony = new("sts2diy.tests.dark_ritual");
    private static List<(Creature Owner, decimal Amount)> _losses = [];
    private static List<(Player Owner, decimal Amount, AbstractModel Source)> _summons = [];
    private static List<string> _order = [];
    private static bool _ending;
    private static bool _endDuringLoss;
    private static bool _dieDuringLoss;

    public DarkRitualTests()
    {
        _losses = [];
        _summons = [];
        _order = [];
        _ending = _endDuringLoss = _dieDuringLoss = false;
        _harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsOverOrEnding)),
            prefix: new HarmonyMethod(typeof(DarkRitualTests), nameof(CombatEnding)));
        _harmony.Patch(AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.LoseMaxHp)),
            prefix: new HarmonyMethod(typeof(DarkRitualTests), nameof(LoseMaxHp)));
        _harmony.Patch(AccessTools.Method(typeof(OstyCmd), nameof(OstyCmd.Summon)),
            prefix: new HarmonyMethod(typeof(DarkRitualTests), nameof(Summon)));
    }

    public void Dispose() => _harmony.UnpatchAll(_harmony.Id);

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    public async Task EachOwnerTurnPaysOneMaxHpBeforeSummoning(int amount)
    {
        var owner = CreatePlayer(amount);
        var other = CreatePlayer(8);
        var context = new ThrowingPlayerChoiceContext();
        await owner.Power.AfterPlayerTurnStart(context, other.Player);
        Assert.Empty(_losses);
        Assert.Empty(_summons);
        await owner.Power.AfterPlayerTurnStart(context, owner.Player);
        await owner.Power.AfterPlayerTurnStart(context, owner.Player);
        Assert.Equal(38, owner.Creature.MaxHp);
        Assert.Equal(40, other.Creature.MaxHp);
        Assert.Equal(new[] { "loss", "summon", "loss", "summon" }, _order);
        Assert.All(_losses, loss => Assert.Equal((owner.Creature, 1m), loss));
        Assert.All(_summons, summon => Assert.Equal((owner.Player, (decimal)amount, (AbstractModel)owner.Power), summon));
        Assert.Equal(amount, owner.Power.Amount);
    }

    [Fact]
    public async Task MixedUpgradedAndNormalCopiesEachKeepTheirOwnCostAndSummonAmount()
    {
        var owner = CreatePlayer(8);
        var upgraded = CreatePower(owner.Creature, 10);
        Assert.Equal(PowerInstanceType.Instanced, owner.Power.InstanceType);
        var context = new ThrowingPlayerChoiceContext();
        await owner.Power.AfterPlayerTurnStart(context, owner.Player);
        await upgraded.AfterPlayerTurnStart(context, owner.Player);
        Assert.Equal(38, owner.Creature.MaxHp);
        Assert.Equal(new decimal[] { 8, 10 }, _summons.Select(s => s.Amount));
        Assert.Equal(2, _losses.Count);
    }

    [Theory]
    [InlineData(true, false, 8)]
    [InlineData(false, true, 8)]
    [InlineData(false, false, 0)]
    public async Task DeadOwnersEndedFightsAndEmptyPowersDoNothing(bool dead, bool ending, int amount)
    {
        var owner = CreatePlayer(amount);
        if (dead) AccessTools.Field(typeof(Creature), "_currentHp").SetValue(owner.Creature, 0);
        _ending = ending;
        await owner.Power.AfterPlayerTurnStart(new ThrowingPlayerChoiceContext(), owner.Player);
        Assert.Empty(_losses);
        Assert.Empty(_summons);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task DoesNotSummonWhenTheMaxHpCostKillsTheOwnerOrEndsCombat(bool dies, bool ends)
    {
        var owner = CreatePlayer(8);
        _dieDuringLoss = dies;
        _endDuringLoss = ends;
        await owner.Power.AfterPlayerTurnStart(new ThrowingPlayerChoiceContext(), owner.Player);
        Assert.Single(_losses);
        Assert.Empty(_summons);
    }

    [Fact]
    public void CardHasUpgradeableSummonAndPlayingOnlyInstallsThePower()
    {
        var card = (DarkRitualCard)RuntimeHelpers.GetUninitializedObject(typeof(DarkRitualCard));
        Assert.Equal(new[] { CardKeyword.Ethereal }, card.CanonicalKeywords);
        var variables = (IEnumerable<DynamicVar>)typeof(DarkRitualCard)
            .GetProperty("CanonicalVars", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(card)!;
        var summon = Assert.Single(variables.OfType<SummonVar>());
        Assert.Equal(8m, summon.BaseValue);
        summon.UpgradeValueBy(2m);
        Assert.Equal(10m, summon.BaseValue);
        var upgrade = PatchProcessor.GetOriginalInstructions(AccessTools.Method(typeof(DarkRitualCard), "OnUpgrade"));
        Assert.Contains(upgrade, i => i.operand is MethodInfo m && m.Name == "get_Summon");
        Assert.Contains(upgrade, i => i.operand is MethodInfo m && m.Name == "UpgradeValueBy");
        var play = PatchProcessor.GetOriginalInstructions(AccessTools.Method(typeof(DarkRitualCard), "OnPlay"));
        Assert.Contains(play, i => i.operand is MethodInfo m && m.DeclaringType == typeof(PowerCmd)
            && m.Name == "Apply" && m.IsGenericMethod
            && m.GetGenericArguments().SequenceEqual([typeof(DarkRitualPower)]));
        Assert.DoesNotContain(play, i => i.operand is MethodInfo m
            && (m.DeclaringType == typeof(OstyCmd) || m.DeclaringType == typeof(CreatureCmd)));
    }

    [Theory]
    [InlineData("zhs", "黑暗仪式")]
    [InlineData("eng", "Dark Ritual")]
    public void LocalizedCardPreviewsSummonAndStatusDisplaysTheCurrentAmount(string language, string title)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "sts2diy.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        string folder = Path.Combine(root.FullName, "sts2diy", "localization", language);
        using var cards = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "cards.json")));
        using var powers = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "powers.json")));
        Assert.Equal(title, cards.RootElement.GetProperty("STS2DIY_CARD_DARK_RITUAL_CARD.title").GetString());
        Assert.Contains("{Summon:diff()}", cards.RootElement.GetProperty("STS2DIY_CARD_DARK_RITUAL_CARD.description").GetString());
        Assert.Equal(title, powers.RootElement.GetProperty("STS2DIY_POWER_DARK_RITUAL_POWER.title").GetString());
        Assert.Contains("{Amount}", powers.RootElement.GetProperty("STS2DIY_POWER_DARK_RITUAL_POWER.description").GetString());
    }

    private static bool CombatEnding(ref bool __result)
    {
        __result = _ending;
        return false;
    }

    private static bool LoseMaxHp(PlayerChoiceContext choiceContext, Creature creature, decimal amount,
        CardModel? cardSource, ref Task __result)
    {
        Assert.Null(cardSource);
        _losses.Add((creature, amount));
        _order.Add("loss");
        AccessTools.Field(typeof(Creature), "_maxHp").SetValue(creature, creature.MaxHp - (int)amount);
        if (_dieDuringLoss) AccessTools.Field(typeof(Creature), "_currentHp").SetValue(creature, 0);
        if (_endDuringLoss) _ending = true;
        __result = Task.CompletedTask;
        return false;
    }

    private static bool Summon(PlayerChoiceContext choiceContext, Player summoner, decimal amount,
        AbstractModel source, ref Task<SummonResult> __result)
    {
        _summons.Add((summoner, amount, source));
        _order.Add("summon");
        __result = Task.FromResult(new SummonResult(null!, amount));
        return false;
    }

    private static (Player Player, Creature Creature, DarkRitualPower Power) CreatePlayer(int amount)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var creature = (Creature)RuntimeHelpers.GetUninitializedObject(typeof(Creature));
        AccessTools.Field(typeof(Creature), "<Player>k__BackingField").SetValue(creature, player);
        AccessTools.Field(typeof(Player), "<Creature>k__BackingField").SetValue(player, creature);
        AccessTools.Field(typeof(Creature), "_currentHp").SetValue(creature, 20);
        AccessTools.Field(typeof(Creature), "_maxHp").SetValue(creature, 40);
        return (player, creature, CreatePower(creature, amount));
    }

    private static DarkRitualPower CreatePower(Creature owner, int amount)
    {
        var power = (DarkRitualPower)RuntimeHelpers.GetUninitializedObject(typeof(DarkRitualPower));
        AccessTools.Property(typeof(AbstractModel), "IsMutable").SetValue(power, true);
        AccessTools.Field(typeof(PowerModel), "_owner").SetValue(power, owner);
        AccessTools.Field(typeof(PowerModel), "_amount").SetValue(power, amount);
        return power;
    }
}
