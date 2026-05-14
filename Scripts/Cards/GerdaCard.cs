using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public class GerdaCard : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Unplayable
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<UndeadPower>(1)
    ];


    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<UndeadPower>(),
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/GerdaCard.jpg"
    );

    public GerdaCard() : base(-1, CardType.Skill, CardRarity.Common, TargetType.None)
    {
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this)
        {
            return;
        }

        await CardCmd.Exhaust(choiceContext, this);
        await PowerCmd.Apply<UndeadPower>(Owner.Creature, DynamicVars["UndeadPower"].BaseValue, Owner.Creature, this);
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }
}
