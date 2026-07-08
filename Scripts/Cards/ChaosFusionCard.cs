using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Implements the Chaos Fusion card.</summary>
[RegisterCard(typeof(EventCardPool))]
public class ChaosFusionCard : ModCardTemplate
{
    private const int DefaultCost = 0;

    private List<SerializableCard> _materials = [];
    private CardKeyword[] _fusedKeywords = [];
    private CardTag[] _fusedTags = [];
    private CardType _fusedType = CardType.Skill;
    private int _fusedCost;

    public override CardType Type => BlackSouls_FusedType;

    public override IEnumerable<CardKeyword> CanonicalKeywords => BlackSouls_FusedKeywords;

    public override IEnumerable<CardTag> Tags => BlackSouls_FusedTags;

    protected override HashSet<CardTag> CanonicalTags => BlackSouls_FusedTags.ToHashSet();

    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/GiftOfChaosCard.png"
    );

    [SavedProperty]
    public List<SerializableCard> BlackSouls_Materials
    {
        get => _materials;
        set
        {
            AssertMutable();
            _materials = value ?? [];
            RefreshCost();
        }
    }

    [SavedProperty]
    public CardKeyword[] BlackSouls_FusedKeywords
    {
        get => _fusedKeywords;
        set
        {
            AssertMutable();
            _fusedKeywords = value ?? [];
        }
    }

    [SavedProperty]
    public CardTag[] BlackSouls_FusedTags
    {
        get => _fusedTags;
        set
        {
            AssertMutable();
            _fusedTags = value ?? [];
        }
    }

    [SavedProperty]
    public CardType BlackSouls_FusedType
    {
        get => _fusedType;
        set
        {
            AssertMutable();
            _fusedType = value;
        }
    }

    [SavedProperty]
    public int BlackSouls_FusedCost
    {
        get => _fusedCost;
        set
        {
            AssertMutable();
            _fusedCost = value;
            RefreshCost();
        }
    }

    public ChaosFusionCard() : base(DefaultCost, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    public void ConfigureFrom(IReadOnlyList<CardModel> materials, CardType fusedType, int? fusedCost = null)
    {
        BlackSouls_Materials = materials.Select(card => card.ToSerializable()).ToList();
        BlackSouls_FusedType = fusedType;
        BlackSouls_FusedCost = fusedCost ?? materials.Sum(GetMaterialCost);
        BlackSouls_FusedKeywords = materials
            .SelectMany(card => card.Keywords)
            .Distinct()
            .OrderBy(keyword => keyword)
            .ToArray();
        BlackSouls_FusedTags = materials
            .SelectMany(card => card.Tags)
            .Distinct()
            .OrderBy(tag => tag)
            .ToArray();
        ApplyFusedKeywords();
        RefreshCost();
    }

    protected override void AfterDeserialized()
    {
        ApplyFusedKeywords();
        RefreshCost();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState is null || BlackSouls_Materials.Count == 0)
        {
            return;
        }

        List<CardModel> materialCards = BlackSouls_Materials
            .Select(CardModel.FromSerializable)
            .ToList();

        foreach (CardModel material in materialCards)
        {
            CombatState.AddCard(material, Owner);
        }

        foreach (CardModel material in materialCards.StableShuffle(Owner.RunState.Rng.CombatCardSelection))
        {
            await CardCmd.AutoPlay(choiceContext, material, null, AutoPlayType.Default, skipCardPileVisuals: false);

            if (material.Pile?.Type.IsCombatPile() == true)
            {
                await CardPileCmd.RemoveFromCombat(material, skipVisuals: true);
            }
        }
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        string materialNames = string.Join("、", BlackSouls_Materials.Select(GetMaterialTitle));
        description.Add("Materials", string.IsNullOrWhiteSpace(materialNames) ? "无" : materialNames);
    }

    private void RefreshCost()
    {
        if (IsMutable)
        {
            EnergyCost.SetCustomBaseCost(Math.Max(0, BlackSouls_FusedCost));
        }
    }

    private void ApplyFusedKeywords()
    {
        _ = Keywords;
        foreach (CardKeyword keyword in BlackSouls_FusedKeywords)
        {
            AddKeyword(keyword);
        }
    }

    private static int GetMaterialCost(CardModel card)
    {
        if (card.EnergyCost.CostsX)
        {
            return 0;
        }

        return Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.Local));
    }

    private static string GetMaterialTitle(SerializableCard save)
    {
        try
        {
            return CardModel.FromSerializable(save).Title;
        }
        catch
        {
            return save.Id?.Entry ?? "?";
        }
    }
}
