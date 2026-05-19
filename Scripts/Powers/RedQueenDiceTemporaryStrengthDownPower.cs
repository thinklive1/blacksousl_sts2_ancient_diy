using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterPower]
public class RedQueenDiceTemporaryStrengthDownPower : ModPowerTemplate, ITemporaryPower
{
    private bool _shouldIgnoreNextInstance;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public AbstractModel OriginModel => ModelDb.Relic<RedQueenDiceRelic>();

    public PowerModel InternallyAppliedPower => ModelDb.Power<StrengthPower>();

    public override LocString Title => ModelDb.Relic<RedQueenDiceRelic>().Title;

    public override LocString Description => new("powers", "TEMPORARY_STRENGTH_DOWN.description");

    protected override string SmartDescriptionLocKey => "TEMPORARY_STRENGTH_DOWN.smartDescription";

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        .. HoverTipFactory.FromRelic(ModelDb.Relic<RedQueenDiceRelic>()),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/RedQueenDiceRelic.png"
    );

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_shouldIgnoreNextInstance)
        {
            _shouldIgnoreNextInstance = false;
            return;
        }

        await PowerCmd.Apply<StrengthPower>(target, -amount, applier, cardSource, silent: true);
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            if (_shouldIgnoreNextInstance)
            {
                _shouldIgnoreNextInstance = false;
                return;
            }

            await PowerCmd.Apply<StrengthPower>(Owner, -amount, applier, cardSource, silent: true);
        }
    }

    public void IgnoreNextInstance()
    {
        _shouldIgnoreNextInstance = true;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
        {
            return;
        }

        Flash();
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null);
    }
}
