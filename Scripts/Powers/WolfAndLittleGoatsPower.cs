using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterPower]
public sealed class WolfAndLittleGoatsPower : ModPowerTemplate
{
    private const string PowerIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private bool _hasSeenFirstOwnerTurnStart;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: PowerIconPath,
        BigIconPath: PowerIconPath
    );

    [SavedProperty]
    public bool BlackSouls_HasSeenFirstOwnerTurnStart
    {
        get => _hasSeenFirstOwnerTurnStart;
        set
        {
            AssertMutable();
            _hasSeenFirstOwnerTurnStart = value;
        }
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        if (!BlackSouls_HasSeenFirstOwnerTurnStart)
        {
            BlackSouls_HasSeenFirstOwnerTurnStart = true;
            return;
        }

        if (Amount <= 1)
        {
            await PowerCmd.Remove(this);
            return;
        }

        SetAmount(Amount - 1, silent: true);
    }
}
