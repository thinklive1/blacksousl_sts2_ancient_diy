using BlackSouls.Scripts;
using BlackSouls.Scripts.Patches;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace BsAncient.Tests;

public sealed class CompatibilitySurfaceTests
{
    [Fact]
    public void InstalledGameStillExposesRequiredCompatibilityMembers()
    {
        Assert.True(OnlyUseModAncientsPatch.HasRoomAccess);
        Assert.True(OnlyUseModAncientsPatch.HasSharedAncientSubsetAccess);
        Assert.True(HelmsmansPageRelicPatch.CanInspectMoves);
        Assert.True(NeowRethinkPokerPatch.CanCreateRelicOption);
        Assert.True(HiddenEventOptionSupport.CanFinishEvents);
        Assert.True(BoojumHistoryPurge.HasSaveStoreAccess);
    }

    [Fact]
    public void InstalledGameStillExposesPatchedLifecycleMethods()
    {
        Assert.NotNull(AccessTools.Method(typeof(EventOption), "AddLocVars", [typeof(EventModel)]));
        Assert.NotNull(AccessTools.Method(
            typeof(EventModel),
            "SetEventState",
            [typeof(LocString), typeof(IEnumerable<EventOption>)]));
        Assert.NotNull(AccessTools.Method(
            typeof(Hook),
            nameof(Hook.BeforeRoomEntered),
            [typeof(IRunState), typeof(AbstractRoom)]));
        Assert.NotNull(AccessTools.Method(
            typeof(Hook),
            nameof(Hook.AfterCombatEnd),
            [typeof(IRunState), typeof(ICombatState), typeof(CombatRoom)]));
        Assert.NotNull(AccessTools.Method(
            typeof(Hook),
            nameof(Hook.AfterCardDrawn),
            [typeof(ICombatState), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)]));
        Assert.NotNull(AccessTools.Method(
            typeof(Hook),
            nameof(Hook.AfterCardPlayed),
            [typeof(ICombatState), typeof(PlayerChoiceContext), typeof(CardPlay)]));
        Assert.NotNull(AccessTools.Method(
            typeof(Hook),
            nameof(Hook.AfterPlayerTurnStart),
            [typeof(ICombatState), typeof(PlayerChoiceContext), typeof(Player)]));
        Assert.NotNull(AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.OnPlayWrapper),
            [typeof(PlayerChoiceContext), typeof(Creature), typeof(bool), typeof(ResourceInfo), typeof(bool)]));
        Assert.NotNull(AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.CanPlay),
            [typeof(UnplayableReason).MakeByRefType(), typeof(AbstractModel).MakeByRefType()]));
        Assert.NotNull(AccessTools.Method(typeof(NEventOptionButton), nameof(NEventOptionButton._Ready)));
        Assert.NotNull(AccessTools.Method(typeof(NEventOptionButton), nameof(NEventOptionButton.EnableButton)));
        Assert.NotNull(AccessTools.Method(typeof(NEventOptionButton), "OnFocus"));
        Assert.NotNull(AccessTools.Method(typeof(NEventOptionButton), "OnUnfocus"));
    }

    [Theory]
    [InlineData("ENDLESS_TEA_PARTY_EVENT.title", true)]
    [InlineData("QUEEN_OF_HEARTS_EVENT.pages.INITIAL.description", true)]
    [InlineData("NEOW.pages.INITIAL.description", false)]
    [InlineData("THIRD_PARTY_EVENT.pages.INITIAL.description", false)]
    public void LegacyLocalizationFallbackIsLimitedToKnownModEvents(string key, bool expected)
    {
        Assert.Equal(expected, ModEventCompatibilityPatch.IsKnownLegacyEventKey(key));
    }
}
