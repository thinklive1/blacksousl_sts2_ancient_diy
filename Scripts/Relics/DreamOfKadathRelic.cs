using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class DreamOfKadathRelic : ModRelicTemplate
{
    private const decimal ExtraRestHealPercent = 0.3m;
    private const int UpgradeCount = 2;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(UpgradeCount)];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/DreamOfKadathRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/DreamOfKadathRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/DreamOfKadathRelic.png"
    );

    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
    {
        if (creature.Player != Owner && creature.PetOwner != Owner)
        {
            return amount;
        }

        return amount + creature.MaxHp * ExtraRestHealPercent;
    }

    public override Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        if (player != Owner)
        {
            return Task.CompletedTask;
        }

        Flash();
        Status = RelicStatus.Normal;

        IEnumerable<CardModel> cards = PileType.Deck.GetPile(Owner).Cards
            .Where(card => card?.IsUpgradable ?? false)
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Niche)
            .Take(DynamicVars.Cards.IntValue);

        foreach (CardModel card in cards)
        {
            CardCmd.Upgrade(card, CardPreviewStyle.MessyLayout);
        }

        return Task.CompletedTask;
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner)
        {
            return false;
        }

        List<RestSiteOption> optionsToRemove = options
            .Where(option => option is not HealRestSiteOption)
            .ToList();

        foreach (RestSiteOption option in optionsToRemove)
        {
            options.Remove(option);
        }

        return optionsToRemove.Count > 0;
    }

    public override IReadOnlyList<LocString> ModifyExtraRestSiteHealText(Player player, IReadOnlyList<LocString> currentExtraText)
    {
        if (!LocalContext.IsMe(Owner))
        {
            return currentExtraText;
        }

        LocString? additionalRestSiteHealText = AdditionalRestSiteHealText;

        if (additionalRestSiteHealText == null)
        {
            return currentExtraText;
        }

        LocString[] extraText = new LocString[currentExtraText.Count + 1];

        for (int i = 0; i < currentExtraText.Count; i++)
        {
            extraText[i] = currentExtraText[i];
        }

        extraText[^1] = additionalRestSiteHealText;
        return extraText;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        Status = room is RestSiteRoom ? RelicStatus.Active : RelicStatus.Normal;
        return Task.CompletedTask;
    }
}
