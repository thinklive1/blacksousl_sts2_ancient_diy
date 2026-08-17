using System.Globalization;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Provides an initial purse and post-combat interest.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class QueenOfHeartsYellowDeckRelic : ModRelicTemplate
{
    private const int InitialGold = 100;
    private const int InterestPercent = 20;
    private const int InterestCap = 50;
    private const string RelicIconPath =
        "res://bs_ancient/assets/images/relics/QueenOfHeartsYellowDeckRelic.png";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new GoldVar(InitialGold),
        new DynamicVar("InterestPercent", InterestPercent),
        new GoldVar("InterestCap", InterestCap),
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath);

    public override bool IsAllowed(IRunState runState) => false;

    public override Task AfterObtained()
    {
        return PlayerCmd.GainGold(InitialGold, Owner);
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        int interest = Math.Min(
            InterestCap,
            (int)Math.Floor(Owner.Gold * (InterestPercent / 100d)));
        return interest > 0 ? PlayerCmd.GainGold(interest, Owner) : Task.CompletedTask;
    }
}
