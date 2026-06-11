using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class AliceHandkerchiefRelic : ModRelicTemplate
{
    public const string RelicIconPath = "res://bs_ancient/assets/images/relics/AliceHandkerchiefRelic.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool ShowCounter => true;

    public override int DisplayAmount => GetModifier()?.BlackSouls_RemainingNodes ?? 0;

    public override bool IsUsedUp => GetModifier() is { BlackSouls_RemainingNodes: <= 0 };

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    internal void RefreshCounter()
    {
        Status = IsUsedUp ? RelicStatus.Disabled : RelicStatus.Normal;
        InvokeDisplayAmountChanged();
    }

    private AliceHandkerchiefModifier? GetModifier()
    {
        return Owner?.RunState.Modifiers.OfType<AliceHandkerchiefModifier>().FirstOrDefault();
    }
}
