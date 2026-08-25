using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Displays the active Royal Chip condition and its live progress.</summary>
public abstract class RoyalChipConditionPowerBase : ModPowerTemplate
{
    private const string PowerIconPath =
        "res://bs_ancient/assets/images/relics/RoyalChipRelic.png";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => GetRelic()?.GetActiveConditionProgress() ?? 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Current", 0)
    ];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: PowerIconPath,
        BigIconPath: PowerIconPath);

    internal static void Refresh(Player player)
    {
        RoyalChipConditionPowerBase? power = player.Creature.Powers
            .OfType<RoyalChipConditionPowerBase>()
            .FirstOrDefault();
        power?.RefreshDisplay(power.GetRelic()?.GetActiveConditionProgress() ?? 0);
    }

    private RoyalChipRelic? GetRelic()
    {
        return Owner?.Player?.GetRelic<RoyalChipRelic>();
    }

    private void RefreshDisplay(int current)
    {
        DynamicVars["Current"].BaseValue = current;
        InvokeDisplayAmountChanged();
    }
}

/// <summary>Tracks the turn limit for a Royal Chip gamble.</summary>
[RegisterPower]
public sealed class RoyalChipBeforeTurnFivePower : RoyalChipConditionPowerBase
{
}

/// <summary>Tracks damage taken for a Royal Chip gamble.</summary>
[RegisterPower]
public sealed class RoyalChipHealthLossPower : RoyalChipConditionPowerBase
{
}

/// <summary>Tracks potion use for a Royal Chip gamble.</summary>
[RegisterPower]
public sealed class RoyalChipNoPotionPower : RoyalChipConditionPowerBase
{
}

/// <summary>Tracks cards played for a Royal Chip gamble.</summary>
[RegisterPower]
public sealed class RoyalChipCardLimitPower : RoyalChipConditionPowerBase
{
}

/// <summary>Tracks the highest number of times one card name was played.</summary>
[RegisterPower]
public sealed class RoyalChipUniqueCardNamesPower : RoyalChipConditionPowerBase
{
}

/// <summary>Tracks an overkill kill for a Royal Chip gamble.</summary>
[RegisterPower]
public sealed class RoyalChipOverkillKillPower : RoyalChipConditionPowerBase
{
}

/// <summary>Tracks Attack cards played for a Royal Chip gamble.</summary>
[RegisterPower]
public sealed class RoyalChipAttackLimitPower : RoyalChipConditionPowerBase
{
}

/// <summary>Tracks Skill cards played for a Royal Chip gamble.</summary>
[RegisterPower]
public sealed class RoyalChipSkillLimitPower : RoyalChipConditionPowerBase
{
}

/// <summary>Tracks Power cards played for a Royal Chip gamble.</summary>
[RegisterPower]
public sealed class RoyalChipAbilityLimitPower : RoyalChipConditionPowerBase
{
}
