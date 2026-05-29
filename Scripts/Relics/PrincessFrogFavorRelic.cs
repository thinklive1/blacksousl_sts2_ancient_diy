using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
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
    private const int PenaltyChance = 30;
    private const int PenaltyAmount = 1;

    private bool _isApplyingPenalty;

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

    public override decimal ModifyPowerAmountGiven(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (!IsOwnerDebuffingEnemy(power, amount, giver, target))
        {
            return amount;
        }

        return amount * 2m;
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_isApplyingPenalty || !IsOwnerDebuffingEnemy(power, amount, applier, power.Owner))
        {
            return;
        }

        if (Owner.RunState.Rng.Niche.NextInt(100) >= DynamicVars["Chance"].BaseValue)
        {
            return;
        }

        Flash();
        _isApplyingPenalty = true;
        try
        {
            await ApplyRandomPenalty();
        }
        finally
        {
            _isApplyingPenalty = false;
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

    private Task ApplyRandomPenalty()
    {
        decimal amount = DynamicVars["Penalty"].BaseValue;
        return Owner.RunState.Rng.Niche.NextInt(3) switch
        {
            0 => PowerCmd.Apply<WeakPower>(Owner.Creature, amount, Owner.Creature, null),
            1 => PowerCmd.Apply<VulnerablePower>(Owner.Creature, amount, Owner.Creature, null),
            _ => PowerCmd.Apply<FrailPower>(Owner.Creature, amount, Owner.Creature, null)
        };
    }
}
