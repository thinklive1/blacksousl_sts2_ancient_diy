using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Implements the Banai Reflection card.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class BanaiReflectionCard : ModCardTemplate
{
    private const int Block = 12;
    private const int UpgradeBlock = 3;
    private const int CopyBlockLoss = 4;
    private const int UpgradeCopyBlockLoss = -1;
    private const int SanLoss = 5;

    private int _copyBlockLoss;
    private bool _isMirrorCopy;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(Block, ValueProp.Move),
        new DynamicVar("CopyBlockLoss", CopyBlockLoss),
        new DynamicVar("SanLoss", SanLoss)
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/BanaiReflectionCard.jpg"
    );

    public BanaiReflectionCard() : base(1, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    [SavedProperty]
    public int BlackSouls_CopyBlockLoss
    {
        get => _copyBlockLoss;
        set
        {
            AssertMutable();
            _copyBlockLoss = value;
        }
    }

    [SavedProperty]
    public bool BlackSouls_IsMirrorCopy
    {
        get => _isMirrorCopy;
        set
        {
            AssertMutable();
            _isMirrorCopy = value;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, Math.Max(0, DynamicVars.Block.BaseValue - BlackSouls_CopyBlockLoss), DynamicVars.Block.Props, cardPlay);
        await MirrorSan.Change(Owner, -SanLoss);

        ICombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        CardModel copy = combatState.CreateCard<BanaiReflectionCard>(Owner);
        if (copy is BanaiReflectionCard banaiCopy)
        {
            ConfigureMirrorCopy(banaiCopy, this);
        }

        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Draw, Owner, CardPilePosition.Random);
        RefreshPileCounter(result, PileType.Draw);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card == this && BlackSouls_IsMirrorCopy)
        {
            modifiedCost = 0;
            return true;
        }

        modifiedCost = originalCost;
        return false;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(UpgradeBlock);
        DynamicVars["CopyBlockLoss"].UpgradeValueBy(UpgradeCopyBlockLoss);
    }

    private static void ConfigureMirrorCopy(BanaiReflectionCard copy, BanaiReflectionCard source)
    {
        bool upgraded = source.CurrentUpgradeLevel > 0;
        if (upgraded)
        {
            CardCmd.Upgrade(copy);
        }

        int copyBlockLoss = source.DynamicVars["CopyBlockLoss"].IntValue;
        int nextBlock = Math.Max(0, source.DynamicVars.Block.IntValue - copyBlockLoss);
        copy.BlackSouls_IsMirrorCopy = true;
        copy.BlackSouls_CopyBlockLoss = 0;
        copy.DynamicVars.Block.UpgradeValueBy(nextBlock - copy.DynamicVars.Block.IntValue);
        copy.DynamicVars.RecalculateForUpgradeOrEnchant();
    }

    private void RefreshPileCounter(CardPileAddResult result, PileType pileType)
    {
        if (result.success)
        {
            pileType.GetPile(Owner).InvokeCardAddFinished();
        }
    }
}

/// <summary>Implements the Orr Reflection card.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class OrrReflectionCard : ModCardTemplate
{
    private const int Cost = 3;
    private const int UpgradeCost = 2;
    private const int Heal = 3;
    private const int UpgradeHeal = 2;
    private const int SanLoss = 30;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Heal", Heal),
        new DynamicVar("SanLoss", SanLoss)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<OrrReflectionPendingPower>()
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/OrrReflectionCard.jpg"
    );

    public OrrReflectionCard() : base(Cost, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
        await MirrorSan.Change(Owner, -SanLoss);
        await PowerCmd.Apply<OrrReflectionPendingPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, this, false);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(UpgradeCost - Cost);
        DynamicVars["Heal"].UpgradeValueBy(UpgradeHeal);
        AddKeyword(CardKeyword.Retain);
    }
}

/// <summary>Implements the Holmes Reflection card.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class HolmesReflectionCard : ModCardTemplate
{
    private const int Options = 3;
    private const int SanGain = 20;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(Options),
        new DynamicVar("SanGain", SanGain)
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/HolmesReflectionCard.jpg"
    );

    public HolmesReflectionCard() : base(2, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> cards = CardFactory.GetDistinctForCombat(
            Owner,
            ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint),
            DynamicVars.Cards.IntValue,
            Owner.RunState.Rng.CombatCardGeneration).ToList();

        if (CurrentUpgradeLevel > 0)
        {
            CardCmd.Upgrade(cards, CardPreviewStyle.HorizontalLayout);
        }

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, cards, Owner, canSkip: true);
        if (selected != null)
        {
            await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner, CardPilePosition.Top);
        }

        await MirrorSan.Change(Owner, SanGain);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add(
            "UpgradeText",
            CurrentUpgradeLevel > 0
                ? new LocString("cards", $"{Id.Entry}.upgradeText").GetFormattedText()
                : string.Empty);
    }
}

