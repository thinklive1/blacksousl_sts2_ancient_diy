using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
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
    private const int FoodGoldCost = 75;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://bs_ancient/assets/images/events/HorrifyingGluttonEvent.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(FoodGoldCost)];

    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex == 1
            && runState.Players.All(player =>
                HorrifyingGluttonRelic.HasAttackCandidate(player)
                || (player.Gold >= FoodGoldCost && player.RelicGrabBag.HasAvailableRelics(runState)));
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool canHunt = HorrifyingGluttonRelic.HasAttackCandidate(Owner!);
        bool canBuyFood = Owner!.Gold >= FoodGoldCost && Owner.RelicGrabBag.HasAvailableRelics(Owner.RunState);

        return
        [
            RelicOption<HorrifyingGluttonRelic>(canHunt ? ObtainHorrifyingGluttonRelic : null),
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
        RelicModel relic = RelicFactory.PullNextRelicFromFront(Owner!).ToMutable();
        await RelicCmd.Obtain(relic, Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.BUY_FOOD.description"));
    }

}
