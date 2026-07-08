using BlackSouls.Scripts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Implements the Candy House Candy card.</summary>
public abstract class CandyHouseCandyCard : ModCardTemplate
{
    private bool _isAutoPlaying;

    protected abstract string CandyPortraitPath { get; }

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: CandyPortraitPath
    );

    protected CandyHouseCandyCard() : base(0, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this || _isAutoPlaying || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        _isAutoPlaying = true;
        try
        {
            await CardCmd.AutoPlay(choiceContext, this, null);
        }
        finally
        {
            _isAutoPlaying = false;
        }
    }
}

/// <summary>Implements the Sweet Candy card.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class SweetCandyCard : CandyHouseCandyCard
{
    protected override string CandyPortraitPath => "res://bs_ancient/assets/images/cards/SweetCandyCard.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HealVar(6)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
    }
}

/// <summary>Implements the Bitter Candy card.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class BitterCandyCard : CandyHouseCandyCard
{
    protected override string CandyPortraitPath => "res://bs_ancient/assets/images/cards/BitterCandyCard.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.LoseEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
}

/// <summary>Implements the Hallucinogenic Candy card.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class HallucinogenicCandyCard : CandyHouseCandyCard
{
    protected override string CandyPortraitPath => "res://bs_ancient/assets/images/cards/HallucinogenicCandyCard.png";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HallucinogenicCandyPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this,
            false);
    }
}
