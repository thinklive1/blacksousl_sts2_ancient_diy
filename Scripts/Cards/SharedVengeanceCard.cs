using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.ValueProps;

namespace Sts2Diy.Scripts.Cards;

/// <summary>Player and Osty attack in sequence; the owner's Osty deaths lower its combat cost.</summary>
[RegisterCard(typeof(NecrobinderCardPool))]
public sealed class SharedVengeanceCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://sts2diy/assets/images/cards/SharedVengeanceCard.jpg");
    protected override HashSet<CardTag> CanonicalTags => [CardTag.OstyAttack];
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6m, ValueProp.Move),
        new OstyDamageVar(9m, ValueProp.Move)
    ];

    public SharedVengeanceCard() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_slash").Execute(context);

        // The first attack can kill the target or trigger reactions that remove Osty.
        if (CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead || cardPlay.Target.IsDead
            || Owner.Osty is not { IsAlive: true } osty) return;

        await DamageCmd.Attack(DynamicVars.OstyDamage.BaseValue).FromOsty(osty, this, cardPlay)
            .Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3").Execute(context);
    }

    public override Task AfterDeath(PlayerChoiceContext context, Creature creature,
#if !STS2DIY_STABLE_1110
        CardModel? cardSource,
#endif
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (!wasRemovalPrevented && creature == Owner.Osty && Pile is { IsCombatPile: true })
        {
            EnergyCost.AddThisCombat(-1);
        }
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.OstyDamage.UpgradeValueBy(3m);
    }
}
