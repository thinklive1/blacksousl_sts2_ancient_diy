using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterEnchantment]
public class TwinEnchantment : ModEnchantmentTemplate
{
    public override bool ShowAmount => false;

    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/TwinWaxStatueRelic.png"
    );

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card != Card || cardPlay.IsAutoPlay || Card.Owner?.Creature.CombatState == null)
        {
            return;
        }

        CardModel? twin = CardPile
            .GetCards(Card.Owner, PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust)
            .FirstOrDefault(IsPlayableTwin);
        if (twin == null)
        {
            return;
        }

        Creature? target = cardPlay.Target is { IsAlive: true } ? cardPlay.Target : null;
        await CardPileCmd.Add(twin, PileType.Play);
        await CardCmd.AutoPlay(context, twin, target);
    }

    private bool IsPlayableTwin(CardModel card)
    {
        return card != Card
            && card.Id == Card.Id
            && card.Enchantment is TwinEnchantment
            && card.Pile?.Type != PileType.Play;
    }
}
