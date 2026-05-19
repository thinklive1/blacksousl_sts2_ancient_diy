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

[RegisterCard(typeof(EventCardPool))]
public class StageEndCard : ModCardTemplate
{
    private const int CardsUntilDeath = 8;
    private const int MadnessGain = 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Ethereal,
        CardKeyword.Exhaust
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(CardsUntilDeath),
        new PowerVar<MadnessPower>(MadnessGain)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<MadnessPower>(),
        HoverTipFactory.FromPower<StageEndCountdownPower>()
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/StageEndCard.png"
    );

    public StageEndCard() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await RefillHand(choiceContext);
        await RefillEnergy();
        await PowerCmd.Apply<MadnessPower>(Owner.Creature, DynamicVars["MadnessPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<StageEndCountdownPower>(Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);
    }

    private async Task RefillHand(PlayerChoiceContext choiceContext)
    {
        if (Owner.PlayerCombatState is null)
        {
            return;
        }

        int cardsToDraw = Math.Max(0, 10 - Owner.PlayerCombatState.Hand.Cards.Count);
        if (cardsToDraw > 0)
        {
            await CardPileCmd.Draw(choiceContext, cardsToDraw, Owner);
        }
    }

    private async Task RefillEnergy()
    {
        if (Owner.PlayerCombatState is null)
        {
            return;
        }

        int energyToGain = Owner.PlayerCombatState.MaxEnergy - Owner.PlayerCombatState.Energy;
        if (energyToGain > 0)
        {
            await PlayerCmd.GainEnergy(energyToGain, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}
