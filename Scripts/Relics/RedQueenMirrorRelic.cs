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

[RegisterRelic(typeof(EventRelicPool))]
public class RedQueenMirrorRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<RedQueenMirrorCard>()
            .Append(RelicHoverTipHelpers.Details(this));

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count <= 1;
    }

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/RedQueenMirrorRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/RedQueenMirrorRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/RedQueenMirrorRelic.png"
    );

    public override async Task AfterObtained()
    {
        if (Owner.RunState.Players.Count > 1)
        {
            return;
        }

        CardModel card = Owner.RunState.CreateCard<RedQueenMirrorCard>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck, MegaCrit.Sts2.Core.Entities.Cards.CardPilePosition.Top, this, false), 2f);
    }
}
