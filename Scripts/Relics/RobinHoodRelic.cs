using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
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

/// <summary>Implements the Robin Hood relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class RobinHoodRelic : ModRelicTemplate
{
    private const int DexterityAmount = 1;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private bool _lostHpThisCombat;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DexterityPower>(DexterityAmount)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DexterityPower>()
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
        _lostHpThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || _lostHpThisCombat)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<DexterityPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["DexterityPower"].BaseValue,
            Owner.Creature,
            null,
            false);
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner?.Creature && delta < 0)
        {
            _lostHpThisCombat = true;
        }

        return Task.CompletedTask;
    }
}
