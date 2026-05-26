using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class RethinkPokerRelic : ModRelicTemplate
{
    private const int CardChoices = 3;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/RethinkPokerRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/RethinkPokerRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/RethinkPokerRelic.png"
    );

    public override bool IsAllowed(IRunState runState)
    {
        if (runState.Players.Count != 1)
        {
            return false;
        }

        RunHistory? history = LoadLatestRunHistory();
        RunHistoryPlayer? historyPlayer = history?.Players.FirstOrDefault();
        return historyPlayer != null
            && IsOriginalCharacter(historyPlayer.Character)
            && historyPlayer.Deck.Any(IsUsableOriginalCard);
    }

    public override async Task AfterObtained()
    {
        if (Owner.RunState.Players.Count != 1)
        {
            return;
        }

        RunHistory? history = LoadLatestRunHistory();
        RunHistoryPlayer? historyPlayer = history?.Players.FirstOrDefault();
        if (history == null || historyPlayer == null)
        {
            return;
        }

        if (!IsOriginalCharacter(historyPlayer.Character))
        {
            return;
        }

        int previousGold = GetLastGold(history, historyPlayer.Id);
        if (previousGold > 0)
        {
            Flash();
            await PlayerCmd.GainGold(previousGold, Owner);
        }

        List<SerializableCard> candidates = historyPlayer.Deck
            .Where(IsUsableOriginalCard)
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        List<CardModel> options = PickRandomCards(candidates, CardChoices)
            .Select(CreateCardFromHistory)
            .ToList();

        CardModel? selected = (await CardSelectCmd.FromSimpleGrid(
            new BlockingPlayerChoiceContext(),
            options,
            Owner,
            new CardSelectorPrefs(L10NLookup(Id.Entry + ".selectionScreenPrompt"), 1))).FirstOrDefault();

        foreach (CardModel option in options.Where(option => option != selected))
        {
            Owner.RunState.RemoveCard(option);
        }

        if (selected != null)
        {
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(selected, PileType.Deck, source: this), 2f);
        }
    }

    private static RunHistory? LoadLatestRunHistory()
    {
        string? latestHistoryName = SaveManager.Instance.GetAllRunHistoryNames()
            .OrderByDescending(GetHistoryStartTime)
            .FirstOrDefault();
        if (latestHistoryName == null)
        {
            return null;
        }

        ReadSaveResult<RunHistory> result = SaveManager.Instance.LoadRunHistory(latestHistoryName);
        return result.Success ? result.SaveData : null;
    }

    private static long GetHistoryStartTime(string historyName)
    {
        string fileName = Path.GetFileNameWithoutExtension(historyName);
        return long.TryParse(fileName, out long startTime) ? startTime : 0;
    }

    private static int GetLastGold(RunHistory history, ulong playerId)
    {
        PlayerMapPointHistoryEntry? entry = history.MapPointHistory
            .SelectMany(act => act)
            .LastOrDefault(mapPoint => mapPoint.PlayerStats.Any(player => player.PlayerId == playerId))
            ?.GetEntry(playerId);

        return entry?.CurrentGold ?? 0;
    }

    private static bool IsUsableOriginalCard(SerializableCard serializableCard)
    {
        ModelId? id = serializableCard.Id;
        if (id == null || id.Category != "CARD")
        {
            return false;
        }

        CardModel card = SaveUtil.CardOrDeprecated(id);
        return card is not DeprecatedCard
            && card.GetType().Namespace == "MegaCrit.Sts2.Core.Models.Cards";
    }

    private static bool IsOriginalCharacter(ModelId characterId)
    {
        return SaveUtil.CharacterOrDeprecated(characterId) is Ironclad
            or Silent
            or Defect
            or Necrobinder
            or Regent;
    }

    private List<SerializableCard> PickRandomCards(List<SerializableCard> candidates, int count)
    {
        List<SerializableCard> remaining = [.. candidates];
        List<SerializableCard> selected = [];
        while (selected.Count < count && remaining.Count > 0)
        {
            SerializableCard? card = Owner.RunState.Rng.Niche.NextItem(remaining);
            if (card == null)
            {
                break;
            }

            selected.Add(card);
            remaining.Remove(card);
        }

        return selected;
    }

    private CardModel CreateCardFromHistory(SerializableCard serializableCard)
    {
        return Owner.RunState.LoadCard(serializableCard, Owner);
    }
}
