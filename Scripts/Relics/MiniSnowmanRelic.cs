using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class MiniSnowmanRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => HoverTipFactory.FromAffliction<EvilQiAffliction>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/MiniSnowmanRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/MiniSnowmanRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/MiniSnowmanRelic.png"
    );

    public override Task AfterObtained()
    {
        foreach (CardModel card in PileType.Deck.GetPile(Owner).Cards)
        {
            if (card.Enchantment != null)
            {
                CardCmd.ClearEnchantment(card);
            }
        }

        return Task.CompletedTask;
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner.PlayerCombatState == null)
        {
            return;
        }

        foreach (CardModel card in Owner.PlayerCombatState.AllCards.Where(ModelDb.Affliction<EvilQiAffliction>().CanAfflict))
        {
            if (card.Affliction is EvilQiAffliction)
            {
                continue;
            }

            if (card.Affliction != null)
            {
                CardCmd.ClearAffliction(card);
            }

            if (card.Affliction == null)
            {
                await CardCmd.Afflict<EvilQiAffliction>(card, 1m);
            }
        }
    }
}
