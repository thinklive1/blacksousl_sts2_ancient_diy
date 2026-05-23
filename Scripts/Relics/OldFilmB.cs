using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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

[RegisterRelic(typeof(SharedRelicPool))]
public class OldFilmB : ModRelicTemplate
{
    private const int VulnerableAmount = 3;

    private readonly HashSet<Creature> _triggeredTargets = [];

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<VulnerablePower>(VulnerableAmount)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this),
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/OldFilmB.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/OldFilmB.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/OldFilmB.png"
    );

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (!ShouldTrigger(dealer, result, props, target))
        {
            return;
        }

        Flash();
        await CreatureCmd.Stun(target);
        await PowerCmd.Apply<VulnerablePower>(
            target,
            DynamicVars["VulnerablePower"].BaseValue,
            Owner.Creature,
            null);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _triggeredTargets.Clear();
        return Task.CompletedTask;
    }

    private bool ShouldTrigger(Creature? dealer, DamageResult result, ValueProp props, Creature target)
    {
        return (dealer == Owner.Creature || dealer?.PetOwner?.Creature == Owner.Creature)
            && props.IsPoweredAttack()
            && result.TotalDamage > 0
            && target.IsAlive
            && target.Side != Owner.Creature.Side
            && target.Monster is { IntendsToAttack: false }
            && _triggeredTargets.Add(target);
    }
}
