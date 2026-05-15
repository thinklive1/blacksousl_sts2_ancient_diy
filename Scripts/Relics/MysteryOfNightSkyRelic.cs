using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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
    private bool _isReplayingCard;

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
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_isReplayingCard || _hasCheckedFirstCardThisTurn || cardPlay.Card.Owner != Owner || Owner.Creature.IsDead)
        {
            return;
        }

        _hasCheckedFirstCardThisTurn = true;
        if (Owner.RunState.Rng.Niche.NextInt(100) >= ReplayChance)
        {
            return;
        }

        Flash();
        _isReplayingCard = true;
        try
        {
            await CardCmd.AutoPlay(
                choiceContext,
                cardPlay.Card,
                cardPlay.Target,
                skipXCapture: true,
                skipCardPileVisuals: true
            );
        }
        finally
        {
            _isReplayingCard = false;
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _hasCheckedFirstCardThisTurn = false;
        _isReplayingCard = false;
        return Task.CompletedTask;
    }
}
