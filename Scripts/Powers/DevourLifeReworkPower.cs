using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Sts2Diy.Scripts.Powers;

/// <summary>Separate counters preserve each copy's Soul destination when upgraded and normal copies coexist.</summary>
public abstract class DevourLifeReworkBase : ModPowerTemplate
{
    protected abstract PileType Destination { get; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: ModelDb.Power<DevourLifePower>().IconPath,
        BigIconPath: ModelDb.Power<DevourLifePower>().ResolvedBigIconPath);
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<Soul>()];

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combatState)
    {
        if (player != Owner.Player || Owner.IsDead || Amount <= 0 || CombatManager.Instance.IsOverOrEnding) return;
        int copies = Amount;
        var enemies = combatState.HittableEnemies.Where(c => c.IsAlive).ToArray();
        if (enemies.Length > 0)
            await CreatureCmd.Damage(context, enemies, copies,
                ValueProp.Unpowered | ValueProp.Unblockable | ValueProp.Move, Owner);
        int souls = copies * enemies.Length;
        for (int i = 0; i < souls; i++)
        {
            if (Owner.IsDead || CombatManager.Instance.IsOverOrEnding) return;
            CardModel soul = combatState.CreateCard<Soul>(player);
            await CardPileCmd.AddGeneratedCardToCombat(soul, Destination, player, CardPilePosition.Random);
        }
    }
}

[RegisterPower]
public sealed class DevourLifeDiscardPower : DevourLifeReworkBase
{
    protected override PileType Destination => PileType.Discard;
}

[RegisterPower]
public sealed class DevourLifeDrawPower : DevourLifeReworkBase
{
    protected override PileType Destination => PileType.Draw;
}
