using BlackSouls.Scripts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Implements the Stagnant Gear card.</summary>
[RegisterCard(typeof(EventCardPool))]
public class StagnantGearCard : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(MyKeywords.Encore)];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/StagnantGearCard.jpg"
    );

    public StagnantGearCard() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? selected = (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1, 1),
                card => card != this && !card.Keywords.Contains(MyKeywords.Encore),
                this))
            .FirstOrDefault();

        if (selected == null)
        {
            return;
        }

        CardCmd.ApplyKeyword(selected, MyKeywords.Encore);
        await EncoreNextTurnPower.AddTrackedCard(Owner, selected);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
