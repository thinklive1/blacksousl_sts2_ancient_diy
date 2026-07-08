using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Lonely enchantment.</summary>
[RegisterEnchantment]
public class LonelyEnchantment : ModEnchantmentTemplate
{
    private const int ReplayCount = 1;

    public override bool ShowAmount => false;

    public override bool HasExtraCardText => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Times", ReplayCount)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.ReplayDynamic, DynamicVars["Times"])];

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/LonelyWaxStatueRelic.png"
    );

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card != Card || cardPlay.PlayIndex != 0 || Card.Owner?.Creature.CombatState == null)
        {
            return;
        }

        CardModel? sameNameCard = Card.Owner.RunState.Rng.CombatCardSelection.NextItem(
            CardPile
                .GetCards(Card.Owner, PileType.Draw, PileType.Hand, PileType.Discard)
                .Where(card => card != Card && card.Id == Card.Id)
                .ToList());
        if (sameNameCard?.Pile != null)
        {
            await CardCmd.Exhaust(context, sameNameCard);
            Card.BaseReplayCount += DynamicVars["Times"].IntValue;
        }
    }
}
