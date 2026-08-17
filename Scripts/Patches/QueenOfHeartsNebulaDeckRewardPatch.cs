using MegaCrit.Sts2.Core.Rewards;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Records skipped card rewards for the Queen of Hearts' Nebula Deck.</summary>
public sealed class QueenOfHeartsNebulaDeckRewardPatch : IPatchMethod
{
    public static string PatchId => "queen_of_hearts_nebula_deck_skipped_reward";
    public static string Description => "Forget cards from skipped rewards for the Queen of Hearts' Nebula Deck.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardReward), nameof(CardReward.OnSkipped))];

    public static void Postfix(CardReward __instance)
    {
        QueenOfHeartsNebulaDeckRelic.RecordSkippedReward(__instance);
    }
}
