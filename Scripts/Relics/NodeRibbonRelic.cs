using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class NodeRibbonRelic : ModRelicTemplate
{
    private bool _extraTurnPending;
    private bool _hasTriggeredThisCombat;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this),
        HoverTipFactory.FromKeyword(MyKeywords.Kill)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/NodeRibbonRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/NodeRibbonRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/NodeRibbonRelic.png"
    );

    public override Task BeforeTurnEndVeryEarly(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (_hasTriggeredThisCombat || side != Owner.Creature.Side || Owner.Creature.IsDead || !WouldEnemyAttacksBeLethal())
        {
            return Task.CompletedTask;
        }

        Flash();
        _extraTurnPending = true;
        _hasTriggeredThisCombat = true;
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
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        _extraTurnPending = false;
        _hasTriggeredThisCombat = false;
        return Task.CompletedTask;
    }

    private bool WouldEnemyAttacksBeLethal()
    {
        decimal effectiveHealth = Owner.Creature.CurrentHp
            + Owner.Creature.Block
            + Owner.Creature.GetPowerAmount<PlatingPower>()
            + GetOstyCurrentHp();

        return GetIncomingEnemyAttackDamage() >= effectiveHealth;
    }

    private int GetOstyCurrentHp()
    {
        Creature? osty = Owner.Osty;
        return osty is { IsAlive: true } ? osty.CurrentHp : 0;
    }

    private int GetIncomingEnemyAttackDamage()
    {
        Creature ownerCreature = Owner.Creature;
        CombatState? combatState = ownerCreature.CombatState;
        if (combatState == null)
        {
            return 0;
        }

        Creature[] target = [ownerCreature];
        int incomingDamage = 0;
        foreach (Creature enemy in combatState.Enemies.Where(enemy => enemy.IsAlive))
        {
            foreach (AttackIntent intent in enemy.Monster?.NextMove.Intents.OfType<AttackIntent>() ?? [])
            {
                incomingDamage += intent.GetTotalDamage(target, enemy);
            }
        }

        return incomingDamage;
    }
}
