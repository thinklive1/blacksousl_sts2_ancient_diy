using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Makes the owner's next merchant purchase free.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class HeartJackRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/HeartJackRelic.png";

    private bool _pending = true;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool ShowCounter => true;

    public override int DisplayAmount => BlackSouls_Pending ? 1 : 0;

    public override bool IsUsedUp => !BlackSouls_Pending;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    [SavedProperty]
    public bool BlackSouls_Pending
    {
        get => _pending;
        set
        {
            AssertMutable();
            _pending = value;
            InvokeDisplayAmountChanged();
            Status = IsUsedUp ? RelicStatus.Disabled : RelicStatus.Normal;
        }
    }

    public override bool IsAllowed(IRunState runState) => false;

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice)
    {
        return player == Owner && BlackSouls_Pending ? 0 : originalPrice;
    }

    public override Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
    {
        if (player == Owner && BlackSouls_Pending)
        {
            Flash();
            BlackSouls_Pending = false;
        }

        return Task.CompletedTask;
    }
}
