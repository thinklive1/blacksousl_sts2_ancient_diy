using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Sleep enchantment.</summary>
[RegisterEnchantment]
public class SleepEnchantment : ModEnchantmentTemplate
{
    private int _originalAmount;
    private bool _awakened;

    public override bool ShowAmount => true;

    public override bool HasExtraCardText => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("OriginalAmount", 0),
        new DynamicVar("PlayableDraw", 1)
    ];

    [SavedProperty]
    public int BlackSouls_OriginalAmount
    {
        get => _originalAmount;
        set
        {
            AssertMutable();
            _originalAmount = value;
        }
    }

    [SavedProperty]
    public bool BlackSouls_Awakened
    {
        get => _awakened;
        set
        {
            AssertMutable();
            _awakened = value;
        }
    }

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/enchantment/SleepEnchantment.png"
    );

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType is CardType.Attack or CardType.Skill;
    }

    protected override void OnEnchant()
    {
        BlackSouls_OriginalAmount = Amount;
        BlackSouls_Awakened = false;
        SyncDynamicVars();
    }

    public override void RecalculateValues()
    {
        base.RecalculateValues();
        SyncDynamicVars();
    }

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != Card || BlackSouls_OriginalAmount <= 0 || BlackSouls_Awakened)
        {
            return Task.CompletedTask;
        }

        if (Amount > 0)
        {
            Amount--;
            return Task.CompletedTask;
        }

        BlackSouls_Awakened = true;
        return Task.CompletedTask;
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card == Card && BlackSouls_OriginalAmount > 0 && !BlackSouls_Awakened)
        {
            return false;
        }

        return base.ShouldPlay(card, autoPlayType);
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card == Card && BlackSouls_OriginalAmount > 0 && BlackSouls_Awakened)
        {
            return playCount + BlackSouls_OriginalAmount;
        }

        return playCount;
    }

    private void SyncDynamicVars()
    {
        if (DynamicVars.TryGetValue("OriginalAmount", out DynamicVar? originalAmountVar) && originalAmountVar is not null)
        {
            originalAmountVar.BaseValue = BlackSouls_OriginalAmount;
        }

        if (DynamicVars.TryGetValue("PlayableDraw", out DynamicVar? playableDrawVar) && playableDrawVar is not null)
        {
            playableDrawVar.BaseValue = BlackSouls_OriginalAmount + 1;
        }
    }
}
