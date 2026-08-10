using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Queen of Hearts' playing-card deck relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class QueenOfHeartsPlayingCardDeckRelic : ModRelicTemplate
{
    private const int EnchantChancePercent = 10;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/QueenOfHeartsPlayingCardDeckRelic.png";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner)
        {
            return false;
        }

        bool modified = false;
        foreach (CardReward cardReward in rewards.OfType<CardReward>())
        {
            foreach (CardModel card in cardReward.Cards)
            {
                if (Owner.RunState.Rng.Niche.NextInt(100) >= EnchantChancePercent || card.Enchantment != null)
                {
                    continue;
                }

                if (TryApplyRandomSuit(card))
                {
                    modified = true;
                }
            }
        }

        if (modified)
        {
            Flash();
        }

        return modified;
    }

    private bool TryApplyRandomSuit(CardModel card)
    {
        decimal amount = Owner.RunState.Rng.Niche.NextInt(PlayingCardSuitEnchantment.MaxTriggersPerCombat)
            + PlayingCardSuitEnchantment.MinTriggersPerCombat;

        return Owner.RunState.Rng.Niche.NextInt(4) switch
        {
            0 when ModelDb.Enchantment<HeartSuitEnchantment>().CanEnchant(card) => Enchant<HeartSuitEnchantment>(card, amount),
            1 when ModelDb.Enchantment<DiamondSuitEnchantment>().CanEnchant(card) => Enchant<DiamondSuitEnchantment>(card, amount),
            2 when ModelDb.Enchantment<ClubSuitEnchantment>().CanEnchant(card) => Enchant<ClubSuitEnchantment>(card, amount),
            3 when ModelDb.Enchantment<SpadeSuitEnchantment>().CanEnchant(card) => Enchant<SpadeSuitEnchantment>(card, amount),
            _ => false
        };
    }

    private static bool Enchant<T>(CardModel card, decimal amount) where T : PlayingCardSuitEnchantment, new()
    {
        CardCmd.Enchant<T>(card, amount);
        return true;
    }
}
