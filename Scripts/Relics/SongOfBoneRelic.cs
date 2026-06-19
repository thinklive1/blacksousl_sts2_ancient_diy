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

[RegisterRelic(typeof(EventRelicPool))]
public sealed class SongOfBoneRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private bool _playedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Dirge>();

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
        _playedThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (_playedThisCombat || player != Owner || Owner.Creature.CombatState == null)
        {
            return;
        }

        _playedThisCombat = true;
        Flash();

        CardModel dirge = Owner.Creature.CombatState.CreateCard<Dirge>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(dirge, PileType.Hand, Owner, CardPilePosition.Top);
        await CardCmd.AutoPlay(choiceContext, dirge, GetAutoPlayTarget(dirge));
    }

    private Creature? GetAutoPlayTarget(CardModel card)
    {
        if (!card.TargetType.IsSingleTarget() || Owner?.Creature.CombatState == null)
        {
            return null;
        }

        return Owner.Creature.CombatState.HittableEnemies
            .FirstOrDefault(creature => creature.IsAlive);
    }
}
