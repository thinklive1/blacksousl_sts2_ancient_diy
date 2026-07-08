using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the My Former Rascal relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class MyFormerRascalRelic : ModRelicTemplate
{
    private const int StrengthGain = 5;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private bool _pending = true;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool IsUsedUp => !BlackSouls_Pending;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<StrengthPower>(StrengthGain)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

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
            if (IsUsedUp)
            {
                Status = RelicStatus.Disabled;
            }
        }
    }

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override async Task BeforeCombatStart()
    {
        if (!BlackSouls_Pending
            || Owner.RunState.CurrentRoom is not CombatRoom { RoomType: RoomType.Monster or RoomType.Elite }
            || Owner.Creature.CombatState is not { } combatState)
        {
            return;
        }

        Flash();
        foreach (Creature enemy in combatState.Enemies.Where(enemy => enemy.IsAlive).ToList())
        {
            await PowerCmd.Apply<StrengthPower>(
                new ThrowingPlayerChoiceContext(),
                enemy,
                DynamicVars["StrengthPower"].BaseValue,
                Owner.Creature,
                null,
                false);
        }

        BlackSouls_Pending = false;
    }
}
