using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Randomly redistributes the owner's combat cards between the three regular card piles.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class BakersPageRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/scripts.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return !SnarkPageRelicTrackerModifier.HasAppearedOrOwned<BakersPageRelic>(runState);
    }

    public override Task AfterObtained()
    {
        if (Owner != null)
        {
            SnarkPageRelicTrackerModifier.MarkAppeared<BakersPageRelic>(Owner);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player != Owner)
        {
            return;
        }

        List<CardModel> cards = PileType.Hand.GetPile(Owner).Cards
            .Concat(PileType.Draw.GetPile(Owner).Cards)
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Distinct()
            .ToList();

        if (cards.Count == 0)
        {
            return;
        }

        Flash();
        foreach (CardModel card in cards)
        {
            PileType destination = Owner.RunState.Rng.CombatCardSelection.NextInt(3) switch
            {
                0 => PileType.Hand,
                1 => PileType.Draw,
                _ => PileType.Discard
            };

            await CardPileCmd.Add(card, destination, CardPilePosition.Random, this, false);
        }
    }
}
