using System.Reflection;
using System.Runtime.CompilerServices;
using BlackSouls.Scripts;
using BlackSouls.Scripts.Patches;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BsAncient.Tests;

public sealed class SnarkSingleplayerTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(4, false)]
    public void SnarkContentRequiresExactlyOnePlayer(int count, bool expected)
    {
        Assert.Equal(expected, SnarkPageRelicTrackerModifier.CanAppearInRun(CreateRun(count)));
    }

    [Fact]
    public void SnarkContentDoesNotAppearWithoutARun()
    {
        Assert.False(SnarkPageRelicTrackerModifier.CanAppearInRun(null));
    }

    [Theory]
    [InlineData(typeof(BankersPageRelic))]
    [InlineData(typeof(BakersPageRelic))]
    [InlineData(typeof(BarristersPageRelic))]
    [InlineData(typeof(BeaversBiologyClassRelic))]
    [InlineData(typeof(BellmansPageRelic))]
    [InlineData(typeof(ButchersMathClassRelic))]
    [InlineData(typeof(HelmsmansPageRelic))]
    [InlineData(typeof(BoojumPageRelic))]
    public void EverySnarkPageRejectsMultiplayerBeforeReadingRelicsOrRng(Type relicType)
    {
        // Relic constructors need Godot; these eligibility checks must only read the run.
        RelicModel relic = (RelicModel)RuntimeHelpers.GetUninitializedObject(relicType);
        MethodInfo eligibility = relicType.GetMethod(nameof(RelicModel.IsAllowed))!;
        Assert.False((bool)eligibility.Invoke(relic, [CreateRun(2)])!);
    }

    [Fact]
    public void MultiplayerHiddenInjectionDoesNotEnumerateOrReplaceEventOptions()
    {
        EventModel eventModel = CreateMultiplayerEvent();
        IEnumerable<EventOption> options = ThrowOnEnumeration();
        IEnumerable<EventOption> original = options;

        HiddenEventOptionInjectionPatch.Prefix(eventModel, ref options);

        Assert.Same(original, options);
    }

    [Fact]
    public void MultiplayerHiddenRollDoesNotReadTheLedgerOrConsumeRng()
    {
        EventModel eventModel = CreateMultiplayerEvent();

        Assert.False(SnarkPageRelicTrackerModifier.ShouldOfferHiddenOption<BankersPageRelic>(eventModel, 100));
        Assert.Equal(0, SnarkPageRelicTrackerModifier.GetOrCreateHiddenOptionOutcome(
            eventModel, "LESSON", () => throw new InvalidOperationException("Must not roll in multiplayer.")));
    }

    private static EventModel CreateMultiplayerEvent()
    {
        Player player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        typeof(Player).GetField("_runState", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(player, CreateRun(2));
        EventModel eventModel = (EventModel)RuntimeHelpers.GetUninitializedObject(typeof(EndlessTeaPartyEvent));
        typeof(EventModel).GetProperty(nameof(EventModel.Owner))!.SetValue(eventModel, player);
        return eventModel;
    }

    private static IEnumerable<EventOption> ThrowOnEnumeration()
    {
        return Enumerable.Range(0, 1).Select<int, EventOption>(
            _ => throw new InvalidOperationException("Multiplayer options must remain untouched."));
    }

    private static IRunState CreateRun(int playerCount)
    {
        IRunState run = DispatchProxy.Create<IRunState, PlayerCountRunProxy>();
        ((PlayerCountRunProxy)run).Players = new Player[playerCount];
        return run;
    }

    public class PlayerCountRunProxy : DispatchProxy
    {
        public IReadOnlyList<Player> Players { get; set; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_Players"
                ? Players
                : throw new InvalidOperationException($"Unexpected run access: {targetMethod?.Name}");
    }
}
