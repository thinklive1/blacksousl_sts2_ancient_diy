using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Sts2Diy.Scripts.Cards;

/// <summary>Accepts Doom in exchange for immediate card draw.</summary>
[RegisterCard(typeof(NecrobinderCardPool))]
public sealed class PoisonedDraughtCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://sts2diy/assets/images/cards/PoisonedDraughtCard.jpg");

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<DoomPower>(6m), new CardsVar(2)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DoomPower>()];

    public PoisonedDraughtCard() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        // New cards use native Doom directly, not the vanilla-card amplification rules.
        await PowerCmd.Apply<DoomPower>(context, Owner.Creature,
            DynamicVars.Doom.BaseValue, Owner.Creature, this);
        if (Owner.Creature.IsAlive)
        {
            await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Doom.UpgradeValueBy(-3m);
}
