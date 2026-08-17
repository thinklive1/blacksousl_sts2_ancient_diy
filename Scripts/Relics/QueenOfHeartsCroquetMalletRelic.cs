using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using BlackSouls.Scripts.Cards;

namespace BlackSouls.Scripts;

/// <summary>Adds the Queen's Croquet Mallet to the deck when obtained.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class QueenOfHeartsCroquetMalletRelic : ModRelicTemplate
{
    private const string RelicIconPath =
        "res://bs_ancient/assets/images/relics/QueenOfHeartsCroquetMalletRelic.png";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<QueenOfHeartsCroquetMalletCard>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath);

    public override bool IsAllowed(IRunState runState) => false;

    public override async Task AfterObtained()
    {
        CardModel card = Owner.RunState.CreateCard<QueenOfHeartsCroquetMalletCard>(Owner);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.Add(card, PileType.Deck, CardPilePosition.Top, this, false),
            2f);
    }
}
