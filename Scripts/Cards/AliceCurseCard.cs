using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

[RegisterCard(typeof(EventCardPool))]
public class AliceCurseCard : ModCardTemplate
{
    private const int SelfDamage = 1;
    private const int DrawOnExhaust = 1;
    private const int CopiesToAdd = 2;
    private const int IntangibleGain = 1;
    private const int UndeadGain = 1;
    private const int StrengthGain = 2;
    private const int DexterityGain = 1;
    private const int PlatingGain = 5;
    private const int MadnessGain = 1;
    private const int RandomDrawGain = 2;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Exhaust,
        CardKeyword.Ethereal
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(SelfDamage, ValueProp.Unpowered | ValueProp.Move),
        new CardsVar(DrawOnExhaust),
        new DynamicVar("Copies", CopiesToAdd),
        new PowerVar<IntangiblePower>(IntangibleGain),
        new PowerVar<UndeadPower>(UndeadGain),
        new PowerVar<StrengthPower>(StrengthGain),
        new PowerVar<DexterityPower>(DexterityGain),
        new PowerVar<PlatingPower>(PlatingGain),
        new PowerVar<MadnessPower>(MadnessGain),
        new DynamicVar("RandomDraw", RandomDrawGain)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<IntangiblePower>(),
        HoverTipFactory.FromPower<UndeadPower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromPower<PlatingPower>(),
        HoverTipFactory.FromPower<MadnessPower>()
    ];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://bs_ancient/assets/images/cards/AliceCurseCard.png"
    );

    public AliceCurseCard() : base(0, CardType.Curse, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card != this || Owner?.Creature is not { IsDead: false })
        {
            return;
        }

        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            DynamicVars.Damage.BaseValue,
            DynamicVars.Damage.Props,
            Owner.Creature,
            this
        );

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await AddCopiesToCombat();
        await ApplyRandomEffect(choiceContext);
    }

    private async Task AddCopiesToCombat()
    {
        CombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        CardModel drawCopy = combatState.CreateCard<AliceCurseCard>(Owner);
        CardModel discardCopy = combatState.CreateCard<AliceCurseCard>(Owner);

        await CardPileCmd.AddGeneratedCardToCombat(drawCopy, PileType.Draw, addedByPlayer: true, CardPilePosition.Random);
        await CardPileCmd.AddGeneratedCardToCombat(discardCopy, PileType.Discard, addedByPlayer: true);
    }

    private async Task ApplyRandomEffect(PlayerChoiceContext choiceContext)
    {
        switch (Owner.RunState.Rng.CombatCardSelection.NextInt(7))
        {
            case 0:
                await PowerCmd.Apply<IntangiblePower>(Owner.Creature, DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, this);
                break;
            case 1:
                await PowerCmd.Apply<UndeadPower>(Owner.Creature, DynamicVars["UndeadPower"].BaseValue, Owner.Creature, this);
                break;
            case 2:
                await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this);
                break;
            case 3:
                await PowerCmd.Apply<DexterityPower>(Owner.Creature, DynamicVars["DexterityPower"].BaseValue, Owner.Creature, this);
                break;
            case 4:
                await PowerCmd.Apply<PlatingPower>(Owner.Creature, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, this);
                break;
            case 5:
                if (!Owner.Creature.Powers.OfType<MadnessPower>().Any())
                {
                    await PowerCmd.Apply<MadnessPower>(Owner.Creature, DynamicVars["MadnessPower"].BaseValue, Owner.Creature, this);
                }
                break;
            case 6:
                await CardPileCmd.Draw(choiceContext, DynamicVars["RandomDraw"].BaseValue, Owner);
                break;
        }
    }
}
