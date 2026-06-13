using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterPower]
public sealed class OrrReflectionPendingPower : ModPowerTemplate
{
    private const string OrrIconPath = "res://bs_ancient/assets/images/powers/Orrpower.png";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: OrrIconPath,
        BigIconPath: OrrIconPath
    );

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Side)
        {
            await PowerCmd.Apply<OrrReflectionPower>(Owner, Amount, Owner, null);
            await PowerCmd.Remove(this);
        }
    }
}

[RegisterPower]
public sealed class OrrReflectionPower : ModPowerTemplate
{
    private const string OrrIconPath = "res://bs_ancient/assets/images/powers/Orrpower.png";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: OrrIconPath,
        BigIconPath: OrrIconPath
    );

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (Amount <= 0
            || cardPlay.IsAutoPlay
            || cardPlay.Card.Owner != Owner.Player
            || cardPlay.Card.Type != CardType.Attack
            || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        Creature? target = cardPlay.Target;
        if (cardPlay.Card.TargetType.IsSingleTarget() && cardPlay.Card.TargetType != TargetType.Self && target is not { IsAlive: true })
        {
            return;
        }

        await CardCmd.AutoPlay(context, cardPlay.Card, target, skipXCapture: true);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}

[RegisterPower]
public sealed class TwoSidedVirtuePower : ModPowerTemplate
{
    public const int PlaysPerTransform = 3;
    private const string TwoSidedVirtueIconPath = "res://bs_ancient/assets/images/powers/TwoSidedVirtuePower.png";

    private bool _transformQueued;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("PlaysPerTransform", PlaysPerTransform)
    ];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: TwoSidedVirtueIconPath,
        BigIconPath: TwoSidedVirtueIconPath
    );

    [SavedProperty]
    public bool BlackSouls_TransformQueued
    {
        get => _transformQueued;
        set
        {
            AssertMutable();
            _transformQueued = value;
        }
    }

    public override Task AfterPowerAmountChanged(PowerModel power, decimal originalAmount, Creature? source, CardModel? cardSource)
    {
        if (power == this && !BlackSouls_TransformQueued && Amount >= PlaysPerTransform)
        {
            QueueTransform();
        }

        return Task.CompletedTask;
    }

    public void QueueTransform()
    {
        BlackSouls_TransformQueued = true;
        SetAmount(PlaysPerTransform, silent: true);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (!BlackSouls_TransformQueued || Owner.Player == null)
        {
            return;
        }

        await PowerCmd.Remove(this);

        CardModel? selected = (await CardSelectCmd.FromDeckGeneric(
                Owner.Player,
                new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1),
                card => card.IsTransformable && card is not JackTheRipperReflectionCard))
            .FirstOrDefault();

        if (selected == null)
        {
            return;
        }

        CardModel jack = Owner.Player.RunState.CreateCard<JackTheRipperReflectionCard>(Owner.Player);
        await CardCmd.Transform(selected, jack);
    }
}
