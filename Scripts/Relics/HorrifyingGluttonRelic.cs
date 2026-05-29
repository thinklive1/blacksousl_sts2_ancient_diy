using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
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
public sealed class HorrifyingGluttonRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<FeedingEnchantment>(FeedingEnchantment.InitialDamagePercent);

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://bs_ancient/assets/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://bs_ancient/assets/images/relics/{GetType().Name}.png",
        BigIconPath: $"res://bs_ancient/assets/images/relics/{GetType().Name}.png"
    );

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Any(HasAttackCandidate);
    }

    public override async Task AfterObtained()
    {
        CardModel? selected = (await CardSelectCmd.FromDeckForEnchantment(
            Owner,
            ModelDb.Enchantment<FeedingEnchantment>(),
            FeedingEnchantment.InitialDamagePercent,
            card => card is { Type: CardType.Attack },
            new CardSelectorPrefs(SelectionScreenPrompt, DynamicVars.Cards.IntValue))).FirstOrDefault();

        if (selected == null)
        {
            return;
        }

        CardCmd.Enchant<FeedingEnchantment>(selected, FeedingEnchantment.InitialDamagePercent);
        NCardEnchantVfx? vfx = NCardEnchantVfx.Create(selected);
        if (vfx != null)
        {
            NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
        }
    }

    public static bool HasAttackCandidate(Player player)
    {
        FeedingEnchantment feeding = ModelDb.Enchantment<FeedingEnchantment>();
        return PileType.Deck.GetPile(player).Cards
            .Any(card => card.Type == CardType.Attack && feeding.CanEnchant(card));
    }
}
