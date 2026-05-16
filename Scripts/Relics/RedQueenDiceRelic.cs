using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class RedQueenDiceRelic : ModRelicTemplate
{
    private const int CardDrawPerTurn = 1;
    private const int StrengthGain = 2;
    private const int StrengthDoubleTurn = 2;
    private const int DexterityGain = 3;
    private const int CardsPerDraw = 4;
    private const int RewardCards = 5;

    private int _roll;
    private int _cardsPlayedThisCombat;
    private int _ownerTurnsEndedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress && _roll > 0;

    public override int DisplayAmount => _roll;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar("CardDrawPerTurn", CardDrawPerTurn),
        new PowerVar<StrengthPower>(StrengthGain),
        new DynamicVar("StrengthDoubleTurn", StrengthDoubleTurn),
        new PowerVar<DexterityPower>(DexterityGain),
        new CardsVar("CardsPerDraw", CardsPerDraw),
        new CardsVar("RewardCards", RewardCards)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png"
    );

    public override async Task BeforeCombatStart()
    {
        _roll = Owner.RunState.Rng.Niche.NextInt(1, IsMultiplayerRun() ? 6 : 7);
        _cardsPlayedThisCombat = 0;
        _ownerTurnsEndedThisCombat = 0;
        InvokeDisplayAmountChanged();

        Flash();

        if (HasEffect(2))
        {
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, StrengthGain, Owner.Creature, null);
        }

        if (HasEffect(3))
        {
            await PowerCmd.Apply<DexterityPower>(Owner.Creature, DexterityGain, Owner.Creature, null);
        }
    }

    public override async Task BeforeCombatStartLate()
    {
        if (HasEffect(5))
        {
            await ChooseRareCard();
        }

        if (HasEffect(6))
        {
            await DoubleEnemyHp();
        }
    }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner || !HasEffect(1))
        {
            return count;
        }

        return count + CardDrawPerTurn;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || !HasEffect(4) || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        _cardsPlayedThisCombat++;
        if (_cardsPlayedThisCombat % CardsPerDraw != 0)
        {
            return;
        }

        Flash();
        await CardPileCmd.Draw(context, 1, Owner);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Creature.Side)
        {
            return;
        }

        if (!HasEffect(2))
        {
            return;
        }

        _ownerTurnsEndedThisCombat++;
        if (_ownerTurnsEndedThisCombat != StrengthDoubleTurn)
        {
            return;
        }

        int strength = Owner.Creature.GetPowerAmount<StrengthPower>();
        if (strength <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, strength, Owner.Creature, null);
    }

    public override async Task AfterCombatEnd(CombatRoom _)
    {
        _roll = 0;
        _cardsPlayedThisCombat = 0;
        _ownerTurnsEndedThisCombat = 0;
        InvokeDisplayAmountChanged();
        await Task.CompletedTask;
    }

    private bool HasEffect(int value)
    {
        return _roll == value || _roll == 6;
    }

    private bool IsMultiplayerRun()
    {
        return Owner.RunState.Players.Count > 1;
    }

    private async Task ChooseRareCard()
    {
        List<CardModel> cards = CreateRareCardsFromOriginalCharacters();
        if (cards.Count == 0)
        {
            return;
        }

        foreach (CardModel card in await CardSelectCmd.FromSimpleGrid(
                     new BlockingPlayerChoiceContext(),
                     cards,
                     Owner,
                     new CardSelectorPrefs(L10NLookup(Id.Entry + ".selectionScreenPrompt"), 1)))
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true));
        }
    }

    private List<CardModel> CreateRareCardsFromOriginalCharacters()
    {
        CombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return [];
        }

        List<CardPoolModel> cardPools = [
            ModelDb.Character<Ironclad>().CardPool,
            ModelDb.Character<Silent>().CardPool,
            ModelDb.Character<Defect>().CardPool,
            ModelDb.Character<Regent>().CardPool,
            ModelDb.Character<Necrobinder>().CardPool
        ];

        List<CardModel> cards = [];
        foreach (CardPoolModel cardPool in cardPools)
        {
            List<CardModel> candidates = cardPool
                .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                .Where(card => card.Rarity == CardRarity.Rare && card.CanBeGeneratedInCombat)
                .ToList();
            if (candidates.Count == 0)
            {
                continue;
            }

            CardModel? selectedCard = Owner.RunState.Rng.CombatCardGeneration.NextItem(candidates);
            if (selectedCard == null)
            {
                continue;
            }

            cards.Add(combatState.CreateCard(selectedCard, Owner));
        }

        return cards;
    }

    private async Task DoubleEnemyHp()
    {
        CombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        foreach (Creature enemy in combatState.Enemies.Where(enemy => enemy.IsAlive).ToList())
        {
            await CreatureCmd.SetMaxHp(enemy, enemy.MaxHp * 2);
            await CreatureCmd.SetCurrentHp(enemy, enemy.CurrentHp * 2);
        }
    }

}
