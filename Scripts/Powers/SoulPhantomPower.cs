using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Sts2Diy.Scripts.Powers;

/// <summary>Changes only the owner's combat copies and uses native synchronized card selection.</summary>
[RegisterPower]
public sealed class SoulPhantomPower : ModPowerTemplate
{
    private bool _upgradeSouls;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile
    {
        get
        {
            PowerModel original = ModelDb.Power<HauntPower>();
            return new(IconPath: original.IconPath, BigIconPath: original.ResolvedBigIconPath);
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Ethereal)];

    [SavedProperty]
    public bool Sts2Diy_UpgradeSouls
    {
        get => _upgradeSouls;
        set
        {
            AssertMutable();
            _upgradeSouls = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            description.Add("UpgradeSouls", IsMutable && Sts2Diy_UpgradeSouls);
            return description;
        }
    }

    public void Activate(bool upgraded)
    {
        AssertMutable();
        Sts2Diy_UpgradeSouls |= upgraded;
        // AllCards contains combat copies, never their permanent deck versions.
        foreach (CardModel card in Owner.Player?.PlayerCombatState?.AllCards.ToArray() ?? [])
        {
            EnhanceSoul(card);
        }
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (!CombatManager.Instance.IsOverOrEnding) EnhanceSoul(card);
        return Task.CompletedTask;
    }

    private void EnhanceSoul(CardModel card)
    {
        if (Amount <= 0 || card is not Soul || card.Owner != Owner.Player) return;
        if (Sts2Diy_UpgradeSouls && card.IsUpgradable) CardCmd.Upgrade(card);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card is not Soul || cardPlay.Card.Owner != Owner.Player || Owner.IsDead
            || Amount <= 0 || CombatManager.Instance.IsOverOrEnding) return;

        CardPile? drawPile = Owner.Player?.PlayerCombatState?.DrawPile;
        if (drawPile == null || !drawPile.Cards.Any(CanAddEthereal)) return;

        // The optional native selection synchronizes combat-card identities in multiplayer.
        IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(context, drawPile, Owner.Player!,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, Amount), CanAddEthereal);
        if (Owner.IsDead || CombatManager.Instance.IsOverOrEnding) return;
        foreach (CardModel card in selected)
        {
            if (card.Owner == Owner.Player && card.Pile == drawPile && CanAddEthereal(card))
            {
                CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
            }
        }
    }

    private static bool CanAddEthereal(CardModel card) => !card.Keywords.Contains(CardKeyword.Ethereal);
}
