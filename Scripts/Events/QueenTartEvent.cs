using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterActEvent(typeof(Overgrowth))]
public sealed class QueenTartEvent : ModEventTemplate
{
    private const int MaxHpGain = 8;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://bs_ancient/assets/images/events/QueenTartEvent.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(MaxHpGain)];

    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex == 0
            && !runState.Players.Any(player => player.GetRelic<QueenTartRelic>() != null)
            && !runState.Modifiers.Any(modifier => modifier is QueenTartModifier);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(this, EatTart, InitialOptionKey("EAT")),
        new EventOption(
            this,
            KeepTart,
            InitialOptionKey("KEEP"),
            HoverTipFactory.FromRelic<QueenTartRelic>()),
    ];

    private async Task EatTart()
    {
        await CreatureCmd.GainMaxHp(Owner!.Creature, DynamicVars.MaxHp.BaseValue);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.EAT.description"));
    }

    private async Task KeepTart()
    {
        await RelicCmd.Obtain<QueenTartRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.KEEP.description"));
    }
}
