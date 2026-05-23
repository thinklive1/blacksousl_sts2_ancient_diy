using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class CinderellaFavorRelic : ModRelicTemplate
{
    private const int StrengthLoss = 3;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<StrengthPower>(StrengthLoss)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/CinderellaFavorRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/CinderellaFavorRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/CinderellaFavorRelic.png"
    );

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Creature.Side)
        {
            return;
        }

        Flash();
        await ApplyStrengthLoss();
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Creature.Side)
        {
            return;
        }

        List<PowerModel> debuffs = Owner.Creature.Powers
            .Where(power => power.TypeForCurrentAmount == PowerType.Debuff)
            .ToList();

        if (debuffs.Count == 0)
        {
            return;
        }

        Flash();
        foreach (PowerModel debuff in debuffs)
        {
            await PowerCmd.Remove(debuff);
        }
    }

    private Task ApplyStrengthLoss()
    {
        return PowerCmd.Apply<StrengthPower>(Owner.Creature, -StrengthLoss, Owner.Creature, null);
    }
}
