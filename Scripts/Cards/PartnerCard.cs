using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Sts2Diy.Scripts.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Sts2Diy.Scripts.Cards;

/// <summary>Lets a living Osty absorb one full hit at the cost of temporarily blocking Summon.</summary>
[RegisterCard(typeof(NecrobinderCardPool))]
public sealed class PartnerCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://sts2diy/assets/images/cards/PartnerCard.jpg");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Turns", 2m)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<PartnerGuardPower>(), HoverTipFactory.FromPower<NoSummonPower>()
    ];
    protected override bool IsPlayable => IsMutable && Owner?.Creature is { IsAlive: true }
        && Owner.Osty is { IsAlive: true };

    public PartnerCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) => card != this || IsPlayable;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!IsPlayable) return;
        await PowerCmd.Apply<PartnerGuardPower>(context, Owner.Creature, 1m, Owner.Creature, this);
        await PowerCmd.Apply<NoSummonPower>(
            context, Owner.Creature, DynamicVars["Turns"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Turns"].UpgradeValueBy(-1m);
}
