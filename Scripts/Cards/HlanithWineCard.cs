using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

[RegisterCard(typeof(EventCardPool))]
public class HlanithWineCard : ModCardTemplate
{
    private const int Cost = 3;
    private const int UpgradedCost = 2;

    private bool _extraTurnPending;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Ethereal
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/HlanithWineCard.png"
    );

    public HlanithWineCard() : base(Cost, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _extraTurnPending = true;
        return Task.CompletedTask;
    }

    public override bool ShouldTakeExtraTurn(Player player)
    {
        return player == Owner && _extraTurnPending;
    }

    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player == Owner)
        {
            _extraTurnPending = false;
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(UpgradedCost - Cost);
    }
}
