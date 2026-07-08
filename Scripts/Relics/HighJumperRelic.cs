using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the High Jumper relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class HighJumperRelic : ModRelicTemplate
{
    private const int FlutterAmount = 2;
    private const int DamageTurn = 3;
    private const int DamageAmount = 15;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private int _ownerTurnsEnded;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<EdithFlutterPower>(FlutterAmount),
        new DynamicVar("Turn", DamageTurn),
        new DamageVar(DamageAmount, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<EdithFlutterPower>()
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
        _ownerTurnsEnded = 0;
        return ApplyFlutter();
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Creature.Side)
        {
            return;
        }

        _ownerTurnsEnded++;
        if (_ownerTurnsEnded != DamageTurn)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            DynamicVars["Damage"].BaseValue,
            ValueProp.Move,
            Owner.Creature,
            null);
    }

    private async Task ApplyFlutter()
    {
        Flash();
        await PowerCmd.Apply<EdithFlutterPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["EdithFlutterPower"].BaseValue,
            Owner.Creature,
            null,
            false);
    }
}
