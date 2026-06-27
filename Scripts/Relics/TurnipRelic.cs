using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class TurnipRelic : ModRelicTemplate
{
    private const int MaxFusionMaterials = 3;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private bool _pending = true;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool ShowCounter => true;

    public override int DisplayAmount => BlackSouls_Pending ? 1 : 0;

    public override bool IsUsedUp => !BlackSouls_Pending;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(MaxFusionMaterials)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [CreateFusionHoverTip(), .. HoverTipFactory.FromCardWithCardHoverTips<ChaosFusionCard>()];

    [SavedProperty]
    public bool BlackSouls_Pending
    {
        get => _pending;
        set
        {
            AssertMutable();
            _pending = value;
            InvokeDisplayAmountChanged();
            Status = IsUsedUp ? RelicStatus.Disabled : RelicStatus.Normal;
        }
    }

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> cardRewardOptions,
        CardCreationOptions creationOptions)
    {
        if (!BlackSouls_Pending || player != Owner)
        {
            return false;
        }

        List<CardModel> materials = cardRewardOptions
            .Select(r => r.Card)
            .Where(IsValidMaterial)
            .Take(MaxFusionMaterials)
            .ToList();

        if (materials.Count < 2)
        {
            BlackSouls_Pending = false;
            Flash();
            return false;
        }

        ChaosFusionCard fusionCard = Owner.RunState.CreateCard<ChaosFusionCard>(Owner);
        CardType fusedType = GetRandomFusionType(materials);
        fusionCard.ConfigureFrom(materials, fusedType);
        PreserveRandomEnchantment(materials, fusionCard);

        cardRewardOptions.Clear();
        cardRewardOptions.Add(new CardCreationResult(fusionCard));

        BlackSouls_Pending = false;
        Flash();
        return true;
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
