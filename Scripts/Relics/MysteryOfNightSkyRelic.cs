using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public class MysteryOfNightSkyRelic : ModRelicTemplate
{
    private const int ReplayChance = 50;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Chance", ReplayChance)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/MysteryOfNightSkyRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/MysteryOfNightSkyRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/MysteryOfNightSkyRelic.png"
    );

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == Owner.Creature.Side)
        {
            int decision = Owner.RunState.Rng.Niche.NextInt(100) < ReplayChance
                ? MysteryOfNightSkyDecisionPower.ReplayAvailable
                : MysteryOfNightSkyDecisionPower.NoReplayAvailable;
            await BsPowerCmd.SetAmount<MysteryOfNightSkyDecisionPower>(Owner.Creature, decision, Owner.Creature, null);
        }
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner != Owner || Owner.Creature.IsDead)
        {
            return playCount;
        }

        MysteryOfNightSkyDecisionPower? decision = Owner.Creature.GetPower<MysteryOfNightSkyDecisionPower>();
        if (decision == null || decision.Amount == MysteryOfNightSkyDecisionPower.Consumed)
        {
            return playCount;
        }

        bool shouldReplay = decision.Amount == MysteryOfNightSkyDecisionPower.ReplayAvailable;
        decision.SetAmount(MysteryOfNightSkyDecisionPower.Consumed, silent: true);

        if (shouldReplay)
        {
            Flash();
            return playCount + 1;
        }

        return playCount;
    }
}
