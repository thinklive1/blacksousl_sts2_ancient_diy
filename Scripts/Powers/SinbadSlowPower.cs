using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterPower]
public sealed class SinbadSlowPower : ModPowerTemplate
{
    private const int DamageTakenPercentPerCard = 10;
    private const int ActivePowerOffset = 1;
    private const string PowerIconPath = "res://bs_ancient/assets/images/powers/SinbadSlowPower.png";

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => CardsPlayedThisTurn;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("DamageTakenPercent", DamageTakenPercentPerCard)
    ];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: PowerIconPath,
        BigIconPath: PowerIconPath
    );

    private int CardsPlayedThisTurn => Math.Max(0, Amount - ActivePowerOffset);

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || CardsPlayedThisTurn <= 0)
        {
            return 1m;
        }

        return 1m + CardsPlayedThisTurn * DamageTakenPercentPerCard / 100m;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner || Owner.IsDead || CombatManager.Instance.IsOverOrEnding)
        {
            return Task.CompletedTask;
        }

        SetAmount(Amount + 1, silent: true);
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner) && Amount != 0)
        {
            SetAmount(ActivePowerOffset, silent: true);
        }

        return Task.CompletedTask;
    }
}
