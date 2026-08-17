using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Provides four combat-only right-click discard-and-draw actions.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class QueenOfHeartsRedDeckRelic : ModRelicTemplate
{
    public const int DiscardsPerCombat = 4;
    private const string RelicIconPath =
        "res://bs_ancient/assets/images/relics/QueenOfHeartsRedDeckRelic.png";

    private int _remainingDiscards;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override int DisplayAmount => _remainingDiscards;

    [SavedProperty]
    public int BlackSouls_RemainingDiscards
    {
        get => _remainingDiscards;
        set
        {
            AssertMutable();
            _remainingDiscards = Math.Clamp(value, 0, DiscardsPerCombat);
        }
    }

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath);

    public override bool IsAllowed(IRunState runState)
    {
        // Lorina awards this relic directly; keep it out of random event relic pools.
        return false;
    }

    public override Task BeforeCombatStart()
    {
        BlackSouls_RemainingDiscards = DiscardsPerCombat;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    internal bool CanDiscardAndDraw(CardModel card)
    {
        return _remainingDiscards > 0
            && card.Owner == Owner
            && card.Pile?.Type == PileType.Hand
            && Owner.Creature.CombatState != null
            && !CombatManager.Instance.IsOverOrEnding;
    }

    internal async Task TryDiscardAndDraw(CardModel card, PlayerChoiceContext choiceContext)
    {
        if (!CanDiscardAndDraw(card))
        {
            return;
        }

        BlackSouls_RemainingDiscards--;
        InvokeDisplayAmountChanged();
        Flash();
        await CardCmd.DiscardAndDraw(choiceContext, [card], 1);
    }
}
