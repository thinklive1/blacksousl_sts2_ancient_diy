using Blacksouls.Scripts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

[RegisterCard(typeof(EventCardPool))]
public class StagnantGearCard : ModCardTemplate
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(MyKeywords.Encore)];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/StagnantGearCard.png"
    );

    public StagnantGearCard() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
        AddKeyword(CardKeyword.Exhaust);
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

        int priorPlays = GetPriorPlayCount(selected);
        selected.AddKeyword(MyKeywords.Encore);
        await EncoreNextTurnPower.AddTrackedCard(Owner, selected, priorPlays);
        NCardEnchantVfx? vfx = NCardEnchantVfx.Create(selected);
        if (vfx != null)
        {
            NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
        }
    }

    private static int GetPriorPlayCount(CardModel card)
    {
        return CombatManager.Instance.History.CardPlaysFinished
            .Count(entry => entry.CardPlay.Card == card);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
