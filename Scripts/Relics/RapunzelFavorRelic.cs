using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public class RapunzelFavorRelic : ModRelicTemplate
{
    private const int DrawLoss = 1;
    private const int TurnsPerExtraTurn = 3;

    private int _ownerTurnsEnded;
    private bool _extraTurnPending;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    public override int DisplayAmount => TurnsPerExtraTurn - (BlackSouls_OwnerTurnsEnded % TurnsPerExtraTurn);

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count <= 1;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar("DrawLoss", DrawLoss),
        new DynamicVar("Turns", TurnsPerExtraTurn)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/RapunzelFavorRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/RapunzelFavorRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/RapunzelFavorRelic.png"
    );

    [SavedProperty]
    public int BlackSouls_OwnerTurnsEnded
    {
        get => _ownerTurnsEnded;
        set
        {
            AssertMutable();
            _ownerTurnsEnded = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        return player == Owner ? Math.Max(0m, count - DynamicVars["DrawLoss"].BaseValue) : count;
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Creature.Side)
        {
            return Task.CompletedTask;
        }

        BlackSouls_OwnerTurnsEnded++;
        if (BlackSouls_OwnerTurnsEnded % TurnsPerExtraTurn == 0)
        {
            _extraTurnPending = true;
        }

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
        _extraTurnPending = false;
        return Task.CompletedTask;
    }
}
