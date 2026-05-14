using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class QuillPenRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/TimeQueenBlessingRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/TimeQueenBlessingRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/TimeQueenBlessingRelic.png"
    );

    public override async Task AfterObtained()
    {
        CardModel card = Owner.RunState.CreateCard<PowerOfRewrite>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck, source: this), 2f);
    }
}
