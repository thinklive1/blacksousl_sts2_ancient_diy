using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterActEvent(typeof(Hive))]
public sealed class BirdSingerEvent : ModEventTemplate
{
    private const int HpLoss = 10;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://bs_ancient/assets/images/events/BirdSinger.png"
    );

    public override bool IsAllowed(IRunState runState)
    {
        if (runState.CurrentActIndex != 1 || runState.Modifiers.Any(modifier => modifier is BirdSingerModifier))
        {
            return false;
        }

        MapPoint? currentPoint = runState.CurrentMapPoint;
        return currentPoint != null
            && currentPoint.coord.row <= runState.Map.GetRowCount() / 2
            && runState.Map.GetAllMapPoints().Any(point =>
                point.PointType == MapPointType.Monster
                && point.coord.row > currentPoint.coord.row);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(this, Wait, InitialOptionKey("WAIT")).ThatDoesDamage(HpLoss),
        new EventOption(this, Flee, InitialOptionKey("FLEE")),
    ];

    private async Task Wait()
    {
        await ApplyEffect(showMarkers: true);
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner!.Creature,
            HpLoss,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.WAIT.description"));
    }

    private async Task Flee()
    {
        await ApplyEffect(showMarkers: false);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.FLEE.description"));
    }

    private Task ApplyEffect(bool showMarkers)
    {
        if (Owner!.RunState is not RunState runState)
        {
            return Task.CompletedTask;
        }

        BirdSingerModifier modifier = (BirdSingerModifier)ModelDb.Modifier<BirdSingerModifier>().ToMutable();
        modifier.OnRunLoaded(runState);
        modifier.Configure(runState, showMarkers);
        runState.AddModifierDebug(modifier);
        return Task.CompletedTask;
    }
}
