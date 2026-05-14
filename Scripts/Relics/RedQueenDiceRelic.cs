using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
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
    private const int DexterityGain = 3;
    private const int CardsPerDraw = 4;
    private const int RewardCards = 5;
    private const int StrengthBonusDuration = 2;

    private readonly List<TemporaryStrengthBonus> _temporaryStrengthBonuses = [];

    private int _roll;
    private int _cardsPlayedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress && _roll > 0;

    public override int DisplayAmount => _roll;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar("CardDrawPerTurn", CardDrawPerTurn),
        new PowerVar<DexterityPower>(DexterityGain),
        new CardsVar("CardsPerDraw", CardsPerDraw),
        new CardsVar("RewardCards", RewardCards)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/TimeQueenBlessingRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/TimeQueenBlessingRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/TimeQueenBlessingRelic.png"
    );

    public override async Task BeforeCombatStart()
    {
        _roll = Owner.RunState.Rng.Niche.NextInt(1, 7);
        _cardsPlayedThisCombat = 0;
        _temporaryStrengthBonuses.Clear();
        InvokeDisplayAmountChanged();

        Flash();

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

        await TickTemporaryStrengthBonuses();

        if (!HasEffect(2))
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
        _temporaryStrengthBonuses.Add(new TemporaryStrengthBonus(strength, StrengthBonusDuration));
    }

    public override async Task AfterCombatEnd(CombatRoom _)
    {
        _roll = 0;
        _cardsPlayedThisCombat = 0;
        _temporaryStrengthBonuses.Clear();
        InvokeDisplayAmountChanged();
        await Task.CompletedTask;
    }

    private bool HasEffect(int value)
    {
        return _roll == value || _roll == 6;
    }

    private async Task ChooseRareCard()
    {
        List<CardCreationResult> cards = CreateRareCardsFromOriginalCharacters();
        if (cards.Count == 0)
        {
            return;
        }

        foreach (CardModel card in await CardSelectCmd.FromSimpleGridForRewards(
                     context: new BlockingPlayerChoiceContext(),
                     cards: cards,
                     player: Owner,
                     prefs: new CardSelectorPrefs(L10NLookup(Id.Entry + ".selectionScreenPrompt"), 1)))
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true));
        }
    }

    private List<CardCreationResult> CreateRareCardsFromOriginalCharacters()
    {
        List<CardPoolModel> cardPools = [
            ModelDb.Character<Ironclad>().CardPool,
            ModelDb.Character<Silent>().CardPool,
            ModelDb.Character<Defect>().CardPool,
            ModelDb.Character<Regent>().CardPool,
            ModelDb.Character<Necrobinder>().CardPool
        ];

        List<CardCreationResult> cards = [];
        foreach (CardPoolModel cardPool in cardPools)
        {
            CardCreationOptions options = CardCreationOptions
                .ForNonCombatWithUniformOdds([cardPool], card => card.Rarity == CardRarity.Rare)
                .WithFlags(CardCreationFlags.NoRarityModification | CardCreationFlags.NoCardPoolModifications);
            cards.AddRange(CardFactory.CreateForReward(Owner, 1, options));
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

    private async Task TickTemporaryStrengthBonuses()
    {
        for (int i = _temporaryStrengthBonuses.Count - 1; i >= 0; i--)
        {
            TemporaryStrengthBonus bonus = _temporaryStrengthBonuses[i];
            bonus.TurnsRemaining--;
            if (bonus.TurnsRemaining > 0)
            {
                _temporaryStrengthBonuses[i] = bonus;
                continue;
            }

            _temporaryStrengthBonuses.RemoveAt(i);
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, -bonus.Amount, Owner.Creature, null);
        }
    }

    private struct TemporaryStrengthBonus
    {
        public TemporaryStrengthBonus(int amount, int turnsRemaining)
        {
            Amount = amount;
            TurnsRemaining = turnsRemaining;
        }

        public int Amount { get; }
        public int TurnsRemaining { get; set; }
    }
}
