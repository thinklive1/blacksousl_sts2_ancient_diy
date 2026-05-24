using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

[RegisterCard(typeof(EventCardPool))]
public class RedQueenRerollCard : ModCardTemplate
{
    private const int EnergyGain = 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust,
        CardKeyword.Ethereal
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(EnergyGain)
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/RedQueenRerollCard.png"
    );

    public RedQueenRerollCard() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RedQueenDiceRelic? dice = Owner.GetRelic<RedQueenDiceRelic>();
        return dice?.Reroll(choiceContext, cardPlay.Card) ?? Task.CompletedTask;
    }
}
