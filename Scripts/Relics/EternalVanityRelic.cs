using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class EternalVanityRelic : ModRelicTemplate
{
    private const int CardsPerTurn = 1;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(CardsPerTurn)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this),
        HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
    ];

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

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player != Owner)
        {
            return;
        }

        IReadOnlyList<CardModel> candidates = Owner.Character.CardPool
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(card => card.Rarity is not (CardRarity.Basic or CardRarity.Ancient))
            .ToList();

        if (candidates.Count == 0)
        {
            return;
        }

        CardModel[] cards = CardFactory
            .GetDistinctForCombat(Owner, candidates, DynamicVars.Cards.IntValue, Owner.RunState.Rng.CombatCardGeneration)
            .ToArray();

        foreach (CardModel card in cards)
        {
            CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
        }

        Flash();
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, addedByPlayer: true);
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
