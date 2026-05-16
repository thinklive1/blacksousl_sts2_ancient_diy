using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
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

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (CanAffect(card))
        {
            CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
        }

        return Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom || Owner.PlayerCombatState is null)
        {
            return Task.CompletedTask;
        }

        Flash();
        foreach (CardModel card in Owner.PlayerCombatState.AllCards)
        {
            if (CanAffect(card))
            {
                CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
            }
        }

        return Task.CompletedTask;
    }

    private bool CanAffect(CardModel card)
    {
        return card.Owner == Owner
            && !card.Keywords.Contains(CardKeyword.Ethereal);
    }
}
