using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Lake God Myth relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class LakeGodMythRelic : ModRelicTemplate
{
    private const int TriggerTurn = 3;
    private const int DexLoss = 2;
    private const int CorruptionAmount = 1;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/MythRelic.png";

    private int _ownerTurnCount;
    private bool _triggeredThisCombat;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Turn", TriggerTurn),
        new PowerVar<DexterityPower>(DexLoss),
        new PowerVar<CorruptionPower>(CorruptionAmount)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromPower<CorruptionPower>()
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override Task BeforeCombatStart()
    {
        _ownerTurnCount = 0;
        _triggeredThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (_triggeredThisCombat || player != Owner)
        {
            return;
        }

        _ownerTurnCount++;
        if (_ownerTurnCount < TriggerTurn)
        {
            return;
        }

        _triggeredThisCombat = true;
        Flash();

        await PowerCmd.Apply<DexterityPower>(
            choiceContext, Owner.Creature, -DexLoss, Owner.Creature, null, false);
        await PowerCmd.Apply<CorruptionPower>(
            choiceContext, Owner.Creature, CorruptionAmount, Owner.Creature, null, false);
    }
}
