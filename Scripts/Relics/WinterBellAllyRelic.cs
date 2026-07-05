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
public class WinterBellAllyRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<GerdaCard>()
            .Prepend(RelicHoverTipHelpers.Details(this))
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<FlorenceCard>())
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<GhostHunterCard>());

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/WinterBellAllyRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/WinterBellAllyRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/WinterBellAllyRelic.png"
    );

    public override async Task BeforeCombatStart()
    {
        if (Owner.Creature.CombatState == null)
        {
            return;
        }

        Flash();

        CardModel[] cards = [
            Owner.Creature.CombatState.CreateCard<GerdaCard>(Owner),
            Owner.Creature.CombatState.CreateCard<FlorenceCard>(Owner),
            Owner.Creature.CombatState.CreateCard<GhostHunterCard>(Owner)
        ];

        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardsToCombat(
            cards,
            PileType.Draw,
            Owner,
            CardPilePosition.Random));
    }
}
