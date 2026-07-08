using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Mermaid Princess relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class MermaidPrincessRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<MermaidTearCard>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override async Task AfterObtained()
    {
        CardModel card = Owner.RunState.CreateCard<MermaidTearCard>(Owner);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.Add(card, PileType.Deck, CardPilePosition.Top, this, false),
            2f);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        foreach (MermaidTearCard card in PileType.Deck.GetPile(Owner).Cards.OfType<MermaidTearCard>().ToList())
        {
            await card.RemoveSwallowedDeckCards();
        }
    }
}
