using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Stores the selected score target and timeout state for the poker dummy combat.</summary>
[RegisterActEncounter(typeof(Glory))]
public sealed class BalatroTrainingDummyEncounter : ModEncounterTemplate
{
    public override RoomType RoomType => RoomType.Monster;
    public override bool ShouldGiveRewards => false;
    public override bool IsValidForAct(ActModel act) => false;

    public int ScoreTarget { get; set; } = 300;
    public int RewardTier { get; set; } = 1;
    public bool RanOutOfTime { get; set; }
    public bool PokerDeckInitialized { get; set; }

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        [ModelDb.Monster<BattleFriendV2>(), ModelDb.Monster<BattleFriendV3>()];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
        [(ScoreTarget <= 300
            ? ModelDb.Monster<BattleFriendV2>().ToMutable()
            : ModelDb.Monster<BattleFriendV3>().ToMutable(), null)];

    public override Dictionary<string, string> SaveCustomState() => new()
    {
        [nameof(ScoreTarget)] = ScoreTarget.ToString(System.Globalization.CultureInfo.InvariantCulture),
        [nameof(RewardTier)] = RewardTier.ToString(System.Globalization.CultureInfo.InvariantCulture),
        [nameof(RanOutOfTime)] = RanOutOfTime.ToString(),
        [nameof(PokerDeckInitialized)] = PokerDeckInitialized.ToString(),
    };

    public override void LoadCustomState(Dictionary<string, string> state)
    {
        ScoreTarget = int.Parse(state[nameof(ScoreTarget)], System.Globalization.CultureInfo.InvariantCulture);
        RewardTier = int.Parse(state[nameof(RewardTier)], System.Globalization.CultureInfo.InvariantCulture);
        RanOutOfTime = bool.Parse(state[nameof(RanOutOfTime)]);
        PokerDeckInitialized = bool.Parse(state[nameof(PokerDeckInitialized)]);
    }
}
