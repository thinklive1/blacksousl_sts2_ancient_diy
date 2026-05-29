using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterEnchantment]
public class FeedingEnchantment : ModEnchantmentTemplate
{
    public const int InitialDamagePercent = 200;
    private const int CombatEndDecay = 20;
    private const int KillGrowth = 50;
    private const int MinDamagePercent = 50;
    private const int MaxDamagePercent = 200;

    public override bool ShowAmount => true;

    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/HorrifyingGluttonRelic.png"
    );

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType == CardType.Attack;
    }

    public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props)
    {
        return Amount / 100m;
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (cardSource == Card && target.Side != dealer?.Side && result.WasTargetKilled)
        {
            Amount = Math.Min(MaxDamagePercent, Amount + KillGrowth);
            Card.DynamicVars.RecalculateForUpgradeOrEnchant();
        }

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        Amount = Math.Max(MinDamagePercent, Amount - CombatEndDecay);
        Card.DynamicVars.RecalculateForUpgradeOrEnchant();
        return Task.CompletedTask;
    }
}
