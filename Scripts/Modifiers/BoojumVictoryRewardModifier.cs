using System.Text.Json;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Scaffolding.Content;
using BlackSouls.Scripts.Cards;

namespace BlackSouls.Scripts;

/// <summary>Stores Boojum's victory reward until the post-combat hook is dispatched.</summary>
public sealed class BoojumVictoryRewardModifier : ModModifierTemplate
{
    private const int RewardDeckCount = 3;
    private const string TransparentIconPath = "res://bs_ancient/assets/images/modifiers/TransparentModifier.png";
    private string _recordsJson = string.Empty;
    private bool _armed;

    public override ModifierAssetProfile AssetProfile => new(TransparentIconPath);

    [SavedProperty]
    public string BlackSouls_BoojumRewardRecords
    {
        get => _recordsJson;
        set
        {
            AssertMutable();
            _recordsJson = value ?? string.Empty;
        }
    }

    [SavedProperty]
    public bool BlackSouls_BoojumRewardArmed
    {
        get => _armed;
        set
        {
            AssertMutable();
            _armed = value;
        }
    }

    public static void Arm(Player player, IEnumerable<BoojumMemoryRecord> records)
    {
        if (player.RunState is not RunState runState)
        {
            return;
        }

        BoojumVictoryRewardModifier reward = runState.Modifiers
            .OfType<BoojumVictoryRewardModifier>()
            .FirstOrDefault()
            ?? Create(runState);
        reward.BlackSouls_BoojumRewardRecords = JsonSerializer.Serialize(CloneRecords(records));
        reward.BlackSouls_BoojumRewardArmed = true;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (!BlackSouls_BoojumRewardArmed || room.Encounter is not BoojumEventEncounter)
        {
            return;
        }

        // Disarm before opening the selector so this reward cannot be shown twice.
        BlackSouls_BoojumRewardArmed = false;
        if (RunState.Players is not [var player])
        {
            return;
        }

        List<CardModel> choices = CreateRewardChoices(player);
        Entry.Logger.Info($"Boojum prepared {choices.Count} card(s) from up to {RewardDeckCount} memory decks for the victory reward.");
        if (choices.Count == 0)
        {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(),
                choices,
                player,
                new CardSelectorPrefs(new LocString("events", "BS_ANCIENT_MONSTER_BOOJUM.selectionScreenPrompt"), 1, 1)
                {
                    RequireManualConfirmation = true
                }))
            .FirstOrDefault();

        foreach (CardModel choice in choices.Where(choice => choice != selected))
        {
            player.RunState.RemoveCard(choice);
        }

        if (selected != null)
        {
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.Add(selected, PileType.Deck, CardPilePosition.Top, this, false),
                2f);
        }
    }

    private static BoojumVictoryRewardModifier Create(RunState runState)
    {
        BoojumVictoryRewardModifier reward =
            (BoojumVictoryRewardModifier)ModelDb.Modifier<BoojumVictoryRewardModifier>().ToMutable();
        reward.OnRunLoaded(runState);
        runState.AddModifierDebug(reward);
        return reward;
    }

    private List<CardModel> CreateRewardChoices(Player player)
    {
        List<BoojumMemoryRecord> available = LoadRecords()
            .Where(record => record.Deck.Count > 0)
            .ToList();
        List<BoojumMemoryRecord> selectedRecords = [];
        while (available.Count > 0 && selectedRecords.Count < RewardDeckCount)
        {
            BoojumMemoryRecord? record = player.RunState.Rng.Niche.NextItem(available);
            if (record == null)
            {
                break;
            }

            available.Remove(record);
            selectedRecords.Add(record);
        }

        List<CardModel> choices = [];
        foreach (SerializableCard savedCard in selectedRecords.SelectMany(record => record.Deck))
        {
            try
            {
                CardModel card = player.RunState.LoadCard(savedCard, player);
                if (card is DeprecatedCard or StageEndCard)
                {
                    player.RunState.RemoveCard(card);
                    continue;
                }

                choices.Add(card);
            }
            catch (Exception exception)
            {
                Entry.Logger.Warn($"Boojum skipped an unreadable reward memory: {exception.Message}");
            }
        }

        return choices;
    }

    private List<BoojumMemoryRecord> LoadRecords()
    {
        try
        {
            return JsonSerializer.Deserialize<List<BoojumMemoryRecord>>(BlackSouls_BoojumRewardRecords) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<BoojumMemoryRecord> CloneRecords(IEnumerable<BoojumMemoryRecord> records)
    {
        return records
            .Where(record => BoojumHistoryMemory.IsSafeHistoryFileName(record.FileName))
            .Select(record => new BoojumMemoryRecord(
                record.FileName,
                record.RemainingCards,
                record.StartTime,
                record.Deck.ToList()))
            .ToList();
    }
}
