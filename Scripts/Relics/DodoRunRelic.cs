using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class DodoRunRelic : ModRelicTemplate
{
    private const int HealthThreshold = 20;

    private bool _wasUsed;
    private bool _shouldClearSkippedCombatRewards;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool IsUsedUp => BlackSouls_WasUsed;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Health", HealthThreshold)];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/DodoRunRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/DodoRunRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/DodoRunRelic.png"
    );

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count == 1;
    }

    [SavedProperty]
    public bool BlackSouls_WasUsed
    {
        get => _wasUsed;
        set
        {
            AssertMutable();
            _wasUsed = value;
            if (IsUsedUp)
            {
                Status = RelicStatus.Disabled;
            }
        }
    }

    public override async Task BeforeCombatStart()
    {
        if (!ShouldSkipCombat())
        {
            return;
        }

        ICombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        Flash();
        BlackSouls_WasUsed = true;
        _shouldClearSkippedCombatRewards = true;

        foreach (Creature enemy in combatState.Enemies.ToList())
        {
            await CreatureCmd.Escape(enemy);
        }

        await CombatManager.Instance.CheckWinCondition();
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || !_shouldClearSkippedCombatRewards || room is not CombatRoom)
        {
            return false;
        }

        rewards.Clear();
        _shouldClearSkippedCombatRewards = false;
        return true;
    }

    private bool ShouldSkipCombat()
    {
        return !BlackSouls_WasUsed
            && Owner.RunState.Players.Count == 1
            && Owner.Creature.CurrentHp < DynamicVars["Health"].IntValue
            && Owner.RunState.CurrentRoom is CombatRoom { RoomType: not RoomType.Boss };
    }
}
