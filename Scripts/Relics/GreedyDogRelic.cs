using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class GreedyDogRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Greed>();

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
        CardModel? target = SelectTransformTarget();
        if (target == null)
        {
            return;
        }

        CardModel greed = Owner.RunState.CreateCard<Greed>(Owner);
        await CardCmd.Transform(target, greed, CardPreviewStyle.HorizontalLayout);
    }

    private CardModel? SelectTransformTarget()
    {
        List<CardModel> candidates = PileType.Deck.GetPile(Owner).Cards
            .Where(IsEligible)
            .ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        CardRarity highestRarity = candidates.Max(card => card.Rarity);
        return Owner.RunState.Rng.Niche.NextItem(candidates
            .Where(card => card.Rarity == highestRarity)
            .ToList());
    }

    private static bool IsEligible(CardModel card)
    {
        return card.IsTransformable
            && card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare;
    }
}
