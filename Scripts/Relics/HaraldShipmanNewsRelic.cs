using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Harald Shipman News relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class HaraldShipmanNewsRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/news.png";
    private const int HealAmount = 5;
    private bool _usedPotionThisCombat;

    [SavedProperty]
    public int BlackSouls_CombatsWithoutPotion
    {
        get => _combatsWithoutPotion;
        set
        {
            AssertMutable();
            _combatsWithoutPotion = Math.Max(0, value);
            InvokeDisplayAmountChanged();
        }
    }

    private int _combatsWithoutPotion;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool ShowCounter => true;

    public override int DisplayAmount => BlackSouls_CombatsWithoutPotion;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override Task BeforeCombatStart()
    {
        _usedPotionThisCombat = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 使用药水时恢复生命，并重置未使用计数。
    /// </summary>
    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (Owner == null)
        {
            return;
        }

        _usedPotionThisCombat = true;
        BlackSouls_CombatsWithoutPotion = 0;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, HealAmount);
    }

    /// <summary>
    /// 战斗胜利时，若本场未使用药水，增加未使用计数并失去最大生命值。
    /// </summary>
    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Owner == null)
        {
            return;
        }

        if (!_usedPotionThisCombat)
        {
            BlackSouls_CombatsWithoutPotion++;
            Flash();
            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, false);
        }

        _usedPotionThisCombat = false;
    }
}
