using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Lets an unclaimed Hearts Jack redirect one event into a choice about the Queen's tart.</summary>
[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Underdocks))]
[RegisterActEvent(typeof(Hive))]
[RegisterActEvent(typeof(Glory))]
public sealed class HeartJackEvent : ModEventTemplate
{
    private const int TartDamage = 5;
    private const string PortraitPath = "res://bs_ancient/assets/images/events/HeartJackEvent.jpg";

    public override EventAssetProfile AssetProfile => new(InitialPortraitPath: PortraitPath);

    public override bool IsAllowed(IRunState runState) => false;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(this, LetTartEscape, InitialOptionKey("RELEASE")),
        new EventOption(this, KeepTart, InitialOptionKey("KEEP"))
            .ThatDoesDamage(TartDamage),
    ];

    private async Task LetTartEscape()
    {
        HeartSuitEnchantment.ClaimNextEvent(Owner!);
        await RelicCmd.Obtain<HeartJackRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.RELEASE.description"));
    }

    private async Task KeepTart()
    {
        HeartSuitEnchantment.ClaimNextEvent(Owner!);
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            TartDamage,
            ValueProp.Move,
            null,
            null);
        await RelicCmd.Obtain<QueenTartRelic>(Owner);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.KEEP.description"));
    }
}
