using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Sleep God Myth relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class SleepGodMythRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/MythRelic.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<SleepEnchantment>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override async Task AfterObtained()
    {
        SleepEnchantment sleep = ModelDb.Enchantment<SleepEnchantment>();
        List<CardModel> candidates = PileType.Deck.GetPile(Owner).Cards
            .Where(c => sleep.CanEnchant(c) && !c.EnergyCost.CostsX && c.EnergyCost.GetWithModifiers(CostModifiers.Local) > 0)
            .ToList();

        if (candidates.Count == 0)
        {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromSimpleGrid(
            new BlockingPlayerChoiceContext(),
            candidates,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1)))
            .FirstOrDefault();

        if (selected == null)
        {
            return;
        }

        int cost = selected.EnergyCost.GetWithModifiers(CostModifiers.Local);
        if (cost <= 0)
        {
            return;
        }

        Flash();
        CardCmd.Enchant<SleepEnchantment>(selected, cost);
    }
}
