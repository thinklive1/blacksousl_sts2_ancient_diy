using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
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

/// <summary>Implements the Snow Queen relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class SnowQueenRelic : ModRelicTemplate
{
    private const int SlowAmount = 1;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<SlowPower>(SlowAmount)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<SlowPower>()
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

    public override async Task BeforeCombatStart()
    {
        if (Owner.Creature.CombatState == null)
        {
            return;
        }

        Flash();
        foreach (Creature enemy in Owner.Creature.CombatState.Enemies.Where(enemy => enemy.IsAlive).ToList())
        {
            await PowerCmd.Apply<SlowPower>(
                new ThrowingPlayerChoiceContext(),
                enemy,
                DynamicVars["SlowPower"].BaseValue,
                Owner.Creature,
                null,
                false);
        }
    }
}
