using BlackSouls.Scripts.Cards;
using Blacksouls.Scripts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class StagnantGearRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<StagnantGearCard>()
            .Append(HoverTipFactory.FromKeyword(MyKeywords.Encore));

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/StagnantGearRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/StagnantGearRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/StagnantGearRelic.png"
    );

    public override async Task AfterObtained()
    {
        CardModel card = Owner.RunState.CreateCard<StagnantGearCard>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck, source: this), 2f);
    }
}
