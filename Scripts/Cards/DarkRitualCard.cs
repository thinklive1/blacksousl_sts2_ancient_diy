using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using Sts2Diy.Scripts.Powers;

namespace Sts2Diy.Scripts.Cards;

/// <summary>Trades maximum HP for recurring Osty summons.</summary>
[RegisterCard(typeof(NecrobinderCardPool))]
public sealed class DarkRitualCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://sts2diy/assets/images/cards/DarkRitualCard.jpg");

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new SummonVar(8m)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.SummonDynamic, DynamicVars.Summon)];

    public DarkRitualCard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay) =>
        PowerCmd.Apply<DarkRitualPower>(context, Owner.Creature,
            DynamicVars.Summon.BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars.Summon.UpgradeValueBy(2m);
}
