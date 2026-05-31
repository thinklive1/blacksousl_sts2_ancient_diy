using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class GiftOfChaosRelic : ModRelicTemplate
{
    private const int MaxFusionMaterials = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(MaxFusionMaterials)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [CreateFusionHoverTip(), .. HoverTipFactory.FromCardWithCardHoverTips<ChaosFusionCard>()];

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/GiftOfChaosRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/GiftOfChaosRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/GiftOfChaosRelic.png"
    );

    public override async Task AfterObtained()
    {
        Flash();

        List<CardModel> selectedCards = (await CardSelectCmd.FromDeckGeneric(
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1, MaxFusionMaterials),
                IsValidMaterial))
            .ToList();

        if (selectedCards.Count == 0)
        {
            return;
        }

        ChaosFusionCard fusionCard = Owner.RunState.CreateCard<ChaosFusionCard>(Owner);
        fusionCard.ConfigureFrom(selectedCards, GetRandomFusionType(selectedCards));
        PreserveRandomEnchantment(selectedCards, fusionCard);

        await CardPileCmd.RemoveFromDeck(selectedCards);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(fusionCard, PileType.Deck, source: this), 2f);
    }

    private static bool IsValidMaterial(CardModel card)
    {
        return card.Type is CardType.Attack or CardType.Skill
            && !card.EnergyCost.CostsX;
    }

    private CardType GetRandomFusionType(IReadOnlyList<CardModel> materials)
    {
        IReadOnlyList<CardType> types = materials
            .Select(card => card.Type)
            .Where(type => type is CardType.Attack or CardType.Skill)
            .Distinct()
            .ToList();

        return types.Count == 0 ? CardType.Skill : Owner.RunState.Rng.Niche.NextItem(types);
    }

    private void PreserveRandomEnchantment(IReadOnlyList<CardModel> materials, CardModel fusionCard)
    {
        List<SerializableEnchantment> enchantments = materials
            .Select(card => card.Enchantment?.ToSerializable())
            .OfType<SerializableEnchantment>()
            .ToList();

        SerializableEnchantment? selected = Owner.RunState.Rng.Niche.NextItem(enchantments);
        if (selected == null)
        {
            return;
        }

        EnchantmentModel enchantment = EnchantmentModel.FromSerializable(selected);
        fusionCard.EnchantInternal(enchantment, selected.Amount);
        enchantment.ModifyCard();
        fusionCard.FinalizeUpgradeInternal();
    }

    private IHoverTip CreateFusionHoverTip()
    {
        LocString title = new("relics", $"{Id.Entry}.fusionDetails.title");
        LocString description = new("relics", $"{Id.Entry}.fusionDetails.description");
        DynamicVars.AddTo(description);
        return new HoverTip(title, description);
    }
}
