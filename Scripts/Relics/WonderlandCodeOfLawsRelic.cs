using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Grants Order No. 42 without adding the relic to an Ancient's options yet.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class WonderlandCodeOfLawsRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/WonderlandCodeOfLawsRelic.png";
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool HasUponPickupEffect => true;
    public override bool IsAllowed(IRunState runState) => false;
    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath, IconOutlinePath: RelicIconPath, BigIconPath: RelicIconPath);
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<NumberedOrderCard>();

    public override async Task AfterObtained()
    {
        var card = Owner.RunState.CreateCard<NumberedOrderCard>(Owner);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.Add(card, PileType.Deck, CardPilePosition.Top, this, false), 2f);
    }
}
