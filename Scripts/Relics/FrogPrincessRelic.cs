using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Frog Princess relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class FrogPrincessRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<DissolveEnchantment>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override Task AfterObtained()
    {
        DissolveEnchantment dissolve = ModelDb.Enchantment<DissolveEnchantment>();
        foreach (CardModel card in PileType.Deck.GetPile(Owner).Cards.Where(dissolve.CanEnchant).ToList())
        {
            CardCmd.Enchant<DissolveEnchantment>(card, 1m);
        }

        Flash();
        return Task.CompletedTask;
    }
}
