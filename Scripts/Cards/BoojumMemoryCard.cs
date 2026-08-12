using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace BlackSouls.Scripts.Cards;

/// <summary>Replays the deck snapshot from one completed historical run.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class BoojumMemoryCard : ModCardTemplate
{
    private static readonly SavedAttachedState<BoojumMemoryCard, string> HistoryStartTimes = new(
        "BlackSouls_BoojumHistoryStartTime",
        static () => string.Empty);

    private List<SerializableCard> _historicalDeck = [];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/BoojumMemoryCard.jpg");

    public override string Title => FormatMemoryTitle(BlackSouls_HistoryStartTime);

    public BoojumMemoryCard() : base(0, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    [SavedProperty]
    public List<SerializableCard> BlackSouls_HistoricalDeck
    {
        get => _historicalDeck;
        set
        {
            AssertMutable();
            _historicalDeck = value ?? [];
        }
    }

    public long BlackSouls_HistoryStartTime
    {
        get => long.TryParse(HistoryStartTimes.GetValueOrDefault(this, string.Empty), out long value)
            ? value
            : 0L;
        set
        {
            AssertMutable();
            HistoryStartTimes.Set(this, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    public void Configure(BoojumMemoryRecord record)
    {
        BlackSouls_HistoricalDeck = record.Deck.ToList();
        BlackSouls_HistoryStartTime = record.StartTime;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combatState = CombatState;
        if (combatState == null || Owner == null)
        {
            return;
        }

        foreach (SerializableCard savedCard in BlackSouls_HistoricalDeck)
        {
            CardModel historicalCard;
            try
            {
                historicalCard = CardModel.FromSerializable(savedCard);
            }
            catch (Exception exception)
            {
                Entry.Logger.Warn($"Boojum skipped an unreadable remembered card: {exception.Message}");
                continue;
            }

            if (historicalCard is DeprecatedCard or StageEndCard)
            {
                continue;
            }

            try
            {
                combatState.AddCard(historicalCard, Owner);
                await CardCmd.AutoPlay(choiceContext, historicalCard, GetAutoPlayTarget(historicalCard));
            }
            catch (Exception exception)
            {
                Entry.Logger.Warn($"Boojum could not replay {historicalCard.Id.Entry}: {exception.Message}");
            }
            finally
            {
                if (historicalCard.Pile?.Type.IsCombatPile() == true)
                {
                    await CardPileCmd.RemoveFromCombat(historicalCard, skipVisuals: true);
                }
            }
        }
    }

    private Creature? GetAutoPlayTarget(CardModel card)
    {
        return card.TargetType switch
        {
            TargetType.Self or TargetType.AnyPlayer => Owner!.Creature,
            TargetType.AnyEnemy or TargetType.RandomEnemy => Owner!.RunState.Rng.CombatTargets.NextItem(
                CombatState?.HittableEnemies ?? Enumerable.Empty<Creature>()),
            TargetType.Osty => Owner!.Osty is { IsAlive: true } osty ? osty : null,
            _ => null
        };
    }

    private static string FormatMemoryTitle(long startTime)
    {
        if (startTime <= 0)
        {
            return "无日期的记忆";
        }

        try
        {
            DateTimeOffset localTime = DateTimeOffset.FromUnixTimeSeconds(startTime).ToLocalTime();
            return $"{localTime.Month:D2}月{localTime.Day:D2}日{localTime.Hour:D2}时{localTime.Minute:D2}分的记忆";
        }
        catch (ArgumentOutOfRangeException)
        {
            return "无日期的记忆";
        }
    }
}
