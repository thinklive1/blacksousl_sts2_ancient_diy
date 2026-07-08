using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Bellmans Page relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class BellmansPageRelic : ModRelicTemplate
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
        return !SnarkPageRelicTrackerModifier.HasAppearedOrOwned<BellmansPageRelic>(runState);
    }

    /// <summary>
    /// Mirrors existing odd card groups when the relic is obtained.
    /// </summary>
    public override async Task AfterObtained()
    {
        if (Owner == null)
        {
            return;
        }

        SnarkPageRelicTrackerModifier.MarkAppeared<BellmansPageRelic>(Owner);

        List<CardModel> deck = PileType.Deck.GetPile(Owner).Cards.ToList();
        List<CardModel> cardsToMirror = deck
            .GroupBy(card => card.Id)
            .Where(group => group.Count() % 2 == 1)
            .Select(group => group.Last())
            .ToList();

        foreach (CardModel card in cardsToMirror)
        {
            await MirrorCard(card);
        }
    }

    /// <summary>
    /// Duplicates a deck card when its copy count would otherwise become odd.
    /// </summary>
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        // Only permanent deck additions should be mirrored.
        if (Owner == null || card.Pile?.Type != PileType.Deck)
        {
            return;
        }

        // Combat-only cards are temporary and should not affect deck parity.
        if (card.CombatState != null)
        {
            return;
        }

        List<CardModel> deck = PileType.Deck.GetPile(Owner).Cards.ToList();
        int count = deck.Count(c => c.Id == card.Id);

        // Add a duplicate when the new count is odd.
        if (count % 2 == 1)
        {
            await MirrorCard(card);
        }

        await Task.CompletedTask;
    }

    private async Task MirrorCard(CardModel card)
    {
        if (Owner == null)
        {
            return;
        }

        CardModel clone = Owner.RunState.CloneCard(card);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.Add(clone, PileType.Deck, CardPilePosition.Bottom, this, false),
            2f);
        Flash();
    }
}
