using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class RedQueenAlbumRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/RedQueenAlbumRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/RedQueenAlbumRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/RedQueenAlbumRelic.png"
    );

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return Task.CompletedTask;
        }

        List<CardModel> candidates = PileType.Hand.GetPile(Owner).Cards
            .Where(card => !card.EnergyCost.CostsX && card.EnergyCost.GetWithModifiers(CostModifiers.All) > 0)
            .ToList();

        if (candidates.Count == 0)
        {
            return Task.CompletedTask;
        }

        Flash();
        Owner.RunState.Rng.CombatCardSelection.NextItem(candidates)?.EnergyCost.AddThisCombat(-1);
        return Task.CompletedTask;
    }
}
