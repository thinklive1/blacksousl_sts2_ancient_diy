using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Implements the Executioner Evolution card.</summary>
public abstract class ExecutionerEvolutionCard<TNext> : ModCardTemplate where TNext : CardModel
{
    private int _playCount;

    protected abstract int Damage { get; }

    protected abstract int UpgradeDamage { get; }

    protected abstract int RequiredPlays { get; }

    protected abstract string CardPortraitPath { get; }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(MyKeywords.KillingBlow)
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(Damage, ValueProp.Move),
        new DynamicVar("RequiredPlays", RequiredPlays),
        new DynamicVar("RemainingPlays", RequiredPlays)
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: CardPortraitPath
    );

    protected ExecutionerEvolutionCard(int cost) : base(cost, CardType.Attack, CardRarity.Event, TargetType.AnyEnemy)
    {
    }

    [SavedProperty]
    public int BlackSouls_PlayCount
    {
        get => _playCount;
        set
        {
            AssertMutable();
            _playCount = value;
            TrySyncRemainingPlays();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
        {
            return;
        }

        Creature target = cardPlay.Target;
        await CreatureCmd.Damage(
            choiceContext,
            target,
            DynamicVars.Damage.BaseValue,
            DynamicVars.Damage.Props,
            Owner.Creature,
            this);

        if (target.IsDead)
        {
            await IncrementDeckPlayCount();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamage);
    }

    protected async Task IncrementDeckPlayCount()
    {
        ExecutionerEvolutionCard<TNext> deckCard = DeckVersion as ExecutionerEvolutionCard<TNext> ?? this;
        deckCard.BlackSouls_PlayCount++;
        deckCard.SyncRemainingPlays();

        if (deckCard.BlackSouls_PlayCount < RequiredPlays)
        {
            return;
        }

        CardModel nextCard = Owner.RunState.CreateCard<TNext>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(nextCard, PileType.Deck, MegaCrit.Sts2.Core.Entities.Cards.CardPilePosition.Top, this, false), 2f);

        if (deckCard.Pile?.Type == PileType.Deck)
        {
            await CardPileCmd.RemoveFromDeck(deckCard);
        }
    }

    private void SyncRemainingPlays()
    {
        int remaining = Math.Max(0, RequiredPlays - BlackSouls_PlayCount);
        DynamicVar remainingVar = DynamicVars["RemainingPlays"];
        remainingVar.UpgradeValueBy(remaining - remainingVar.IntValue);
        DynamicVars.RecalculateForUpgradeOrEnchant();
    }

    private void TrySyncRemainingPlays()
    {
        try
        {
            SyncRemainingPlays();
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

/// <summary>Implements the Wriggling Shadow card.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class WrigglingShadowCard : ExecutionerEvolutionCard<XiZhiCard>
{
    protected override string CardPortraitPath => "res://bs_ancient/assets/images/cards/WrigglingShadowCard.png";

    protected override int Damage => 6;

    protected override int UpgradeDamage => 3;

    protected override int RequiredPlays => 3;

    public WrigglingShadowCard() : base(0)
    {
    }
}

/// <summary>Implements the Xi Zhi card.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class XiZhiCard : ExecutionerEvolutionCard<ExecutionerCard>
{
    protected override string CardPortraitPath => "res://bs_ancient/assets/images/cards/XiZhiCard.png";

    protected override int Damage => 18;

    protected override int UpgradeDamage => 6;

    protected override int RequiredPlays => 5;

    public XiZhiCard() : base(1)
    {
    }
}

/// <summary>Implements the Executioner card.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class ExecutionerCard : ExecutionerEvolutionCard<ExecutionerKetchCard>
{
    protected override string CardPortraitPath => "res://bs_ancient/assets/images/cards/ExecutionerCard.png";

    protected override int Damage => 28;

    protected override int UpgradeDamage => 9;

    protected override int RequiredPlays => 3;

    public ExecutionerCard() : base(2)
    {
    }
}

/// <summary>Implements the Executioner Ketch card.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class ExecutionerKetchCard : ModCardTemplate
{
    private const int Damage = 54;
    private const int UpgradeDamage = 12;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(Damage, ValueProp.Move)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/ExecutionerKetchCard.png"
    );

    public ExecutionerKetchCard() : base(4, CardType.Attack, CardRarity.Event, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
        {
            return;
        }

        await RemovePowerIfPresent<ArtifactPower>(cardPlay.Target);

        await CreatureCmd.Damage(
            choiceContext,
            cardPlay.Target,
            DynamicVars.Damage.BaseValue,
            DynamicVars.Damage.Props,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamage);
    }

    private static Task RemovePowerIfPresent<TPower>(MegaCrit.Sts2.Core.Entities.Creatures.Creature target)
        where TPower : PowerModel
    {
        TPower? power = target.GetPower<TPower>();
        return power == null ? Task.CompletedTask : PowerCmd.Remove(power);
    }
}
