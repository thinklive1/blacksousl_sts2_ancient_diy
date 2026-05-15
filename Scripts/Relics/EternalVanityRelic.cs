using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class EternalVanityRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/EternalVanityRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/EternalVanityRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/EternalVanityRelic.png"
    );

    public override Task AfterObtained()
    {
        Flash();
        foreach (CardModel card in PileType.Deck.GetPile(Owner).Cards)
        {
            CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
        }

        return Task.CompletedTask;
    }
}
