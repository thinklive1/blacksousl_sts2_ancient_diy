using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Red Queen Promotion relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public class RedQueenPromotionRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/RedQueenPromotionRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/RedQueenPromotionRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/RedQueenPromotionRelic.png"
    );

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count == 1;
    }

    public override async Task BeforeCombatStart()
    {
        if (!ShouldReduceEnemiesToOneHp())
        {
            return;
        }

        ICombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        Flash();
        foreach (Creature enemy in combatState.HittableEnemies)
        {
            await CreatureCmd.SetCurrentHp(enemy, 1m);
        }
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (creature.Side != CombatSide.Enemy || !ShouldReduceEnemiesToOneHp())
        {
            return;
        }

        Flash();
        await CreatureCmd.SetCurrentHp(creature, 1m);
    }

    private bool ShouldReduceEnemiesToOneHp()
    {
        return Owner.RunState.CurrentRoom is CombatRoom { RoomType: not RoomType.Boss };
    }
}
