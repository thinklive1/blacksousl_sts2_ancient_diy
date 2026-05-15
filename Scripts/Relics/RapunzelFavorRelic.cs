using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class RapunzelFavorRelic : ModRelicTemplate
{
    private const int DrawLoss = 2;
    private const int TurnsPerExtraTurn = 3;

    private int _ownerTurnsEnded;
    private bool _extraTurnPending;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    public override int DisplayAmount => TurnsPerExtraTurn - (_ownerTurnsEnded % TurnsPerExtraTurn);

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar("DrawLoss", DrawLoss),
        new DynamicVar("Turns", TurnsPerExtraTurn)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/RapunzelFavorRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/RapunzelFavorRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/RapunzelFavorRelic.png"
    );

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        return player == Owner ? Math.Max(0m, count - DynamicVars["DrawLoss"].BaseValue) : count;
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Creature.Side)
        {
            return Task.CompletedTask;
        }

        _ownerTurnsEnded++;
        if (_ownerTurnsEnded % TurnsPerExtraTurn == 0)
        {
            _extraTurnPending = true;
        }

        InvokeDisplayAmountChanged();
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

        Flash();
        _extraTurnPending = false;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        _ownerTurnsEnded = 0;
        _extraTurnPending = false;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
