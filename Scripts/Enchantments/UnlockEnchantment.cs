using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using System.Linq;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterEnchantment]
public sealed class UnlockEnchantment : ModEnchantmentTemplate, ICardPlayStateContributor
{
    private const string UnlockIconPath = "res://bs_ancient/assets/images/relics/SilverKeyRelic.png";

    private bool _usedThisCombat;

    public override bool HasExtraCardText => true;

    public override bool ShowAmount => true;

    public override int DisplayAmount => _usedThisCombat ? 0 : 1;

    public override EnchantmentAssetProfile AssetProfile => new(IconPath: UnlockIconPath);

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType is CardType.Attack or CardType.Skill or CardType.Power;
    }

    public override Task BeforeCombatStart()
    {
        _usedThisCombat = false;
        return Task.CompletedTask;
    }

    internal bool IsUnlockAvailable(CardModel card)
    {
        return card == Card && !_usedThisCombat;
    }

    public bool? CanPlay(CardModel card)
    {
        return IsUnlockAvailable(card) ? true : null;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (IsUnlockAvailable(card))
        {
            modifiedCost = 0m;
            return true;
        }

        modifiedCost = originalCost;
        return false;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUnlockAvailable(cardPlay.Card))
        {
            _usedThisCombat = true;
            foreach (CardModel card in cardPlay.Card.Owner?.PlayerCombatState?.AllCards.ToList() ?? [])
            {
                if (card.Affliction != null)
                {
                    CardCmd.ClearAffliction(card);
                }
            }
        }

        return Task.CompletedTask;
    }
}
