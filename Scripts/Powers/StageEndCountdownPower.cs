using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using BlackSouls.Scripts.Cards;

namespace BlackSouls.Scripts;

/// <summary>Implements the Stage End Countdown power.</summary>
[RegisterPower]
public class StageEndCountdownPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/StageEndRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/StageEndRelic.png"
    );

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner || Owner.IsDead)
        {
            return;
        }

        if (cardPlay.Card is StageEndCard)
        {
            return;
        }

        Flash();
        SetAmount(Math.Max(0, Amount - 1), silent: true);

        if (!Owner.IsDead && Amount <= 0)
        {
            await CreatureCmd.Kill(Owner, force: true);
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        BsAncientAudio.StopStageEndLoop();
        return Task.CompletedTask;
    }
}
