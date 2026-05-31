using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class StageEndRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<StageEndCard>()
            .Append(HoverTipFactory.FromPower<MadnessPower>());

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/StageEndRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/StageEndRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/StageEndRelic.png"
    );

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count <= 1;
    }

    public override async Task AfterObtained()
    {
        if (Owner.RunState.Players.Count > 1)
        {
            return;
        }

        CardModel card = Owner.RunState.CreateCard<StageEndCard>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck, source: this), 2f);
    }
}
