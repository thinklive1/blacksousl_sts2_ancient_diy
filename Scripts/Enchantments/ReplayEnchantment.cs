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

[RegisterEnchantment]
public class ReplayEnchantment : ModEnchantmentTemplate
{
    private const int MaxTriggersPerCombat = 3;

    // 是否在卡牌上显示数值
    public override bool ShowAmount => false;

    // 重载这个以改变显示的数字
    // public override int DisplayAmount => DynamicVars.Cards.IntValue;

    // 是否会添加额外的卡牌描述文本
    public override bool HasExtraCardText => true;

    // 像卡牌、遗物、药水等一样，可以使用DynamicVars和ExtraHoverTips
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    // 图标位置。大小1:1就行，原版是64x64
    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/TimeQueenBlessingRelic.png"
    );

    // 决定是否可以附魔到某张卡牌上，这里我们让它只能附魔到获得格挡的卡牌上。
    /*
    public override bool CanEnchant(CardModel card)
    {
        if (base.CanEnchant(card))
        {
            return card.GainsBlock;
        }
        return false;
    }
    */

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
        CombatState? combatState = Card.CombatState ?? player.Creature.CombatState;
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
