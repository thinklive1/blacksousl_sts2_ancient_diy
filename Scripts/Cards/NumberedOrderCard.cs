using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Advances the original decree once after a combat in which it was played.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class NumberedOrderCard : ModCardTemplate
{
    internal const int FirstOrderNumber = 42;
    private int _orderNumber = FirstOrderNumber;
    private bool _pendingAdvance;

    public NumberedOrderCard() : base(0, CardType.Power, CardRarity.Ancient, TargetType.AnyEnemy) { }

    public override int MaxUpgradeLevel => 1;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/NumberedOrderCard.jpg");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<OrderDetentionPower>(2)];

    public override string Title
    {
        get
        {
            var title = new LocString("cards", "BS_ANCIENT_CARD_NUMBERED_ORDER_CARD.numberedTitle");
            title.Add("Number", BlackSouls_OrderNumber);
            return title.GetFormattedText() + (IsUpgraded ? "+" : string.Empty);
        }
    }

    [SavedProperty]
    public int BlackSouls_OrderNumber
    {
        get => _orderNumber;
        set
        {
            AssertMutable();
            _orderNumber = Math.Max(FirstOrderNumber, value);
            EnergyCost.SetCustomBaseCost(GetOrderCost(_orderNumber));
        }
    }

    [SavedProperty]
    public bool BlackSouls_PendingAdvance
    {
        get => _pendingAdvance;
        set { AssertMutable(); _pendingAdvance = value; }
    }

    internal static int GetOrderCost(int number) => Math.Max(0, number - FirstOrderNumber);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);

    protected override void AfterDeserialized()
    {
        EnergyCost.SetCustomBaseCost(GetOrderCost(BlackSouls_OrderNumber));
    }

    protected override void AfterDowngraded()
    {
        // Native downgrades reset custom costs to the canonical zero-energy version.
        EnergyCost.SetCustomBaseCost(GetOrderCost(BlackSouls_OrderNumber));
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("NextNumber", (long)BlackSouls_OrderNumber + 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // Only the permanent original accrues a change; generated copies have no deck original.
        if (DeckVersion is NumberedOrderCard original && original.Pile?.Type == PileType.Deck)
        {
            original.BlackSouls_PendingAdvance = true;
        }

        if (cardPlay.Target is not { IsAlive: true, Monster: not null } target)
        {
            return;
        }

        OrderDetentionPower? existing = target.GetPower<OrderDetentionPower>();
        string resumeMove = existing?.BlackSouls_ResumeMoveId ?? target.Monster.NextMove.Id;
        await PowerCmd.Apply<OrderDetentionPower>(
            choiceContext, target, 2, Owner.Creature, this);
        // A prevented debuff can return an unattached model; only stun if it was applied.
        OrderDetentionPower? detention = target.GetPower<OrderDetentionPower>();
        if (detention != null)
        {
            detention.BlackSouls_ResumeMoveId = resumeMove;
            await detention.EnsureStunned();
        }
    }

    public override Task BeforeCombatStart()
    {
        BlackSouls_PendingAdvance = false;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Pile?.Type == PileType.Deck && Owner.Creature.IsAlive && AdvanceIfPending())
        {
            CardCmd.Preview(this);
        }
        return Task.CompletedTask;
    }

    internal bool AdvanceIfPending()
    {
        if (!BlackSouls_PendingAdvance) return false;
        BlackSouls_PendingAdvance = false;
        if (BlackSouls_OrderNumber == int.MaxValue) return false;
        BlackSouls_OrderNumber++;
        return true;
    }
}
