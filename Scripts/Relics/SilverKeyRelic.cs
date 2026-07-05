using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class SilverKeyRelic : ModRelicTemplate
{
    private const string SilverKeyIconPath = "res://bs_ancient/assets/images/relics/SilverKeyRelic.png";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<UnlockEnchantment>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: SilverKeyIconPath,
        IconOutlinePath: SilverKeyIconPath,
        BigIconPath: SilverKeyIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Any(player =>
            PileType.Deck.GetPile(player).Cards.Any(ModelDb.Enchantment<UnlockEnchantment>().CanEnchant));
    }

    public override async Task AfterObtained()
    {
        foreach (CardModel card in await CardSelectCmd.FromDeckForEnchantment(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, DynamicVars.Cards.IntValue),
            player: Owner,
            enchantment: ModelDb.Enchantment<UnlockEnchantment>(),
            amount: DynamicVars.Cards.IntValue))
        {
            CardCmd.Enchant<UnlockEnchantment>(card, 1m);
            NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
            if (vfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
            }
        }
    }
}
