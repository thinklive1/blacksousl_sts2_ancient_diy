using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Three Little Pigs relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class ThreeLittlePigsRelic : ModRelicTemplate
{
    private const int BlockedCombats = 3;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private int _remainingCombats = BlockedCombats;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool ShowCounter => true;

    public override int DisplayAmount => BlackSouls_RemainingCombats;

    public override bool IsUsedUp => BlackSouls_RemainingCombats <= 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Combats", BlockedCombats)];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    [SavedProperty]
    public int BlackSouls_RemainingCombats
    {
        get => _remainingCombats;
        set
        {
            AssertMutable();
            _remainingCombats = Math.Max(0, value);
            InvokeDisplayAmountChanged();
            Status = IsUsedUp ? RelicStatus.Disabled : RelicStatus.Normal;
        }
    }

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom || BlackSouls_RemainingCombats <= 0)
        {
            return false;
        }

        rewards.Clear();
        BlackSouls_RemainingCombats--;
        Flash();
        return true;
    }
}
