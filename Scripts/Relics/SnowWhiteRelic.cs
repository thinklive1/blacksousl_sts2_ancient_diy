using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Snow White relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class SnowWhiteRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private bool _triggeredThisCombat;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override Task BeforeCombatStart()
    {
        _triggeredThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_triggeredThisCombat || cardPlay.Card.Owner != Owner)
        {
            return;
        }

        _triggeredThisCombat = true;
        Flash();

        int addedToDraw = 0;
        int addedToDiscard = 0;

        CardModel cloneDraw = cardPlay.Card.CreateClone();
        CardPileAddResult resultDraw = await CardPileCmd.AddGeneratedCardToCombat(
            cloneDraw, PileType.Draw, Owner, CardPilePosition.Random);
        if (resultDraw.success)
        {
            addedToDraw++;
        }

        CardModel cloneDiscard = cardPlay.Card.CreateClone();
        CardPileAddResult resultDiscard = await CardPileCmd.AddGeneratedCardToCombat(
            cloneDiscard, PileType.Discard, Owner, CardPilePosition.Random);
        if (resultDiscard.success)
        {
            addedToDiscard++;
        }

        RefreshPileCounter(PileType.Draw, addedToDraw);
        RefreshPileCounter(PileType.Discard, addedToDiscard);
    }

    private void RefreshPileCounter(PileType pileType, int addedCount)
    {
        if (addedCount <= 0)
        {
            return;
        }

        CardPile pile = pileType.GetPile(Owner);
        pile.InvokeContentsChanged();

        for (int i = 0; i < addedCount; i++)
        {
            pile.InvokeCardAddFinished();
        }
    }
}
