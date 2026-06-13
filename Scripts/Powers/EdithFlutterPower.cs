using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterPower]
public sealed class EdithFlutterPower : ModPowerTemplate
{
    private bool _hasSeenFirstPlayerTurnStart;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/powers/FlutterPower.png",
        BigIconPath: "res://bs_ancient/assets/images/powers/FlutterPower.png"
    );

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || !props.IsPoweredAttack())
        {
            return 1m;
        }

        return 0.5m;
    }

    [SavedProperty]
    public bool BlackSouls_HasSeenFirstPlayerTurnStart
    {
        get => _hasSeenFirstPlayerTurnStart;
        set
        {
            AssertMutable();
            _hasSeenFirstPlayerTurnStart = value;
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }

        if (!BlackSouls_HasSeenFirstPlayerTurnStart)
        {
            BlackSouls_HasSeenFirstPlayerTurnStart = true;
            return;
        }

        await PowerCmd.Remove(this);
    }
}
