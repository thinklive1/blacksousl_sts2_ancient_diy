using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class AliceRibbonRelic : ModRelicTemplate
{
    private const int StrengthGain = 10;

    private bool _wasUsed;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool IsUsedUp => BlackSouls_WasUsed;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(StrengthGain)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/AliceRibbonRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/AliceRibbonRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/AliceRibbonRelic.png"
    );

    [SavedProperty]
    public bool BlackSouls_WasUsed
    {
        get => _wasUsed;
        set
        {
            AssertMutable();
            _wasUsed = value;
            if (IsUsedUp)
            {
                Status = RelicStatus.Disabled;
            }
        }
    }

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner.Creature || BlackSouls_WasUsed)
        {
            return true;
        }

        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();
        BlackSouls_WasUsed = true;
        await CreatureCmd.Heal(creature, creature.MaxHp);
        await PowerCmd.Apply<StrengthPower>(creature, DynamicVars["StrengthPower"].BaseValue, creature, null);
    }
}
