using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class MabelSoldierRelic : ModRelicTemplate
{
    private const int FirstActIndex = 0;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/MabelSoldierRelic.png";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<AscensionEnchantment>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count == 1;
    }

    public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || Owner.RunState.CurrentActIndex != FirstActIndex)
        {
            return false;
        }

        bool modified = false;
        AscensionEnchantment ascension = ModelDb.Enchantment<AscensionEnchantment>();
        foreach (CardReward cardReward in rewards.OfType<CardReward>())
        {
            foreach (CardModel card in cardReward.Cards)
            {
                if (card.Enchantment == null && CanReceiveMabelAscension(card, ascension))
                {
                    CardCmd.Enchant<AscensionEnchantment>(card, 1m);
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

    private static bool CanReceiveMabelAscension(CardModel card, AscensionEnchantment ascension)
    {
        return card.Rarity is CardRarity.Basic or CardRarity.Common or CardRarity.Uncommon
            && ascension.CanEnchant(card);
    }
}
