using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class MysteryOfNightSkyRelic : ModRelicTemplate
{
    private const int ReplayChance = 50;

    private bool _hasCheckedFirstCardThisTurn;
    private bool _shouldReplayFirstCardThisTurn;
    private bool _replayedFirstCardThisTurn;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Chance", ReplayChance)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/MysteryOfNightSkyRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/MysteryOfNightSkyRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/MysteryOfNightSkyRelic.png"
    );

    public override Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Creature.Side)
        {
            _hasCheckedFirstCardThisTurn = false;
            _replayedFirstCardThisTurn = false;
            _shouldReplayFirstCardThisTurn = Owner.RunState.Rng.Niche.NextInt(100) < ReplayChance;
        }

        return Task.CompletedTask;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (_hasCheckedFirstCardThisTurn || card.Owner != Owner || Owner.Creature.IsDead)
        {
            return playCount;
        }

        _hasCheckedFirstCardThisTurn = true;
        if (!_shouldReplayFirstCardThisTurn)
        {
            return playCount;
        }

        _replayedFirstCardThisTurn = true;
        return playCount + 1;
    }

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        if (_replayedFirstCardThisTurn)
        {
            Flash();
            _replayedFirstCardThisTurn = false;
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _hasCheckedFirstCardThisTurn = false;
        _shouldReplayFirstCardThisTurn = false;
        _replayedFirstCardThisTurn = false;
        return Task.CompletedTask;
    }
}
