using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public class AliceCurseRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<AliceCurseCard>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/AliceCurseRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/AliceCurseRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/AliceCurseRelic.png"
    );

    public override async Task AfterObtained()
    {
        List<CardModel> curses = PileType.Deck.GetPile(Owner).Cards
            .Where(IsTargetCurse)
            .ToList();

        if (curses.Count == 0)
        {
            return;
        }

        Flash();
        List<CardModel> replacements = curses
            .Select(_ => Owner.RunState.CreateCard<AliceCurseCard>(Owner))
            .ToList<CardModel>();

        await CardPileCmd.RemoveFromDeck(curses, showPreview: false);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(replacements, PileType.Deck, MegaCrit.Sts2.Core.Entities.Cards.CardPilePosition.Top, this, false), 2f);
    }

    private static bool IsTargetCurse(CardModel card)
    {
        return card.Type == CardType.Curse && card is not AliceCurseCard;
    }
}
