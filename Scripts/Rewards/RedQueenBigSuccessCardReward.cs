using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Combat.Rewards;

namespace BlackSouls.Scripts;

/// <summary>Implements the Red Queen Big Success Card reward.</summary>
public sealed class RedQueenBigSuccessCardReward : Reward, IModSerializableReward
{
    private const int CardRewardOptions = 3;

    public RedQueenBigSuccessCardReward(Player player) : base(player)
    {
    }

    public RewardType ModRewardType => BsAncientRewardTypes.RedQueenBigSuccessCardReward;

    protected override RewardType RewardType => ModRewardType;

    public override int RewardsSetIndex => 0;

    public override LocString Description => new(
        "card_reward_ui",
        "BS_ANCIENT_REWARD_RED_QUEEN_BIG_SUCCESS_CARD_REWARD.description");

    public override bool IsPopulated => true;

    protected override string IconPath => "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png";

    public override void Populate()
    {
    }

    protected override async Task<bool> OnSelect()
    {
        await RewardsCmd.OfferCustom(Player, [CreateRareCardReward(Player)]);
        return true;
    }

    public override void MarkContentAsSeen()
    {
    }

    public override SerializableReward ToSerializable()
    {
        return ModRewardSerialization.CreateSerializable(this);
    }

    public string? ToModRewardJson()
    {
        return null;
    }

    private static CardReward CreateRareCardReward(Player player)
    {
        CardCreationOptions options = CardCreationOptions
            .ForNonCombatWithUniformOdds([player.Character.CardPool], card => card.Rarity == CardRarity.Rare)
            .WithFlags(CardCreationFlags.NoRarityModification);

        return new CardReward(options, CardRewardOptions, player);
    }
}
