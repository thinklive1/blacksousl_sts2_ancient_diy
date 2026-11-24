using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
#if STS2DIY_STABLE_1110
using HitCountPreviewVar = MegaCrit.Sts2.Core.Localization.DynamicVars.CalculatedVar;
#else
using HitCountPreviewVar = MegaCrit.Sts2.Core.Localization.DynamicVars.CalculatedHitCountVar;
#endif

namespace Sts2Diy.Scripts.Cards;

/// <summary>Osty hits once plus once for each living creature with Doom, regardless of side.</summary>
[RegisterCard(typeof(NecrobinderCardPool))]
public sealed class NegativeEnergyInfusionCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://sts2diy/assets/images/cards/NegativeEnergyInfusionCard.jpg");
    protected override HashSet<CardTag> CanonicalTags => [CardTag.OstyAttack];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DoomPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new OstyDamageVar(6m, ValueProp.Move),
        new CalculationBaseVar(1m),
        new CalculationExtraVar(1m),
        new HitCountPreviewVar("CalculatedHits").WithMultiplier(static (card, _) =>
            card.CombatState?.Creatures.Count(creature => creature.IsAlive
                && creature.GetPowerAmount<DoomPower>() > 0) ?? 0)
    ];

    public NegativeEnergyInfusionCard() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead || cardPlay.Target.IsDead
            || Owner.Osty is not { IsAlive: true } osty) return;

        // Freeze the count before damage reactions can add Doom or remove creatures.
        int hits = (int)((CalculatedVar)DynamicVars["CalculatedHits"]).Calculate(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.OstyDamage.BaseValue).WithHitCount(hits)
            .FromOsty(osty, this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3").Execute(context);
    }

    protected override void OnUpgrade() => DynamicVars.OstyDamage.UpgradeValueBy(2m);
}
