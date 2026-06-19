using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class FoxAndSourGrapesRelic : ModRelicTemplate
{
    private const int EnvenomAmount = 1;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private readonly HashSet<Creature> _affectedCreatures = [];

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<EnvenomPower>(EnvenomAmount)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<EnvenomPower>(),
        HoverTipFactory.FromPower<PoisonPower>()
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

    public override async Task BeforeCombatStart()
    {
        _affectedCreatures.Clear();

        if (Owner.Creature.CombatState == null)
        {
            return;
        }

        Flash();
        foreach (Creature creature in Owner.Creature.CombatState.Creatures)
        {
            await ApplyEnvenom(creature);
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _affectedCreatures.Clear();
        return Task.CompletedTask;
    }

    public override Task AfterCreatureAddedToCombat(Creature creature)
    {
        return ApplyEnvenom(creature);
    }

    private async Task ApplyEnvenom(Creature creature)
    {
        if (!_affectedCreatures.Add(creature) || !creature.IsAlive)
        {
            return;
        }

        await PowerCmd.Apply<EnvenomPower>(
            new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(),
            creature,
            DynamicVars["EnvenomPower"].BaseValue,
            Owner.Creature,
            null,
            false);
    }
}
