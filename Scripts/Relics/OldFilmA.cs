using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public class OldFilmA : ModRelicTemplate
{
    private const int PowerAmount = 1;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<ViolenceDemonPower>(PowerAmount)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RelicHoverTipHelpers.Details(this),
        HoverTipFactory.FromPower<ViolenceDemonPower>()
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/OldFilmA.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/OldFilmA.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/OldFilmA.png"
    );

    public override async Task BeforeCombatStart()
    {
        if (Owner.Creature.Powers.OfType<ViolenceDemonPower>().Any())
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<ViolenceDemonPower>(
            new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["ViolenceDemonPower"].BaseValue,
            Owner.Creature,
            null,
            false);
    }
}
