using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
public class PrincessFrogFavorRelic : ModRelicTemplate
{
    private const int PenaltyChance = 20;
    private const int PenaltyAmount = 1;

    private bool _isApplyingRelicEffect;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Chance", PenaltyChance),
        new DynamicVar("Penalty", PenaltyAmount)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this),
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<FrailPower>()
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/PrincessFrogFavorRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/PrincessFrogFavorRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/PrincessFrogFavorRelic.png"
    );

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_isApplyingRelicEffect || !IsOwnerDebuffingEnemy(power, amount, applier, power.Owner))
        {
            return;
        }

        Flash();
        _isApplyingRelicEffect = true;
        try
        {
            await ApplyExtraDebuff(choiceContext, power, amount, cardSource);
            if (Owner.RunState.Rng.Niche.NextInt(100) < DynamicVars["Chance"].BaseValue)
            {
                await ApplyRandomPenalty(choiceContext);
            }
        }
        finally
        {
            _isApplyingRelicEffect = false;
        }
    }

    private bool IsOwnerDebuffingEnemy(PowerModel power, decimal amount, Creature? giver, Creature? target)
    {
        return giver == Owner.Creature
            && target is not null
            && target.Side != Owner.Creature.Side
            && amount > 0m
            && power.IsVisible
            && power.GetTypeForAmount(amount) == PowerType.Debuff;
    }

    private Task ApplyExtraDebuff(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, CardModel? cardSource)
    {
        PowerModel extraPower = ModelDb.GetById<PowerModel>(power.Id).ToMutable();
        return PowerCmd.Apply(choiceContext, extraPower, power.Owner, amount, Owner.Creature, cardSource, false);
    }

    private Task ApplyRandomPenalty(PlayerChoiceContext choiceContext)
    {
        decimal amount = DynamicVars["Penalty"].BaseValue;
        return Owner.RunState.Rng.Niche.NextInt(3) switch
        {
            0 => PowerCmd.Apply<WeakPower>(choiceContext, Owner.Creature, amount, Owner.Creature, null, false),
            1 => PowerCmd.Apply<VulnerablePower>(choiceContext, Owner.Creature, amount, Owner.Creature, null, false),
            _ => PowerCmd.Apply<FrailPower>(choiceContext, Owner.Creature, amount, Owner.Creature, null, false)
        };
    }
}
