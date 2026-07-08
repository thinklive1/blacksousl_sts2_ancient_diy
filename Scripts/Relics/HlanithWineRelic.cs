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

/// <summary>Implements the Hlanith Wine relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public class HlanithWineRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count <= 1;
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<HlanithWineCard>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/HlanithWineRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/HlanithWineRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/HlanithWineRelic.png"
    );

    public override async Task AfterObtained()
    {
        CardModel card = Owner.RunState.CreateCard<HlanithWineCard>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck, MegaCrit.Sts2.Core.Entities.Cards.CardPilePosition.Top, this, false), 2f);
    }
}
