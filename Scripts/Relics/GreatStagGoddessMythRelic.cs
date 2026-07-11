using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Great Stag Goddess Myth relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class GreatStagGoddessMythRelic : ModRelicTemplate
{
    private const int CostIncrease = 1;
    private const int CardsToReturn = 1;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/MythRelic.png";
    private readonly HashSet<CardModel> _cardsThatEnteredExhaust = [];

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(CostIncrease),
        new CardsVar(CardsToReturn)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
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

    public override Task BeforeCombatStart()
    {
        _cardsThatEnteredExhaust.Clear();
        return Task.CompletedTask;
    }

    public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner == Owner)
        {
            _cardsThatEnteredExhaust.Add(card);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        CardPile exhaustPile = PileType.Exhaust.GetPile(Owner);
        if (exhaustPile.Cards.Count == 0)
        {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                exhaustPile,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, CardsToReturn, CardsToReturn)))
            .FirstOrDefault();

        if (selected == null)
        {
            return;
        }

        Flash();
        await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Top);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner != Owner || card.EnergyCost.CostsX || originalCost < 0)
        {
            return false;
        }

        if (!_cardsThatEnteredExhaust.Contains(card))
        {
            return false;
        }

        modifiedCost = originalCost + DynamicVars.Energy.BaseValue;
        return true;
    }
}
