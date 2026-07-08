using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Bankers Page relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class BankersPageRelic : ModRelicTemplate
{
    private const int CommonCardPrice = 50;
    private const int UncommonCardPrice = 75;
    private const int RareCardPrice = 150;
    private const float ColorlessPriceMultiplier = 1.15f;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/scripts.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return !SnarkPageRelicTrackerModifier.HasAppearedOrOwned<BankersPageRelic>(runState);
    }

    public override Task AfterObtained()
    {
        if (Owner != null)
        {
            SnarkPageRelicTrackerModifier.MarkAppeared<BankersPageRelic>(Owner);
        }

        return Task.CompletedTask;
    }

    public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner)
        {
            return false;
        }

        bool modified = false;
        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] is not CardReward cardReward)
            {
                continue;
            }

            int goldAmount = cardReward.Cards
                .Select(GetBaseMerchantPrice)
                .DefaultIfEmpty(CommonCardPrice)
                .Max();
            rewards[i] = new GoldReward(goldAmount, player);
            modified = true;
        }

        if (modified)
        {
            Flash();
        }

        return modified;
    }

    private static int GetBaseMerchantPrice(CardModel card)
    {
        int price = card.Rarity switch
        {
            CardRarity.Rare => RareCardPrice,
            CardRarity.Uncommon => UncommonCardPrice,
            _ => CommonCardPrice
        };

        return card.Pool is ColorlessCardPool
            ? Mathf.RoundToInt(price * ColorlessPriceMultiplier)
            : price;
    }
}
