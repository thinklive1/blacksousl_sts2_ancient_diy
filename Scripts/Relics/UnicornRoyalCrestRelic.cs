using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public class UnicornRoyalCrestRelic : ModRelicTemplate
{
    private const int DexterityAmount = 2;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/UnicornRoyalCrestRelic.png";

    private readonly Dictionary<(Creature Target, Creature Dealer), decimal> _incomingAttacks = [];
    private bool _isCounterattacking;

    internal static bool IsCounterattacking { get; private set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DexterityPower>(DexterityAmount)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override async Task BeforeCombatStart()
    {
        _incomingAttacks.Clear();
        _isCounterattacking = false;
        Flash();
        await PowerCmd.Apply<DexterityPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["DexterityPower"].BaseValue,
            Owner.Creature,
            null,
            false);
    }

    public override Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!_isCounterattacking && IsProtectedTarget(target) && IsEnemyAttack(dealer, props) && amount > 0m)
        {
            _incomingAttacks[(target, dealer!)] = amount;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (_isCounterattacking || !IsProtectedTarget(target) || !IsEnemyAttack(dealer, props) || result.UnblockedDamage > 0)
        {
            return;
        }

        decimal counterDamage = GetCounterDamage(target, dealer!, result);
        if (counterDamage <= 0m || dealer!.IsDead)
        {
            return;
        }

        Flash();
        _isCounterattacking = true;
        IsCounterattacking = true;
        try
        {
            await CreatureCmd.Damage(
                choiceContext,
                dealer,
                counterDamage,
                ValueProp.Move,
                Owner.Creature,
                null);
        }
        finally
        {
            _isCounterattacking = false;
            IsCounterattacking = false;
        }
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        _incomingAttacks.Clear();
        _isCounterattacking = false;
        return Task.CompletedTask;
    }

    private decimal GetCounterDamage(Creature target, Creature dealer, DamageResult result)
    {
        if (_incomingAttacks.TryGetValue((target, dealer), out decimal incomingDamage))
        {
            return incomingDamage;
        }

        return result.TotalDamage;
    }

    private bool IsProtectedTarget(Creature target)
    {
        return target == Owner.Creature || target == Owner.Osty;
    }

    private bool IsEnemyAttack(Creature? dealer, ValueProp props)
    {
        return dealer is { IsDead: false }
            && dealer.Side != Owner.Creature.Side
            && props.IsPoweredAttack();
    }
}
