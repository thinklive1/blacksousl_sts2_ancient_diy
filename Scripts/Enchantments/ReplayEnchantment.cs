using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Replay enchantment.</summary>
[RegisterEnchantment]
public class ReplayEnchantment : ModEnchantmentTemplate
{
    private const int MaxTriggersPerCombat = 3;

    // Replay uses its amount as an internal per-combat counter.
    public override bool ShowAmount => false;

    // Adds replay rules text directly to the enchanted card.
    public override bool HasExtraCardText => true;

    // Retain is shown because replayed exhaust cards wait in the exhaust pile.
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/TimeQueenBlessingRelic.png"
    );

    public override Task BeforeCombatStart()
    {
        Amount = MaxTriggersPerCombat;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Card?.Owner != player)
        {
            return;
        }

        if (Card.Pile?.Type != PileType.Exhaust)
        {
            return;
        }

        if (CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        if (Amount <= 0)
        {
            return;
        }

        Creature? target = null;
        ICombatState? combatState = Card.CombatState ?? player.Creature.CombatState;
        if (Card.TargetType == TargetType.RandomEnemy)
        {
            if (combatState == null)
            {
                return;
            }

            target = player.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
            if (target == null)
            {
                return;
            }
        }

        Amount--;
        BsAncientAudio.PlayOneShot(BsAncientAudio.Clock);
        await CardPileCmd.Add(Card, PileType.Play);
        await CardCmd.AutoPlay(choiceContext, Card, target);
    }


}
