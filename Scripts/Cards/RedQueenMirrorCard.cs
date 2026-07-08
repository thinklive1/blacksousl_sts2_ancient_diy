using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Implements the Red Queen Mirror card.</summary>
[RegisterCard(typeof(EventCardPool))]
public class RedQueenMirrorCard : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Ethereal
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/RedQueenMirrorCard.png"
    );

    public RedQueenMirrorCard() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.RunState.Players.Count > 1)
        {
            return;
        }

        ICombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        List<CardModel> choices = [
            combatState.CreateCard<RedQueenMirrorDrawToDiscardOptionCard>(Owner),
            combatState.CreateCard<RedQueenMirrorDiscardToDrawOptionCard>(Owner)
        ];

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, choices, Owner);
        switch (selected)
        {
            case RedQueenMirrorDrawToDiscardOptionCard:
                await Mirror(PileType.Discard, PileType.Draw, PileType.Discard);
                break;
            case RedQueenMirrorDiscardToDrawOptionCard:
                await Mirror(PileType.Draw, PileType.Discard, PileType.Draw);
                break;
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }

    private async Task Mirror(
        PileType pileToExhaust,
        PileType pileToCopy,
        PileType destinationPile)
    {
        List<CardModel> cardsToExhaust = pileToExhaust.GetPile(Owner).Cards.ToList();
        List<CardModel> cardsToCopy = pileToCopy.GetPile(Owner).Cards.ToList();
        List<CardModel> stillInPileToExhaust = cardsToExhaust
            .Where(card => card.Pile?.Type == pileToExhaust)
            .ToList();
        List<CardModel> copies = cardsToCopy
            .Where(card => card.Pile?.Type == pileToCopy)
            .Select(card => card.CreateClone())
            .ToList();

        if (stillInPileToExhaust.Count > 0)
        {
            await CardPileCmd.RemoveFromCombat(stillInPileToExhaust, skipVisuals: true);
        }

        if (copies.Count > 0)
        {
            await CardPileCmd.Add(copies, destinationPile, skipVisuals: true);
        }

        RefreshPileCounter(pileToExhaust, removedCount: stillInPileToExhaust.Count);
        RefreshPileCounter(destinationPile, addedCount: copies.Count);
    }

    private void RefreshPileCounter(PileType pileType, int addedCount = 0, int removedCount = 0)
    {
        CardPile pile = pileType.GetPile(Owner);
        pile.InvokeContentsChanged();

        for (int i = 0; i < removedCount; i++)
        {
            pile.InvokeCardRemoveFinished();
        }

        for (int i = 0; i < addedCount; i++)
        {
            pile.InvokeCardAddFinished();
        }
    }
}

/// <summary>Implements the Red Queen Mirror Draw To Discard Option card.</summary>
[RegisterCard(typeof(EventCardPool))]
public class RedQueenMirrorDrawToDiscardOptionCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/RedQueenMirrorCard.png"
    );

    public RedQueenMirrorDrawToDiscardOptionCard()
        : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self, false)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }
}

/// <summary>Implements the Red Queen Mirror Discard To Draw Option card.</summary>
[RegisterCard(typeof(EventCardPool))]
public class RedQueenMirrorDiscardToDrawOptionCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/RedQueenMirrorCardMirrored.png"
    );

    public RedQueenMirrorDiscardToDrawOptionCard()
        : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self, false)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }
}
