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
public class TwinWaxStatueRelic : ModRelicTemplate
{
    private const int EnchantCount = 2;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(EnchantCount)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<TwinEnchantment>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://bs_ancient/assets/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://bs_ancient/assets/images/relics/{GetType().Name}.png",
        BigIconPath: $"res://bs_ancient/assets/images/relics/{GetType().Name}.png"
    );

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Any(HasTwinCandidates);
    }

    public override async Task AfterObtained()
    {
        List<CardModel> candidates = GetTwinCandidates(Owner).ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromDeckForEnchantment(
            candidates,
            ModelDb.Enchantment<TwinEnchantment>(),
            1,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (selected == null)
        {
            return;
        }

        List<CardModel> sameNameCards = PileType.Deck.GetPile(Owner).Cards
            .Where(card => card.Id == selected.Id && ModelDb.Enchantment<TwinEnchantment>().CanEnchant(card))
            .ToList();
        List<CardModel> cardsToEnchant = (await CardSelectCmd.FromDeckForEnchantment(
            sameNameCards,
            ModelDb.Enchantment<TwinEnchantment>(),
            1,
            new CardSelectorPrefs(SelectionScreenPrompt, EnchantCount, EnchantCount))).ToList();

        foreach (CardModel card in cardsToEnchant)
        {
            CardCmd.Enchant<TwinEnchantment>(card, 1m);
            NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
            if (vfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
            }
        }
    }

    private static bool HasTwinCandidates(Player player)
    {
        return GetTwinCandidates(player).Any();
    }

    private static IEnumerable<CardModel> GetTwinCandidates(Player player)
    {
        TwinEnchantment twin = ModelDb.Enchantment<TwinEnchantment>();
        return PileType.Deck.GetPile(player).Cards
            .Where(twin.CanEnchant)
            .GroupBy(card => card.Id)
            .Where(group => group.Count() >= EnchantCount)
            .Select(group => group.First());
    }
}
