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
    private const int RecentCards = 5;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(RecentCards)
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
        List<CardModel> recentCards = Owner.Deck.Cards
            .Where(card => card.Pile?.Type == PileType.Deck)
            .TakeLast(RecentCards)
            .ToList();

        if (recentCards.Count == 0)
        {
            return;
        }

        List<CardModel> selectedCards = (await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                recentCards,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 0, recentCards.Count)
                {
                    Cancelable = true,
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
}
