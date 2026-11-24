using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Sts2Diy.Scripts.Cards;

/// <summary>Turns the current hand Ethereal and summons separately for each newly affected card.</summary>
[RegisterCard(typeof(NecrobinderCardPool))]
public sealed class VoidPassageCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://sts2diy/assets/images/cards/VoidPassageCard.jpg");
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new SummonVar(2m)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Ethereal),
        HoverTipFactory.Static(StaticHoverTip.SummonDynamic, DynamicVars.Summon)
    ];

    public VoidPassageCard() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        CardPile? hand = Owner.PlayerCombatState?.Hand;
        if (hand == null || Owner.Creature.IsDead || CombatManager.Instance.IsOverOrEnding) return;

        int affected = 0;
        // Snapshot the hand before Summon reactions can draw or move cards.
        foreach (CardModel card in hand.Cards.ToArray())
        {
            if (card == this || card.Owner != Owner || card.Pile != hand
                || card.Keywords.Contains(CardKeyword.Ethereal)) continue;
            CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
            if (card.Keywords.Contains(CardKeyword.Ethereal)) affected++;
        }

        for (int i = 0; i < affected; i++)
        {
            if (Owner.Creature.IsDead || CombatManager.Instance.IsOverOrEnding) break;
            await OstyCmd.Summon(context, Owner, DynamicVars.Summon.BaseValue, this);
        }
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
