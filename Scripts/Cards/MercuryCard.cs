using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

[RegisterCard(typeof(EventCardPool))]
public sealed class MercuryCard : ModCardTemplate
{
    private SerializableCard? _copiedCard;
    private string _copiedDescription = "";
    private int _copiedCost = -1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Retain,
        CardKeyword.Exhaust
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/MercuryCard.png"
    );

    public MercuryCard() : base(0, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    [SavedProperty]
    public SerializableCard? BlackSouls_CopiedCard
    {
        get => _copiedCard;
        set
        {
            AssertMutable();
            _copiedCard = value;
        }
    }

    [SavedProperty]
    public string BlackSouls_CopiedDescription
    {
        get => _copiedDescription;
        set
        {
            AssertMutable();
            _copiedDescription = value;
        }
    }

    [SavedProperty]
    public int BlackSouls_CopiedCost
    {
        get => _copiedCost;
        set
        {
            AssertMutable();
            _copiedCost = value;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = Owner;
        if (owner == null)
        {
            return;
        }

        SerializableCard? copiedCard = BlackSouls_CopiedCard;
        if (copiedCard == null)
        {
            CardModel? previousCard = FindPreviousPlayedCard();
            if (previousCard == null || !TryCopyFrom(previousCard))
            {
                return;
            }

            copiedCard = BlackSouls_CopiedCard;
            if (copiedCard == null)
            {
                return;
            }
        }

        CombatState? combatState = owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        CardModel copy = CardModel.FromSerializable(copiedCard);
        combatState.AddCard(copy, owner);
        Creature? target = GetAutoPlayTarget(copy, cardPlay.Target);
        if (copy.TargetType.IsSingleTarget() && copy.TargetType != TargetType.Self && target == null)
        {
            return;
        }

        await CardCmd.AutoPlay(choiceContext, copy, target, skipXCapture: true, skipCardPileVisuals: true);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner && TryCopyFrom(cardPlay.Card))
        {
            Owner?.PlayerCombatState?.RecalculateCardValues();
            RefreshVisuals();
        }

        return Task.CompletedTask;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card != this)
        {
            modifiedCost = originalCost;
            return false;
        }

        if (BlackSouls_CopiedCard == null || BlackSouls_CopiedCost < 0)
        {
            modifiedCost = originalCost;
            return false;
        }

        modifiedCost = BlackSouls_CopiedCost;
        return true;
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Creature.Side && CurrentUpgradeLevel <= 0 && BlackSouls_CopiedCard != null)
        {
            ClearCopiedCard();
        }

        return Task.CompletedTask;
    }

    private CardModel? FindPreviousPlayedCard()
    {
        return CombatManager.Instance.History.CardPlaysFinished
            .Select(entry => entry.CardPlay.Card)
            .LastOrDefault(card => card != this
                && card is not MercuryCard
                && !card.EnergyCost.CostsX);
    }

    private bool TryCopyFrom(CardModel card)
    {
        if (card == this || card is MercuryCard || card.EnergyCost.CostsX)
        {
            return false;
        }

        BlackSouls_CopiedCard = card.ToSerializable();
        BlackSouls_CopiedDescription = card.GetDescriptionForPile(PileType.Hand);
        BlackSouls_CopiedCost = card.EnergyCost.GetWithModifiers(CostModifiers.All);
        return true;
    }

    private void ClearCopiedCard()
    {
        BlackSouls_CopiedCard = null;
        BlackSouls_CopiedDescription = "";
        BlackSouls_CopiedCost = -1;
        Owner?.PlayerCombatState?.RecalculateCardValues();
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        NCard.FindOnTable(this)?.UpdateVisuals(Pile?.Type ?? PileType.Hand, CardPreviewMode.Normal);
    }

    private Creature? GetAutoPlayTarget(CardModel card, Creature? preferredTarget)
    {
        if (preferredTarget is { IsAlive: true } && preferredTarget.Side != Owner.Creature.Side)
        {
            return preferredTarget;
        }

        CombatState? combatState = Owner.Creature.CombatState;
        return card.TargetType switch
        {
            TargetType.Self or TargetType.AnyPlayer => Owner.Creature,
            TargetType.AnyEnemy or TargetType.RandomEnemy => Owner.RunState.Rng.CombatTargets.NextItem(
                combatState?.HittableEnemies ?? Enumerable.Empty<Creature>()),
            TargetType.Osty => Owner.Osty is { IsAlive: true } osty ? osty : null,
            _ => null
        };
    }
}
