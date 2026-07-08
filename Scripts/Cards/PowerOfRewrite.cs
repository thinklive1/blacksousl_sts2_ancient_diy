using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Implements the Power of Rewrite card.</summary>
[RegisterCard(typeof(EventCardPool))]
public class PowerOfRewrite : ModCardTemplate
{
    private const int Damage = 39;
    private const int UpgradeDamage = 9;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Retain,
        CardKeyword.Exhaust
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(Damage, ValueProp.Move)
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/PowerOfRewrite.png"
    );

    public PowerOfRewrite() : base(3, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        BsAncientAudio.PlayOneShot(BsAncientAudio.Write);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .Execute(choiceContext);

        List<PowerModel> debuffs = Owner.Creature.Powers
            .Where(power => power.TypeForCurrentAmount == PowerType.Debuff)
            .ToList();
        SurroundedPower.Direction? surroundedFacing = debuffs
            .OfType<SurroundedPower>()
            .FirstOrDefault()
            ?.Facing;

        foreach (PowerModel debuff in debuffs)
        {
            await PowerCmd.Remove(debuff);
        }

        if (surroundedFacing != null)
        {
            SurroundedVisualPower? visualPower = await PowerCmd.Apply<SurroundedVisualPower>(
                choiceContext,
                Owner.Creature,
                1,
                Owner.Creature,
                this,
                silent: true);
            visualPower?.SetFacing(surroundedFacing.Value);
        }

    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamage);
    }
}
