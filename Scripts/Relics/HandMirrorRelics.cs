using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

public abstract class HandMirrorRelicBase : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool HasUponPickupEffect => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => MirrorSan.GetValue(Owner);

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

    public override Task AfterObtained()
    {
        MirrorSan.Ensure(Owner);
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public static void RefreshAllCounters(IRunState? runState)
    {
        if (runState == null)
        {
            return;
        }

        foreach (HandMirrorRelicBase relic in runState.Players.SelectMany(player => player.Relics).OfType<HandMirrorRelicBase>())
        {
            relic.InvokeDisplayAmountChanged();
        }
    }

    protected async Task AddCardToDeck<T>() where T : CardModel
    {
        CardModel card = Owner.RunState.CreateCard<T>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck, MegaCrit.Sts2.Core.Entities.Cards.CardPilePosition.Top, this, false), 2f);
    }
}

[RegisterRelic(typeof(EventRelicPool))]
public sealed class PumpkinHandMirrorRelic : HandMirrorRelicBase
{
    protected override string IconName => nameof(PumpkinHandMirrorRelic);

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<OrrReflectionCard>();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await AddCardToDeck<OrrReflectionCard>();
    }
}

[RegisterRelic(typeof(EventRelicPool))]
public sealed class RabbitHandMirrorRelic : HandMirrorRelicBase
{
    protected override string IconName => nameof(RabbitHandMirrorRelic);

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<BanaiReflectionCard>();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await AddCardToDeck<BanaiReflectionCard>();
    }
}

[RegisterRelic(typeof(EventRelicPool))]
public sealed class JackHandMirrorRelic : HandMirrorRelicBase
{
    protected override string IconName => nameof(JackHandMirrorRelic);

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<HolmesReflectionCard>()
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<JackTheRipperReflectionCard>());

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await AddCardToDeck<HolmesReflectionCard>();
        await AddCardToDeck<JackTheRipperReflectionCard>();
        AddRouteModifier();
    }

    private void AddRouteModifier()
    {
        if (Owner.RunState is not RunState runState)
        {
            return;
        }

        JackHandMirrorRouteModifier modifier = (JackHandMirrorRouteModifier)ModelDb.Modifier<JackHandMirrorRouteModifier>().ToMutable();
        modifier.OnRunLoaded(runState);
        modifier.Configure(runState.CurrentActIndex + 1);
        runState.AddModifierDebug(modifier);
    }
}

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class EdithRingRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        new[] { HoverTipFactory.FromPower<EdithFlutterPower>() };

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/EdithRingRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/EdithRingRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/EdithRingRelic.png"
    );

    public override Task BeforeCombatStart()
    {
        return PowerCmd.Apply<EdithFlutterPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, null, false);
    }
}

[RegisterRelic(typeof(EventRelicPool))]
public sealed class GirlHandMirrorRelic : HandMirrorRelicBase
{
    private bool _reflectedThisCombat;

    protected override string IconName => nameof(GirlHandMirrorRelic);

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<LiddellReflectionCard>();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await AddCardToDeck<LiddellReflectionCard>();
    }

    public override Task BeforeCombatStart()
    {
        _reflectedThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (_reflectedThisCombat
            || target != Owner.Creature
            || result.UnblockedDamage <= 0
            || dealer is not { IsAlive: true }
            || dealer == Owner.Creature)
        {
            return;
        }

        _reflectedThisCombat = true;
        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            dealer,
            result.UnblockedDamage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            Owner.Creature,
            null);
    }
}

public sealed class JackHandMirrorRouteModifier : ModModifierTemplate
{
    private int _targetActIndex = -1;

    public override ModifierAssetProfile AssetProfile => new("res://bs_ancient/assets/images/modifiers/JackHandMirrorRouteModifier.png");

    [SavedProperty]
    public int BlackSouls_TargetActIndex
    {
        get => _targetActIndex;
        set
        {
            AssertMutable();
            _targetActIndex = value;
        }
    }

    public void Configure(int targetActIndex)
    {
        AssertMutable();
        BlackSouls_TargetActIndex = targetActIndex;
    }

    public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
    {
        if (actIndex == BlackSouls_TargetActIndex)
        {
            ApplyMonsterEliteRoute(map);
        }

        return map;
    }

    protected override void AfterRunLoaded(RunState runState)
    {
        if (runState.CurrentActIndex == BlackSouls_TargetActIndex)
        {
            ApplyMonsterEliteRoute(runState.Map);
        }
    }

    private static void ApplyMonsterEliteRoute(ActMap map)
    {
        MapPoint? current = map.GetPointsInRow(0).OrderBy(point => point.coord.col).FirstOrDefault();
        if (current == null)
        {
            return;
        }

        int row = 1;
        while (true)
        {
            MapPoint? next = current.Children.OrderBy(point => point.coord.col).FirstOrDefault();
            if (next == null)
            {
                break;
            }

            next.PointType = row % 3 == 0 ? MapPointType.Elite : MapPointType.Monster;
            current = next;
            row++;
        }
    }
}
