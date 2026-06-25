using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class PeterPanRelic : ModRelicTemplate
{
    private const int CardsToRemove = 5;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(CardsToRemove)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override async Task AfterObtained()
    {
        List<CardModel> removableCards = Owner.Deck.Cards
            .Where(card => card.Pile?.Type == PileType.Deck)
            .Where(card => card.IsRemovable)
            .Where(card => !IsStrikeOrDefend(card))
            .ToList();

        if (removableCards.Count == 0)
        {
            return;
        }

        if (removableCards.Count <= CardsToRemove)
        {
            Flash();
            await CardPileCmd.RemoveFromDeck(removableCards);
            return;
        }

        List<CardModel> selectedCards = (await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                removableCards,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, CardsToRemove, CardsToRemove)
                {
                    RequireManualConfirmation = true
                }))
            .ToList();

        if (selectedCards.Count == 0)
        {
            return;
        }

        Flash();
        await CardPileCmd.RemoveFromDeck(selectedCards);
    }

    private static bool IsStrikeOrDefend(CardModel card)
    {
        return card.Tags.Contains(CardTag.Strike)
            || card.Tags.Contains(CardTag.Defend);
    }
}
