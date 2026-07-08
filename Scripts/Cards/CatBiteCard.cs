using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Implements the Cat Bite card.</summary>
[RegisterCard(typeof(EventCardPool))]
public class CatBiteCard : ModCardTemplate
{
    private const int Damage = 15;
    private const int UpgradeDamage = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(Damage, ValueProp.Move)
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/CardBiteCard.png"
    );

    public CatBiteCard() : base(0, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
        {
            return;
        }

        CatCollarRelic? collar = Owner.GetRelic<CatCollarRelic>();
        if (collar == null || !await collar.TryTriggerCatCardEffect("BiteTrigger"))
        {
            return;
        }

        if (cardPlay.Target.Block > 0)
        {
            await CreatureCmd.LoseBlock(cardPlay.Target, cardPlay.Target.Block);
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitVfxNode((Creature target) => NScratchVfx.Create(target, goingRight: true))
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamage);
    }
}
