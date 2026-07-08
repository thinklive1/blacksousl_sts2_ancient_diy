using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Kachi Kachi Yama relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class KachiKachiYamaRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Anger>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.Creature.CombatState == null)
        {
            return;
        }

        int turnNumber = Math.Max(1, Owner.PlayerCombatState?.TurnNumber ?? 1);
        Creature? target = GetAutoPlayTarget();
        if (target == null)
        {
            return;
        }

        Flash();
        for (int i = 0; i < turnNumber; i++)
        {
            if (Owner.Creature.CombatState == null)
            {
                return;
            }

            CardModel anger = Owner.Creature.CombatState.CreateCard<Anger>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(anger, PileType.Hand, Owner, CardPilePosition.Top);
            await CardCmd.AutoPlay(choiceContext, anger, target);
        }
    }

    private Creature? GetAutoPlayTarget()
    {
        return Owner.Creature.CombatState?.HittableEnemies
            .FirstOrDefault(creature => creature.IsAlive);
    }
}
