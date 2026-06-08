using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

public abstract class AngelFeatherRelicBase : ModRelicTemplate
{
    private bool _convertedDamageThisCombat;
    private bool _unlockedLifestealThisCombat;
    private int _remainingLifesteal;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected virtual bool ConvertOnlyFirstDamage => false;

    protected virtual bool HasLifestealAfterFirstDamage => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<VigorPower>()];

    protected abstract string IconName { get; }

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://bs_ancient/assets/images/relics/{IconName}.png",
        IconOutlinePath: $"res://bs_ancient/assets/images/relics/{IconName}.png",
        BigIconPath: $"res://bs_ancient/assets/images/relics/{IconName}.png"
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override Task BeforeCombatStart()
    {
        _convertedDamageThisCombat = false;
        _unlockedLifestealThisCombat = false;
        _remainingLifesteal = 0;
        return SetBrutalizingAngelVisual(0);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner.Creature || result.UnblockedDamage <= 0)
        {
            return;
        }

        if (!ConvertOnlyFirstDamage || !_convertedDamageThisCombat)
        {
            _convertedDamageThisCombat = true;
            Flash();
            await PowerCmd.Apply<VigorPower>(Owner.Creature, result.UnblockedDamage, Owner.Creature, null);
        }

        if (HasLifestealAfterFirstDamage && !_unlockedLifestealThisCombat)
        {
            _unlockedLifestealThisCombat = true;
            _remainingLifesteal = result.UnblockedDamage;
            await SetBrutalizingAngelVisual(_remainingLifesteal);
        }
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (!HasLifestealAfterFirstDamage
            || !_unlockedLifestealThisCombat
            || _remainingLifesteal <= 0
            || !props.IsPoweredAttack()
            || result.UnblockedDamage + result.OverkillDamage <= 0
            || dealer == null
            || (dealer != Owner.Creature && dealer.PetOwner != Owner))
        {
            return;
        }

        int heal = Math.Min(_remainingLifesteal, result.UnblockedDamage + result.OverkillDamage);
        _remainingLifesteal -= heal;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, heal);
        await SetBrutalizingAngelVisual(_remainingLifesteal);
    }

    private Task SetBrutalizingAngelVisual(int amount)
    {
        if (!HasLifestealAfterFirstDamage || Owner?.Creature == null)
        {
            return Task.CompletedTask;
        }

        return PowerCmd.SetAmount<BrutalizingAngelPower>(Owner.Creature, amount, Owner.Creature, null);
    }
}

[RegisterRelic(typeof(EventRelicPool))]
public sealed class AngelFeatherRelic : AngelFeatherRelicBase
{
    protected override string IconName => nameof(AngelFeatherRelic);

    protected override bool ConvertOnlyFirstDamage => true;

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count == 1;
    }

    public override async Task AfterActEntered()
    {
        if (Owner.RunState.CurrentActIndex >= 1)
        {
            await RelicCmd.Replace(this, ModelDb.Relic<QuestionAngelFeatherRelic>().ToMutable());
        }
    }
}

[RegisterRelic(typeof(EventRelicPool))]
public sealed class QuestionAngelFeatherRelic : AngelFeatherRelicBase
{
    protected override string IconName => nameof(QuestionAngelFeatherRelic);

    public override async Task AfterActEntered()
    {
        if (Owner.RunState.CurrentActIndex >= 2)
        {
            await RelicCmd.Replace(this, ModelDb.Relic<BrutalizingAngelFeatherRelic>().ToMutable());
        }
    }
}

[RegisterRelic(typeof(EventRelicPool))]
public sealed class BrutalizingAngelFeatherRelic : AngelFeatherRelicBase
{
    protected override string IconName => "MurderAngelFeatherRelic";

    protected override bool HasLifestealAfterFirstDamage => true;
}
