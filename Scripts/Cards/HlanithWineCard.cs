using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Implements the Hlanith Wine card.</summary>
[RegisterCard(typeof(EventCardPool))]
public class HlanithWineCard : ModCardTemplate
{
    private const int Cost = 2;
    private const int UpgradedCost = 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Ethereal,
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/HlanithWineCard.png"
    );

    public HlanithWineCard() : base(Cost, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return PowerCmd.Apply<HlanithWineExtraTurnPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(UpgradedCost - Cost);
    }
}
