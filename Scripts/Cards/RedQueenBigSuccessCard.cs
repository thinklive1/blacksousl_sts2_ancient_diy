using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Implements the Red Queen Big Success card.</summary>
[RegisterCard(typeof(EventCardPool))]
public class RedQueenBigSuccessCard : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Ethereal
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/RedQueenBigSuccessCard.jpg"
    );

    public RedQueenBigSuccessCard() : base(0, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        BsAncientAudio.PlayOneShot(BsAncientAudio.Claps);
        await PowerCmd.Apply<RedQueenDiceBigSuccessPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
