using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Daddy Long Legs relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class DaddyLongLegsRelic : ModRelicTemplate
{
    private const int BlockAmount = 8;
    private const int ActiveTurns = 3;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private int _ownerTurnCount;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Block", BlockAmount),
        new DynamicVar("Turns", ActiveTurns)
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
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        _ownerTurnCount++;
        if (_ownerTurnCount > ActiveTurns)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars["Block"].BaseValue,
            ValueProp.Unpowered,
            null);
    }
}
