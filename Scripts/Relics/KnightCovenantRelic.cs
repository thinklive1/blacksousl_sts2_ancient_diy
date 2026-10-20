using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Provides four combat-only free Suit triggers through the card context menu.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class KnightCovenantRelic : ModRelicTemplate
{
    public const int TriggersPerCombat = 4;
    private const string RelicIconPath =
        "res://bs_ancient/assets/images/relics/KnightCovenantRelic.png";

    private int _remainingTriggers;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override int DisplayAmount => _remainingTriggers;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..HoverTipFactory.FromEnchantment<HeartSuitEnchantment>(),
        ..HoverTipFactory.FromEnchantment<DiamondSuitEnchantment>(),
        ..HoverTipFactory.FromEnchantment<ClubSuitEnchantment>(),
        ..HoverTipFactory.FromEnchantment<SpadeSuitEnchantment>()
    ];

    [SavedProperty]
    public int BlackSouls_RemainingTriggers
    {
        get => _remainingTriggers;
        set
        {
            AssertMutable();
            _remainingTriggers = Math.Clamp(value, 0, TriggersPerCombat);
        }
    }

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath);

    public override bool IsAllowed(IRunState runState) => false;

    public override Task BeforeCombatStart()
    {
        BlackSouls_RemainingTriggers = TriggersPerCombat;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    internal bool CanTriggerSuitEffect(CardModel card)
    {
        return _remainingTriggers > 0
            && card.Owner == Owner
            && card.Pile?.Type == PileType.Hand
            && card.Enchantment is PlayingCardSuitEnchantment
            && Owner.Creature.CombatState != null
            && !CombatManager.Instance.IsOverOrEnding;
    }

    internal async Task TryTriggerSuitEffect(CardModel card, PlayerChoiceContext choiceContext)
    {
        if (!CanTriggerSuitEffect(card)
            || card.Enchantment is not PlayingCardSuitEnchantment suitEnchantment)
        {
            return;
        }

        BlackSouls_RemainingTriggers--;
        InvokeDisplayAmountChanged();
        Flash();
        await suitEnchantment.TriggerForRightClick(choiceContext, Owner.Creature);
    }
}
