using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
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
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck, MegaCrit.Sts2.Core.Entities.Cards.CardPilePosition.Top, this, false), 2f);
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner.Creature.GetPower<EncoreNextTurnPower>() == null)
        {
            await PowerCmd.Apply<EncoreNextTurnPower>(
                new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(),
                Owner.Creature,
                1,
                Owner.Creature,
                null,
                silent: true);
        }

        if (Owner.Creature.GetPower<EncoreNextTurnVisualPower>() == null)
        {
            await PowerCmd.Apply<EncoreNextTurnVisualPower>(
                new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(),
                Owner.Creature,
                0,
                Owner.Creature,
                null,
                silent: true);
        }
    }
}