/// <summary>Implements the Jack The Ripper Reflection card.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class JackTheRipperReflectionCard : ModCardTemplate
{
    private const int Damage = 4;
    private const int UpgradeDamage = 4;
    private const int DamageGainPerPlay = 2;
    private const int SanLoss = 5;

    private int _damageBonus;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(Damage, ValueProp.Move),
        new DynamicVar("DamageGain", DamageGainPerPlay),
        new DynamicVar("PlaysPerTransform", TwoSidedVirtuePower.PlaysPerTransform),
        new DynamicVar("JackPlaysRemaining", TwoSidedVirtuePower.PlaysPerTransform),
        new DynamicVar("SanLoss", SanLoss)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<TwoSidedVirtuePower>()
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/JackTheRipperReflectionCard.jpg"
    );

    public JackTheRipperReflectionCard() : base(0, CardType.Attack, CardRarity.Event, TargetType.AnyEnemy)
    {
    }

    [SavedProperty]
    public int BlackSouls_DamageBonus
    {
        get => _damageBonus;
        set
        {
            AssertMutable();
            _damageBonus = value;
            TrySyncDamageBonus();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
        {
            return;
        }

        await CreatureCmd.Damage(
            choiceContext,
            cardPlay.Target,
            DynamicVars.Damage.BaseValue,
            DynamicVars.Damage.Props,
            Owner.Creature,
            this);

        GainDamageBonus(DamageGainPerPlay);
        if (ShouldQueueTwoSidedVirtue())
        {
            await PowerCmd.Apply<TwoSidedVirtuePower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), Owner.Creature, TwoSidedVirtuePower.PlaysPerTransform, Owner.Creature, this, false);
            Owner.Creature.GetPower<TwoSidedVirtuePower>()?.QueueTransform();
        }

        Owner.PlayerCombatState?.RecalculateCardValues();

        if (Owner.GetRelic<EdithRingRelic>() == null)
        {
            await MirrorSan.Change(Owner, -SanLoss);
        }
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("JackPlaysRemaining", GetJackPlaysRemaining());
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamage);
        SyncDamageBonus();
    }

    private int GetJackPlaysRemaining()
    {
        if (IsCanonical)
        {
            return TwoSidedVirtuePower.PlaysPerTransform;
        }

        TwoSidedVirtuePower? power = Owner?.Creature?.GetPower<TwoSidedVirtuePower>();
        if (power?.BlackSouls_TransformQueued == true)
        {
            return 0;
        }

        return MirrorSan.Get(Owner)?.GetJackReflectionPlaysRemaining()
            ?? TwoSidedVirtuePower.PlaysPerTransform;
    }

    private bool ShouldQueueTwoSidedVirtue()
    {
        if (Owner == null || Owner.Creature?.GetPower<TwoSidedVirtuePower>()?.BlackSouls_TransformQueued == true)
        {
            return false;
        }

        return MirrorSan.Ensure(Owner).RegisterJackReflectionPlay();
    }

    private void GainDamageBonus(int amount)
    {
        JackTheRipperReflectionCard? deckCard = DeckVersion as JackTheRipperReflectionCard;
        if (deckCard != null && deckCard != this)
        {
            deckCard.BlackSouls_DamageBonus += amount;
            BlackSouls_DamageBonus = deckCard.BlackSouls_DamageBonus;
            return;
        }

        BlackSouls_DamageBonus += amount;
    }

    private void SyncDamageBonus()
    {
        int baseDamage = Damage + (CurrentUpgradeLevel > 0 ? UpgradeDamage : 0);
        int targetDamage = baseDamage + BlackSouls_DamageBonus;
        DynamicVars.Damage.UpgradeValueBy(targetDamage - DynamicVars.Damage.IntValue);
        DynamicVars.RecalculateForUpgradeOrEnchant();
    }

    private void TrySyncDamageBonus()
    {
        try
        {
            SyncDamageBonus();
        }
        catch (InvalidOperationException)
        {
            // Saved properties can be restored before dynamic vars are ready.
        }
        catch (KeyNotFoundException)
        {
            // Older saves or early construction may not have the display var yet.
        }
    }
}

/// <summary>Implements the Liddell Reflection card.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class LiddellReflectionCard : ModCardTemplate
{
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Eternal,
        CardKeyword.Ethereal,
        CardKeyword.Unplayable
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/LiddellReflectionCard.jpg"
    );

    public LiddellReflectionCard() : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
    {
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card != this || Owner?.Creature?.CombatState == null)
        {
            return;
        }

        List<CardModel> sourceCards = [
            .. PileType.Draw.GetPile(Owner).Cards,
            .. PileType.Hand.GetPile(Owner).Cards,
            .. PileType.Discard.GetPile(Owner).Cards
        ];

        List<CardModel> copies = sourceCards
            .Where(sourceCard => sourceCard.Pile != null)
            .Select(sourceCard =>
            {
                CardModel copy = sourceCard.CreateClone();
                copy.AddKeyword(CardKeyword.Ethereal);
                return copy;
            })
            .ToList();

        if (copies.Count > 0)
        {
            await CardPileCmd.AddGeneratedCardsToCombat(
                copies,
                PileType.Discard,
                Owner,
                CardPilePosition.Random);
            RefreshPileCounter(PileType.Discard, copies.Count);
        }
    }

    private void RefreshPileCounter(PileType pileType, int addedCount)
    {
        CardPile pile = pileType.GetPile(Owner);
        pile.InvokeContentsChanged();

        for (int i = 0; i < addedCount; i++)
        {
            pile.InvokeCardAddFinished();
        }
    }
}

/// <summary>Implements the Pervasive Malice card.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class PervasiveMaliceCard : ModCardTemplate
{
    private const int Energy = 1;
    private const int Draw = 3;
    private const int SanLoss = 15;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(Energy),
        new CardsVar(Draw),
        new DynamicVar("SanLoss", SanLoss)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        EnergyHoverTip
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/PervasiveMaliceCard.jpg"
    );

    public PervasiveMaliceCard() : base(0, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await MirrorSan.Change(Owner, -SanLoss);
    }
}
