using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterSharedAncient]
public class GrandGuignolAncient : ModAncientEventTemplate
{
    public override Color ButtonColor => new(0.45f, 0.02f, 0.08f, 0.5f);

    public override Color DialogueColor => new(0.45f, 0.02f, 0.08f);

    public override string? CustomBackgroundScenePath => "res://bs_ancient/assets/scenes/grand_guignol_ancient.tscn";

    public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
        MapIconPath: "res://bs_ancient/assets/images/map/grand_guignol.png",
        MapIconOutlinePath: "res://bs_ancient/assets/images/map/grand_guignol_outline.png",
        RunHistoryIconPath: "res://bs_ancient/assets/images/map/grand_guignol.png",
        RunHistoryIconOutlinePath: "res://bs_ancient/assets/images/map/grand_guignol_outline.png"
    );

    public override IEnumerable<EventOption> AllPossibleOptions => [
        CreateModRelicOption<RethinkPokerRelic>(),
        CreateModRelicOption<WormSmokeRelic>(),
        CreateModRelicOption<MargaretRelic>(),
        CreateModRelicOption<AngelFeatherRelic>(),
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [.. AllPossibleOptions];
    }

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }
}
