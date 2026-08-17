using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Adds Colorless cards to rewards and permanently removes passed-over cards from future rewards.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class QueenOfHeartsNebulaDeckRelic : ModRelicTemplate
{
    private const string RelicIconPath =
        "res://bs_ancient/assets/images/relics/QueenOfHeartsNebulaDeckRelic.png";

    [SavedProperty]
    public string BlackSouls_ForgottenCardIds
    {
        get => _forgottenCardIdsSerialized;
        set
        {
            AssertMutable();
            _forgottenCardIdsSerialized = value ?? string.Empty;
            _forgottenCardIds = null;
        }
    }

    private string _forgottenCardIdsSerialized = string.Empty;
    private HashSet<string>? _forgottenCardIds;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath);

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override CardCreationOptions ModifyCardRewardCreationOptions(
        Player player,
        CardCreationOptions options)
    {
        if (player != Owner
            || options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications)
            || !options.Flags.HasFlag(CardCreationFlags.IsCardReward)
            || options.CustomCardPool != null)
        {
            return options;
        }

        Func<CardModel, bool>? existingFilter = options.CardPoolFilter;
        IEnumerable<CardPoolModel> pools = GetRewardPools(player, options);

        return options.WithCardPools(
            pools,
            card => (existingFilter?.Invoke(card) ?? true)
                && IsRewardEligible(card)
                && !ForgottenCardIds.Contains(GetCardKey(card)));
    }

    /// <summary>Caps a generated reward to its remaining unique cards, or reports that the reward pool is exhausted.</summary>
    public bool TryGetSafeRewardOptionCount(
        Player player,
        CardCreationOptions options,
        int requestedCount,
        out int safeCount)
    {
        safeCount = requestedCount;
        if (player != Owner || options.CustomCardPool != null || requestedCount <= 0)
        {
            return true;
        }

        int availableCount = GetAvailableRewardCardCount(player, options);
        safeCount = Math.Min(requestedCount, availableCount);
        return availableCount > 0;
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner)
        {
            return false;
        }

        int removed = rewards.RemoveAll(reward => reward is CardReward && !reward.IsPopulated);
        if (removed > 0)
        {
            Entry.Logger.Info("Queen of Hearts' Nebula Deck removed an exhausted card reward.");
            Flash();
        }

        return removed > 0;
    }

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> cardRewardOptions,
        CardCreationOptions creationOptions)
    {
        if (player != Owner || ForgottenCardIds.Count == 0)
        {
            return false;
        }

        bool modified = false;
        foreach (CardCreationResult option in cardRewardOptions)
        {
            if (!ForgottenCardIds.Contains(GetCardKey(option.Card)))
            {
                continue;
            }

            CardModel? replacement = CreateReplacement(player, option.Card, creationOptions, cardRewardOptions);
            if (replacement == null)
            {
                continue;
            }

            option.ModifyCard(replacement, this);
            modified = true;
        }

        if (modified)
        {
            Flash();
        }

        return modified;
    }

    public override Task AfterRewardTaken(Player player, Reward reward)
    {
        if (player == Owner && reward is CardReward cardReward)
        {
            ForgetCards(cardReward.Cards);
        }

        return Task.CompletedTask;
    }

    /// <summary>Records every card that was left behind when a card reward was skipped.</summary>
    public static void RecordSkippedReward(CardReward reward)
    {
        reward.Player.GetRelic<QueenOfHeartsNebulaDeckRelic>()?.ForgetCards(reward.Cards);
    }

    private HashSet<string> ForgottenCardIds =>
        _forgottenCardIds ??= DeserializeForgottenCardIds(BlackSouls_ForgottenCardIds);

    private void ForgetCards(IEnumerable<CardModel> cards)
    {
        bool changed = false;
        foreach (CardModel card in cards)
        {
            changed |= ForgottenCardIds.Add(GetCardKey(card));
        }

        if (!changed)
        {
            return;
        }

        BlackSouls_ForgottenCardIds = JsonSerializer.Serialize(ForgottenCardIds);
        Flash();
    }

    private int GetAvailableRewardCardCount(Player player, CardCreationOptions options)
    {
        Func<CardModel, bool>? existingFilter = options.CardPoolFilter;
        return GetRewardPools(player, options)
            .SelectMany(pool => pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            .Where(card => existingFilter?.Invoke(card) ?? true)
            .Where(IsRewardEligible)
            .Where(card => !ForgottenCardIds.Contains(GetCardKey(card)))
            .Select(GetCardKey)
            .Distinct()
            .Count();
    }

    private static IEnumerable<CardPoolModel> GetRewardPools(Player player, CardCreationOptions options)
    {
        // Some custom rewards omit the character pool entirely. Always retain it before adding Colorless cards.
        return options.CardPools
            .Append(player.Character.CardPool)
            .Append(ModelDb.CardPool<ColorlessCardPool>())
            .Distinct();
    }

    private CardModel? CreateReplacement(
        Player player,
        CardModel original,
        CardCreationOptions options,
        IEnumerable<CardCreationResult> existingOptions)
    {
        HashSet<string> displayedCardIds = existingOptions
            .Select(option => GetCardKey(option.Card))
            .ToHashSet();
        List<CardModel> candidates = options.GetPossibleCards(player)
            .Where(IsRewardEligible)
            .Where(card => !ForgottenCardIds.Contains(GetCardKey(card)))
            .Where(card => card.Rarity == original.Rarity)
            .Where(card => !displayedCardIds.Contains(GetCardKey(card)))
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = options.GetPossibleCards(player)
                .Where(IsRewardEligible)
                .Where(card => !ForgottenCardIds.Contains(GetCardKey(card)))
                .Where(card => card.Rarity == original.Rarity)
                .ToList();
        }

        CardModel? candidate = candidates.Count == 0
            ? null
            : player.RunState.Rng.Niche.NextItem(candidates);
        return candidate == null ? null : player.RunState.CreateCard(candidate, player);
    }

    private static string GetCardKey(CardModel card)
    {
        return card.Id.Entry;
    }

    private static bool IsRewardEligible(CardModel card)
    {
        return card.Rarity is not CardRarity.Basic and not CardRarity.Ancient;
    }

    private static HashSet<string> DeserializeForgottenCardIds(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<HashSet<string>>(serialized) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
