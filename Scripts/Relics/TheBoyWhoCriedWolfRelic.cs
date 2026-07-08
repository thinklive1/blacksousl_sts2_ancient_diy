using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the The Boy Who Cried Wolf relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class TheBoyWhoCriedWolfRelic : ModRelicTemplate
{
    private const int FirstTurnStrength = 3;
    private const int SecondTurnStrength = 5;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private int _ownerTurnCount;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("FirstTurnStrength", FirstTurnStrength),
        new DynamicVar("SecondTurnStrength", SecondTurnStrength)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override Task BeforeCombatStart()
    {
        _ownerTurnCount = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        _ownerTurnCount++;
        switch (_ownerTurnCount)
        {
            case 1:
                Flash();
                await PowerCmd.Apply<StrengthPower>(
                    choiceContext,
                    Owner.Creature,
                    DynamicVars["FirstTurnStrength"].BaseValue,
                    Owner.Creature,
                    null,
                    false);
                break;
            case 2:
                Flash();
                await PowerCmd.Apply<StrengthPower>(
                    choiceContext,
                    Owner.Creature,
                    DynamicVars["SecondTurnStrength"].BaseValue,
                    Owner.Creature,
                    null,
                    false);
                break;
            case 3:
                Flash();
                await PowerCmd.Remove<StrengthPower>(Owner.Creature);
                break;
        }
    }
}
