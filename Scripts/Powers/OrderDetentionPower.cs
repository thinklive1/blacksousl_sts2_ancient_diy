using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Repeats the native stun for each remaining enemy turn without losing its follow-up move.</summary>
[RegisterPower]
public sealed class OrderDetentionPower : ModPowerTemplate
{
    private string _resumeMoveId = string.Empty;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/WonderlandCodeOfLawsRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/WonderlandCodeOfLawsRelic.png");

    [SavedProperty]
    public string BlackSouls_ResumeMoveId
    {
        get => _resumeMoveId;
        set { AssertMutable(); _resumeMoveId = value ?? string.Empty; }
    }

    internal Task EnsureStunned()
    {
        if (Amount <= 0 || !Owner.IsAlive || Owner.Monster == null
            || string.IsNullOrEmpty(BlackSouls_ResumeMoveId)) return Task.CompletedTask;
        return CreatureCmd.Stun(Owner, BlackSouls_ResumeMoveId);
    }

    public override Task AfterSideTurnStart(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        // Player turn setup rolls enemy moves; reapply afterward so the intent is accurate.
        return EnsureStunned();
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side && participants.Contains(Owner))
        {
            await PowerCmd.Decrement(this);
        }
    }
}
