using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the King With Donkey Ears relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class KingWithDonkeyEarsRelic : ModRelicTemplate
{
    private const int BossDamagePercent = 50;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("BossDamagePercent", BossDamagePercent)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner.RunState.CurrentRoom is not CombatRoom { RoomType: RoomType.Boss }
            || target is null
            || target.Side == Owner.Creature.Side
            || dealer != Owner.Creature
            || !props.IsPoweredAttack())
        {
            return 1m;
        }

        return 1m + DynamicVars["BossDamagePercent"].BaseValue / 100m;
    }
}
