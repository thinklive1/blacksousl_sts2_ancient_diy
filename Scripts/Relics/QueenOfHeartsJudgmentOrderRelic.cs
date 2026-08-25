using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using BlackSouls.Scripts.Services;

namespace BlackSouls.Scripts;

/// <summary>Grants an opening extra turn and delays manual Attacks until the next turn.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class QueenOfHeartsJudgmentOrderRelic : ModRelicTemplate
{
    private const string RelicIconPath =
        "res://bs_ancient/assets/images/relics/QueenOfHeartsJudgmentOrderRelic.png";

    private bool _extraTurnPending;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath);

    public override bool IsAllowed(IRunState runState) => false;

    public override Task BeforeCombatStart()
    {
        _extraTurnPending = true;
        JudgmentOrderCombatService.Reset(Owner);
        return Task.CompletedTask;
    }

    public override bool ShouldTakeExtraTurn(Player player)
    {
        return player == Owner && _extraTurnPending;
    }

    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player != Owner)
        {
            return Task.CompletedTask;
        }

        _extraTurnPending = false;
        Flash();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        return player == Owner
            ? JudgmentOrderCombatService.ResolveDelayedAttacks(choiceContext, player)
            : Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _extraTurnPending = false;
        JudgmentOrderCombatService.Reset(Owner);
        return Task.CompletedTask;
    }
}
