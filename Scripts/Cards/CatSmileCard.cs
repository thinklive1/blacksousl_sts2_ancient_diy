using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Implements the Cat Smile card.</summary>
[RegisterCard(typeof(EventCardPool))]
public class CatSmileCard : ModCardTemplate
{
    private const int Block = 4;
    private const int UpgradeBlock = 2;
    private const int PlaysForIntangible = 2;
    private const int Intangible = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(Block, ValueProp.Move),
        new DynamicVar("PlaysForIntangible", PlaysForIntangible),
        new PowerVar<IntangiblePower>(Intangible)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<IntangiblePower>()
    ];

    public override bool GainsBlock => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/CatSmileCard.png"
    );

    public CatSmileCard() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CatCollarRelic? collar = Owner.GetRelic<CatCollarRelic>();
        if (collar == null || !await collar.TryTriggerCatCardEffect("SmileTrigger"))
        {
            return;
        }

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, DynamicVars.Block.Props, cardPlay);

        if (await collar.RecordSmileAndShouldGainIntangible())
        {
            await PowerCmd.Apply<IntangiblePower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["IntangiblePower"].BaseValue,
                Owner.Creature,
                this,
                false);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlock);
    }
}
