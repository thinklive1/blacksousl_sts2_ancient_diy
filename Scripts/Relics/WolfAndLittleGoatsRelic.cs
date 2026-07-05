using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class WolfAndLittleGoatsRelic : ModRelicTemplate
{
    private const int RetainedTurns = 3;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private int _ownerTurnCount;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<WolfAndLittleGoatsPower>()
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

    public override Task BeforeCombatStart()
    {
        _ownerTurnCount = 0;
        return PowerCmd.Apply<WolfAndLittleGoatsPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            RetainedTurns,
            Owner.Creature,
            null,
            false);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        _ownerTurnCount++;
        if (_ownerTurnCount == RetainedTurns)
        {
            await DamageAllCreatures(choiceContext);
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _ownerTurnCount = 0;
        return Task.CompletedTask;
    }

    public override bool ShouldClearBlock(Creature creature)
    {
        return creature != Owner.Creature || _ownerTurnCount <= 0 || _ownerTurnCount > RetainedTurns;
    }

    private async Task DamageAllCreatures(PlayerChoiceContext choiceContext)
    {
        decimal damage = Owner.Creature.Block;
        if (damage <= 0 || Owner.Creature.CombatState == null)
        {
            return;
        }

        Flash();
        IReadOnlyList<Creature> targets = Owner.Creature.CombatState.Creatures
            .Where(creature => creature.IsAlive)
            .ToList();
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            targets,
            damage,
            ValueProp.Unpowered,
            Owner.Creature,
            null);
    }
}
