using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Offers a third-act confrontation with Boojum to experienced Snark hunters.</summary>
[RegisterActEvent(typeof(Glory))]
public sealed class BoojumEvent : ModEventTemplate
{
    private const string PortraitPath = "res://bs_ancient/assets/images/events/BoojumEvent.svg";
    private const string DefaultPortraitPath = "res://images/events/bs_ancient_event_boojum_event.png";

    // Combat events must be shared so the event synchronizer can transition into combat.
    public override bool IsShared => true;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: PortraitPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        if (!BsAncientConfig.EnableModEvents
            || runState.CurrentActIndex != 2
            || runState.Players.Count != 1
            || SnarkPageRelicTrackerModifier.HasAppearedOrOwned<BoojumPageRelic>(runState))
        {
            return false;
        }

        return runState.Players.Any(HasRequiredPages);
    }

    public override IEnumerable<string> GetAssetPaths(IRunState runState)
    {
        return base.GetAssetPaths(runState)
            .Select(path => path == DefaultPortraitPath ? PortraitPath : path)
            .Append(PortraitPath)
            .Distinct();
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(this, Challenge, InitialOptionKey("CHALLENGE")),
        new EventOption(
            this,
            Flee,
            InitialOptionKey("FLEE"),
            HoverTipFactory.FromRelic<BoojumPageRelic>())
    ];

    private Task Challenge()
    {
        SnarkPageRelicTrackerModifier.MarkAppeared<BoojumPageRelic>(Owner!);
        BoojumEventEncounter encounter = (BoojumEventEncounter)ModelDb.Encounter<BoojumEventEncounter>().ToMutable();
        EnterCombatWithoutExitingEvent(encounter, [], shouldResumeAfterCombat: false);
        return Task.CompletedTask;
    }

    private async Task Flee()
    {
        SnarkPageRelicTrackerModifier.MarkAppeared<BoojumPageRelic>(Owner!);
        await RelicCmd.Obtain<BoojumPageRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.FLEE.description"));
    }

    private static bool HasRequiredPages(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        int pageCount = player.Relics.Count(relic => relic is
            BankersPageRelic or
            BarristersPageRelic or
            BellmansPageRelic or
            BakersPageRelic or
            BeaversBiologyClassRelic or
            ButchersMathClassRelic or
            HelmsmansPageRelic);

        return pageCount >= 2 || player.GetRelic<BakersPageRelic>() != null;
    }
}
