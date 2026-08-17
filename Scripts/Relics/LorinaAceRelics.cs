using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Adds the corresponding ace card to the deck when obtained.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class LastAceRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<LastAceCard>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/LastAceRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/LastAceRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/LastAceRelic.png");

    public override bool IsAllowed(IRunState runState) => false;

    public override async Task AfterObtained()
    {
        CardModel card = Owner.RunState.CreateCard<LastAceCard>(Owner);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.Add(card, PileType.Deck, CardPilePosition.Top, this, false),
            2f);
    }
}

/// <summary>Enchants the basic Strike and Defend cards when obtained.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class FlippedAceRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/FlippedAceRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/FlippedAceRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/FlippedAceRelic.png");

    public override bool IsAllowed(IRunState runState) => false;

    public override Task AfterObtained()
    {
        foreach (CardModel card in Owner.Deck.Cards)
        {
            if (card.Tags.Contains(CardTag.Strike) || card.Tags.Contains(CardTag.Defend))
            {
                PlayingCardSuitEnchantment.TryEnchantRandomSuit(card, minRank: 8, maxRank: 13);
            }
        }

        Flash();
        return Task.CompletedTask;
    }
}

/// <summary>Adds the corresponding ace card to the deck when obtained.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class ReversedAceRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<ReversedAceCard>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/ReversedAceRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/ReversedAceRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/ReversedAceRelic.png");

    public override bool IsAllowed(IRunState runState) => false;

    public override async Task AfterObtained()
    {
        CardModel card = Owner.RunState.CreateCard<ReversedAceCard>(Owner);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.Add(card, PileType.Deck, CardPilePosition.Top, this, false),
            2f);
    }
}
