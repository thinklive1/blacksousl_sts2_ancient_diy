using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterEnchantment]
public sealed class LieEnchantment : ModEnchantmentTemplate
{
    private const string LieIconPath = "res://bs_ancient/assets/images/enchantment/LieEnchantment.png";

    private int _virtualBlock;
    private int _virtualDamage;

    public override bool ShowAmount => false;

    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(IconPath: LieIconPath);

    [SavedProperty]
    public int BlackSouls_VirtualBlock
    {
        get => _virtualBlock;
        set
        {
            AssertMutable();
            _virtualBlock = value;
        }
    }

    [SavedProperty]
    public int BlackSouls_VirtualDamage
    {
        get => _virtualDamage;
        set
        {
            AssertMutable();
            _virtualDamage = value;
        }
    }

    public override bool CanEnchant(CardModel card)
    {
        return base.CanEnchant(card)
            && !card.Tags.Contains(CardTag.OstyAttack)
            && (GetDamageVars(card).Any() || GetBlockVar(card) != null);
    }

    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        return BlackSouls_VirtualBlock > 0
            ? -originalDamage
            : 0;
    }

    public override decimal ModifyBlockAdditive(
        Creature target,
        decimal originalBlock,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        return BlackSouls_VirtualDamage > 0
            ? -originalBlock
            : 0;
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (cardPlay?.Card != Card)
        {
            return;
        }

        if (BlackSouls_VirtualBlock > 0)
        {
            await CreatureCmd.GainBlock(
                Card.Owner.Creature,
                BlackSouls_VirtualBlock,
                ValueProp.Move,
                cardPlay);
        }

        if (BlackSouls_VirtualDamage > 0)
        {
            Creature? target = GetVirtualDamageTarget(cardPlay);
            if (target != null)
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    target,
                    BlackSouls_VirtualDamage,
                    ValueProp.Move,
                    Card.Owner.Creature,
                    Card);
            }
        }

        CardModel deckCard = Card.DeckVersion ?? Card;
        SwapAttackAndBlock(deckCard);

        if (deckCard != Card)
        {
            SwapAttackAndBlock(Card);
        }

        Card.Owner?.PlayerCombatState?.RecalculateCardValues();
    }

    private static void SwapAttackAndBlock(CardModel card)
    {
        if (card.Enchantment is not LieEnchantment lie)
        {
            return;
        }

        List<DynamicVar> damageVars = GetDamageVars(card).ToList();
        BlockVar? blockVar = GetBlockVar(card);
        if (damageVars.Count == 0 && blockVar == null)
        {
            return;
        }

        int oldDamage = damageVars.Count > 0
            ? damageVars[0].IntValue
            : lie.BlackSouls_VirtualDamage;
        int oldBlock = blockVar?.IntValue ?? lie.BlackSouls_VirtualBlock;

        if (damageVars.Count > 0)
        {
            foreach (DynamicVar damageVar in damageVars)
            {
                damageVar.UpgradeValueBy(oldBlock - damageVar.IntValue);
            }
        }
        else
        {
            lie.BlackSouls_VirtualDamage = Math.Max(0, oldBlock);
        }

        if (blockVar != null)
        {
            blockVar.UpgradeValueBy(oldDamage - blockVar.IntValue);
            lie.BlackSouls_VirtualBlock = 0;
        }
        else
        {
            lie.BlackSouls_VirtualBlock = Math.Max(0, oldDamage);
        }

        if (damageVars.Count > 0)
        {
            lie.BlackSouls_VirtualDamage = 0;
        }

        card.DynamicVars.RecalculateForUpgradeOrEnchant();
    }

    private Creature? GetVirtualDamageTarget(CardPlay? cardPlay)
    {
        if (cardPlay?.Target != null)
        {
            return cardPlay.Target;
        }

        IEnumerable<Creature> targets = Card.Owner.Creature.CombatState?.HittableEnemies ?? [];
        return Card.Owner.RunState.Rng.CombatTargets.NextItem(targets);
    }

    private static IEnumerable<DynamicVar> GetDamageVars(CardModel card)
    {
        return card.DynamicVars.Values.Where(var => var.Name.Contains("Damage", StringComparison.Ordinal));
    }

    private static BlockVar? GetBlockVar(CardModel card)
    {
        return card.DynamicVars.Values.OfType<BlockVar>().FirstOrDefault();
    }
}
