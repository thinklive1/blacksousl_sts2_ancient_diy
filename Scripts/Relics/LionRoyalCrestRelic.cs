using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class LionRoyalCrestRelic : ModRelicTemplate
{
    private const int StrengthAmount = 1;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/LionRoyalCrestRelic.png";

    private bool _isGrantingStrength;
    private bool _blockedEnemyPower;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<BufferPower>(),
        HoverTipFactory.FromPower<PlatingPower>(),
        HoverTipFactory.FromPower<IntangiblePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<StrengthPower>(StrengthAmount)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target.Side != Owner.Creature.Side && !target.HasPower<BurrowedPower>())
        {
            return 0m;
        }

        return 1m;
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (amount <= 0m || target.Side == Owner.Creature.Side || !IsForbiddenEnemyPower(canonicalPower))
        {
            return false;
        }

        modifiedAmount = 0m;
        _blockedEnemyPower = true;
        return true;
    }

    public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        if (!_blockedEnemyPower)
        {
            return;
        }

        _blockedEnemyPower = false;
        Flash();
        await Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_isGrantingStrength || !ShouldGainStrengthFromPowerChange(power, amount))
        {
            return;
        }

        Flash();
        _isGrantingStrength = true;
        try
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["StrengthPower"].BaseValue,
                Owner.Creature,
                null,
                false);
        }
        finally
        {
            _isGrantingStrength = false;
        }
    }

    private static bool IsForbiddenEnemyPower(PowerModel power)
    {
        return power is BufferPower or PlatingPower or IntangiblePower;
    }

    private bool ShouldGainStrengthFromPowerChange(PowerModel power, decimal amount)
    {
        if (amount <= 0m || !power.IsVisible || Owner?.Creature is not { } ownerCreature)
        {
            return false;
        }

        Creature powerOwner = power.Owner;
        return powerOwner.IsAlive
            && powerOwner.CombatState is not null
            && powerOwner.Side != ownerCreature.Side
            && power.Amount > 0
            && power.TypeForCurrentAmount == PowerType.Buff
            && power.GetTypeForAmount(amount) == PowerType.Buff;
    }
}
