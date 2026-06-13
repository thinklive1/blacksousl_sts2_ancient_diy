using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterActEvent(typeof(Hive))]
public sealed class HorrifyingGluttonEvent : ModEventTemplate
{
    private const int FoodGoldCost = 100;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://bs_ancient/assets/images/events/HorrifyingGluttonEvent.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(FoodGoldCost)];

    public override bool IsAllowed(IRunState runState)
    {
        return BsAncientConfig.EnableModEvents
            && runState.CurrentActIndex == 1
            && runState.Players.All(player =>
                HorrifyingGluttonRelic.HasAttackCandidate(player)
                || (player.Gold >= FoodGoldCost && HasAvailableFoodRelic(player)));
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool canHunt = HorrifyingGluttonRelic.HasAttackCandidate(Owner!);
        bool canBuyFood = Owner!.Gold >= FoodGoldCost && HasAvailableFoodRelic(Owner);

        return
        [
            RelicOption<HorrifyingGluttonRelic>(
                canHunt ? ObtainHorrifyingGluttonRelic : null,
                InitialOptionKey(canHunt ? "HUNT" : "HUNT_LOCKED")),
            new EventOption(
                this,
                canBuyFood ? BuyFood : null,
                InitialOptionKey(canBuyFood ? "BUY_FOOD" : "BUY_FOOD_LOCKED")),
        ];
    }

    private async Task ObtainHorrifyingGluttonRelic()
    {
        await RelicCmd.Obtain<HorrifyingGluttonRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.HUNT.description"));
    }

    private async Task BuyFood()
    {
        await PlayerCmd.LoseGold(FoodGoldCost, Owner!, GoldLossType.Spent);
        RelicModel relic = RelicFactory.PullNextRelicFromFront(Owner!, RollFoodRelicRarity(), IsFoodRelic).ToMutable();
        await RelicCmd.Obtain(relic, Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.BUY_FOOD.description"));
    }

    private RelicRarity RollFoodRelicRarity()
    {
        bool hasCommon = HasAvailableFoodRelic(Owner!, RelicRarity.Common);
        bool hasUncommon = HasAvailableFoodRelic(Owner!, RelicRarity.Uncommon);

        if (hasCommon && hasUncommon)
        {
            RelicRarity rarity = RelicFactory.RollRarity(Owner!);
            return rarity == RelicRarity.Common ? RelicRarity.Common : RelicRarity.Uncommon;
        }

        return hasCommon ? RelicRarity.Common : RelicRarity.Uncommon;
    }

    private static bool HasAvailableFoodRelic(Player player)
    {
        return HasAvailableFoodRelic(player, RelicRarity.Common)
            || HasAvailableFoodRelic(player, RelicRarity.Uncommon);
    }

    private static bool HasAvailableFoodRelic(Player player, RelicRarity rarity)
    {
        if (!player.RelicGrabBag.ToSerializable().RelicIdLists.TryGetValue(rarity, out List<ModelId>? relicIds))
        {
            return false;
        }

        return relicIds
            .Select(ModelDb.GetByIdOrNull<RelicModel>)
            .Any(relic => relic != null && IsFoodRelic(relic) && relic.IsAllowed(player.RunState));
    }

    private static bool IsFoodRelic(RelicModel relic)
    {
        return relic.Rarity is RelicRarity.Common or RelicRarity.Uncommon;
    }
}
