using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class DarkGoatOfTheWoodsMythRelic : ModRelicTemplate
{
    private const int CardsToEnchant = 2;
    private const int ChildCount = 4;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/MythRelic.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Select", CardsToEnchant),
        new DynamicVar("Children", ChildCount)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [
            CreateBreedingHoverTip(),
            .. HoverTipFactory.FromCardWithCardHoverTips<ChaosFusionCard>()
        ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override async Task AfterObtained()
    {
        BreedingEnchantment breed = ModelDb.Enchantment<BreedingEnchantment>();
        List<CardModel> deckCards = PileType.Deck.GetPile(Owner).Cards
            .Where(c => breed.CanEnchant(c) && !c.EnergyCost.CostsX)
            .ToList();

        if (deckCards.Count < CardsToEnchant)
        {
            return;
        }

        List<CardModel> selected = (await CardSelectCmd.FromSimpleGrid(
            new BlockingPlayerChoiceContext(),
            deckCards,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, CardsToEnchant, CardsToEnchant)
            {
                RequireManualConfirmation = true
            }))
            .ToList();

        if (selected.Count != CardsToEnchant)
        {
            return;
        }

        Flash();
        foreach (CardModel card in selected)
        {
            CardCmd.Enchant<BreedingEnchantment>(card, 1m);
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        List<CardModel> breedCards = FindBreedingCards(player);
        if (breedCards.Count < 2)
        {
            return;
        }

        Rng rng = Owner.RunState.Rng.Niche;
        List<CardModel> materials = ShuffleTake(breedCards, 2, rng);
        if (materials.Count < 2)
        {
            return;
        }

        Flash();

        // Exhaust the 2 parents
        foreach (CardModel card in materials)
        {
            await CardPileCmd.Add(card, PileType.Exhaust);
        }

        // Generate 4 children
        for (int i = 0; i < ChildCount; i++)
        {
            PileType targetPile = rng.NextItem([PileType.Draw, PileType.Discard]);
            ChaosFusionCard child = player.Creature.CombatState!.CreateCard<ChaosFusionCard>(Owner);
            CardType childType = rng.NextItem(materials)!.Type;
            int childCost = GetMaterialCost(rng.NextItem(materials)!);
            List<CardModel> inheritedMaterials = RollInheritedMaterials(materials, rng);
            child.ConfigureFrom(inheritedMaterials, childType, childCost);
            CardCmd.Enchant<BreedingEnchantment>(child, 1m);

            await CardPileCmd.AddGeneratedCardToCombat(
                child, targetPile, Owner, CardPilePosition.Random);
        }
    }

    private static List<CardModel> FindBreedingCards(Player player)
    {
        List<CardModel> result = [];
        foreach (CardModel card in GetAllCardsInCombat(player))
        {
            if (card.Enchantment is BreedingEnchantment)
            {
                result.Add(card);
            }
        }

        return result;
    }

    private static List<CardModel> GetAllCardsInCombat(Player player)
    {
        List<CardModel> all = [];
        all.AddRange(PileType.Hand.GetPile(player).Cards);
        all.AddRange(PileType.Draw.GetPile(player).Cards);
        all.AddRange(PileType.Discard.GetPile(player).Cards);
        return all;
    }

    private static List<CardModel> ShuffleTake(List<CardModel> source, int count, Rng rng)
    {
        if (source.Count <= count)
        {
            return [.. source];
        }

        List<CardModel> pool = [.. source];
        List<CardModel> result = [];
        for (int i = 0; i < count; i++)
        {
            int index = rng.NextInt(pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    private static List<CardModel> RollInheritedMaterials(IEnumerable<CardModel> parents, Rng rng)
    {
        List<CardModel> result = [];
        foreach (CardModel parent in parents)
        {
            if (rng.NextInt(2) == 0)
            {
                result.Add(parent);
            }
        }

        return result;
    }

    private static int GetMaterialCost(CardModel card)
    {
        if (card.EnergyCost.CostsX)
        {
            return 0;
        }

        return Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.Local));
    }

    private IHoverTip CreateBreedingHoverTip()
    {
        LocString title = new("relics", $"{Id.Entry}.breeding.title");
        LocString description = new("relics", $"{Id.Entry}.breeding.description");
        DynamicVars.AddTo(description);
        return new HoverTip(title, description);
    }
}
