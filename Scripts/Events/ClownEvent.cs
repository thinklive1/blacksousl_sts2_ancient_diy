using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Clown event.</summary>
[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Underdocks))]
public sealed class ClownEvent : ModEventTemplate
{
    private const string PortraitPath = "res://bs_ancient/assets/images/events/ClownEvent.jpg";
    private const string DefaultPortraitPath = "res://images/events/bs_ancient_event_clown_event.png";

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: PortraitPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return BsAncientConfig.EnableModEvents
            && !BsAncientConfig.DisableTestingEvents
            && runState.Players.Count == 1
            && runState.CurrentActIndex == 0;
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
        RelicOption<RabbitHandMirrorRelic>(ObtainRabbitMirror, InitialOptionKey("RABBIT")),
        RelicOption<PumpkinHandMirrorRelic>(ObtainPumpkinMirror, InitialOptionKey("PUMPKIN")),
        RelicOption<JackHandMirrorRelic>(ObtainJackMirror, InitialOptionKey("JACK"))
    ];

    private async Task ObtainRabbitMirror()
    {
        await RelicCmd.Obtain<RabbitHandMirrorRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.DONE.description"));
    }

    private async Task ObtainPumpkinMirror()
    {
        await RelicCmd.Obtain<PumpkinHandMirrorRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.DONE.description"));
    }

    private async Task ObtainJackMirror()
    {
        await RelicCmd.Obtain<JackHandMirrorRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.DONE.description"));
    }
}
