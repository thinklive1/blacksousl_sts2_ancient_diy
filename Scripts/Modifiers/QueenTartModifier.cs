using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BlackSouls.Scripts;

public sealed class QueenTartModifier : ModifierModel
{
    private bool _claimed;

    [SavedProperty]
    public bool BlackSouls_Claimed
    {
        get => _claimed;
        set
        {
            AssertMutable();
            _claimed = value;
        }
    }

    public override EventModel ModifyNextEvent(EventModel currentEvent)
    {
        return RunState.CurrentActIndex == 1 && !BlackSouls_Claimed
            ? ModelDb.Event<QueenOfHeartsEvent>()
            : currentEvent;
    }

    public static QueenTartModifier? FindActive(IRunState runState)
    {
        return runState.Modifiers
            .OfType<QueenTartModifier>()
            .FirstOrDefault(modifier => !modifier.BlackSouls_Claimed);
    }

    public void MarkClaimed()
    {
        AssertMutable();
        BlackSouls_Claimed = true;
    }
}
