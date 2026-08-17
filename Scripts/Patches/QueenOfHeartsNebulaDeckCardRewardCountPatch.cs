using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Adds choices to generated card rewards and suppresses exhausted Nebula Deck rewards.</summary>
public sealed class QueenOfHeartsNebulaDeckCardRewardCountPatch : IPatchMethod
{
    private const int ExtraRewardOptions = 3;

    public static string PatchId => "queen_of_hearts_nebula_deck_card_reward_count";
    public static string Description => "Add three card choices to rewards for the Queen of Hearts' Nebula Deck.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(
            typeof(CardFactory),
            nameof(CardFactory.CreateForReward),
            [typeof(Player), typeof(int), typeof(CardCreationOptions)])];

    public static bool Prefix(
        Player player,
        CardCreationOptions options,
        ref int cardCount,
        ref IEnumerable<CardCreationResult> __result)
    {
        if (player.GetRelic<QueenOfHeartsNebulaDeckRelic>() is not { } relic)
        {
            return true;
        }

        if (relic.TryGetSafeRewardOptionCount(player, options, cardCount + ExtraRewardOptions, out int safeCount))
        {
            cardCount = safeCount;
            return true;
        }

        __result = [];
        return false;
    }
}
