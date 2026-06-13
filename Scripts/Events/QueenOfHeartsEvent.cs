using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterActEvent(typeof(Hive))]
public sealed class QueenOfHeartsEvent : ModEventTemplate
{
    private const int GoldGain = 200;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://bs_ancient/assets/images/events/QueenOfHeartsEvent.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(GoldGain)];

    public override bool IsAllowed(IRunState runState)
    {
        return BsAncientConfig.EnableModEvents
            && runState.CurrentActIndex == 1
            && (runState.Players.Any(player => player.GetRelic<QueenTartRelic>() != null)
                || QueenTartModifier.FindActive(runState) != null);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(
            this,
            RequestWeapon,
            InitialOptionKey("WEAPON"),
            HoverTipFactory.FromRelic<RedQueenGuillotineRelic>()),
        new EventOption(this, RequestGold, InitialOptionKey("GOLD")),
    ];

    private async Task RequestWeapon()
    {
        await RelicCmd.Obtain<RedQueenGuillotineRelic>(Owner!);
        await RemoveTart();
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.WEAPON.description"));
    }

    private async Task RequestGold()
    {
        await PlayerCmd.GainGold(GoldGain, Owner!);
        await RemoveTart();
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.GOLD.description"));
    }

    private async Task RemoveTart()
    {
        if (Owner!.GetRelic<QueenTartRelic>() is { } tart)
        {
            await RelicCmd.Remove(tart);
        }

        if (QueenTartModifier.FindActive(Owner!.RunState) is { } modifier)
        {
            modifier.MarkClaimed();
        }
    }
}
