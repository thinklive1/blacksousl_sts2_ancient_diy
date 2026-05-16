using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class GiftOfChaosRelic : ModRelicTemplate
{
    private const int RewardGroups = 10;
    private const int RewardOptions = 3;

    private bool _allowInitialRewards;
    private bool _allowReplacementCard;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/GiftOfChaosRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/GiftOfChaosRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/GiftOfChaosRelic.png"
    );

    public override async Task AfterObtained()
    {
        Flash();
        _allowInitialRewards = true;
        try
        {
            await RewardsCmd.OfferCustom(Owner, CreateCardRewards());
        }
        finally
        {
            _allowInitialRewards = false;
        }
    }

    public override bool ShouldAddToDeck(CardModel card)
    {
        return card.Owner != Owner || _allowInitialRewards || _allowReplacementCard;
    }

    public override async Task BeforeCardRemoved(CardModel card)
    {
        if (card.Owner != Owner || card.Pile?.Type != PileType.Deck || _allowReplacementCard)
        {
            return;
        }

        CardModel replacement = Owner.RunState.CloneCard(card);
        _allowReplacementCard = true;
        try
        {
            await CardPileCmd.Add(replacement, PileType.Deck, source: this, skipVisuals: true);
        }
        finally
        {
            _allowReplacementCard = false;
        }
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || _allowInitialRewards)
        {
            return false;
        }

        return RemoveCardRelatedRewards(rewards);
    }

    public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || _allowInitialRewards)
        {
            return false;
        }

        return RemoveCardRelatedRewards(rewards);
    }

    public override Task AfterModifyingRewards()
    {
        Flash();
        return Task.CompletedTask;
    }

    private List<Reward> CreateCardRewards()
    {
        List<Reward> rewards = [];
        CardCreationOptions options = CardCreationOptions.ForNonCombatWithUniformOdds([Owner.Character.CardPool]);
        for (int i = 0; i < RewardGroups; i++)
        {
            rewards.Add(new CardReward(options, RewardOptions, Owner));
        }

        return rewards;
    }

    private static bool RemoveCardRelatedRewards(List<Reward> rewards)
    {
        int removed = rewards.RemoveAll(reward => reward is CardReward or CardRemovalReward);
        return removed > 0;
    }
}
