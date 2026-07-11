using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
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

/// <summary>Implements the Shadow Demoness Myth relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class ShadowDemonessMythRelic : ModRelicTemplate
{
    private const int TurnsPerIntangible = 4;
    private const int IntangibleAmount = 1;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/MythRelic.png";

    private int _ownerTurnCount;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    public override int DisplayAmount => TurnsPerIntangible - (_ownerTurnCount % TurnsPerIntangible);

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Turns", TurnsPerIntangible),
        new PowerVar<IntangiblePower>(IntangibleAmount)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<IntangiblePower>(),
        HoverTipFactory.FromPower<NoEnergyGainPower>()
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    // This relic is granted only by Fairy Tale Mode, never by ordinary event rewards.
    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override Task BeforeCombatStart()
    {
        _ownerTurnCount = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        _ownerTurnCount++;
        InvokeDisplayAmountChanged();
        if (_ownerTurnCount % TurnsPerIntangible != 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<IntangiblePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["IntangiblePower"].BaseValue,
            Owner.Creature,
            null,
            false);
    }

    public override decimal ModifyEnergyGain(Player player, decimal amount)
    {
        // Turn-start energy is assigned directly by the game; this only blocks extra energy gains.
        return player == Owner ? 0m : amount;
    }

    public override Task AfterModifyingEnergyGain()
    {
        Flash();
        return Task.CompletedTask;
    }
}
