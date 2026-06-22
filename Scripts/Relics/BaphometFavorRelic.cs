using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class BaphometFavorRelic : ModRelicTemplate
{
    private const int FixedXValue = 3;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/BaphometFavorRelic.png";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("XValue", FixedXValue)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return player == Owner ? 0m : amount;
    }

    public override decimal ModifyEnergyGain(Player player, decimal amount)
    {
        return player == Owner ? 0m : amount;
    }

    public override int ModifyXValue(CardModel card, int originalValue)
    {
        if (card.Owner != Owner || !card.EnergyCost.CostsX)
        {
            return originalValue;
        }

        return DynamicVars["XValue"].IntValue;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.IsAutoPlay || !cardPlay.IsFirstInSeries)
        {
            return;
        }

        int hpCost = GetHpCost(cardPlay.Card);
        if (hpCost <= 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            hpCost,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            cardPlay.Card
        );
    }

    private int GetHpCost(CardModel card)
    {
        if (card.EnergyCost.CostsX)
        {
            return DynamicVars["XValue"].IntValue;
        }

        return Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.Local));
    }
}
