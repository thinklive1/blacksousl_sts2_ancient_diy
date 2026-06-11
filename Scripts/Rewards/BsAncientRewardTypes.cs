using MegaCrit.Sts2.Core.Rewards;
using STS2RitsuLib.Combat.Rewards;

namespace BlackSouls.Scripts;

public static class BsAncientRewardTypes
{
    private static bool _registered;

    public static RewardType RedQueenBigSuccessCardReward { get; private set; }

    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        ModRewardDefinition definition = ModRewardRegistry
            .For(Entry.ModId)
            .RegisterOwned(
                "red_queen_big_success_card_reward",
                (_, player, _) => new RedQueenBigSuccessCardReward(player));

        RedQueenBigSuccessCardReward = definition.RewardType;
        _registered = true;
    }
}
