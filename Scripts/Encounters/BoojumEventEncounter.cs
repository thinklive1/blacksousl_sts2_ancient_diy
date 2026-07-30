using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Provides the event-only combat encounter for Boojum.</summary>
[RegisterActEncounter(typeof(Glory))]
public sealed class BoojumEventEncounter : ModEncounterTemplate
{
    public override RoomType RoomType => RoomType.Monster;

    // The event constructs this encounter explicitly; it must never enter the regular encounter pool.
    public override bool IsValidForAct(ActModel act) => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<Boojum>()];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        [(ModelDb.Monster<Boojum>().ToMutable(), null)];
}
