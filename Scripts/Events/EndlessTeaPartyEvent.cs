using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Endless Tea Party event.</summary>
[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Underdocks))]
[RegisterActEvent(typeof(Hive))]
[RegisterActEvent(typeof(Glory))]
public sealed class EndlessTeaPartyEvent : ModEventTemplate
{
    private const int HatGoldCost = 106;
    private const int RestHeal = 15;
    internal const string PortraitPath = "res://bs_ancient/assets/images/events/EndlessTeaPartyEvent.jpg";
    private const string DefaultPortraitPath = "res://images/events/endless_tea_party_event.png";

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: PortraitPath
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Gold", HatGoldCost),
        new HealVar(RestHeal)
    ];

    public override bool IsAllowed(IRunState runState)
    {
        return BsAncientConfig.EnableModEvents
            && runState.CurrentActIndex is >= 0 and <= 2;
    }

    public override IEnumerable<string> GetAssetPaths(IRunState runState)
    {
        return base.GetAssetPaths(runState)
            .Where(path => path != DefaultPortraitPath)
            .Append(PortraitPath)
            .Distinct();
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(this, AskTea, InitialOptionKey("ANSWER")),
        new EventOption(this, AskTea, InitialOptionKey("ASK")),
        new EventOption(this, AskTea, InitialOptionKey("QUESTION")),
    ];

    private Task AskTea()
    {
        bool hasCopyableRelic = HasCopyableRelic(Owner!);
        bool canBuyHat = Owner!.Gold >= HatGoldCost && hasCopyableRelic;
        string hatOptionKey = canBuyHat
            ? "HAT"
            : Owner.Gold < HatGoldCost
                ? "HAT_LOCKED"
                : "HAT_NO_TARGET_LOCKED";

        SetEventState(L10NLookup($"{Id.Entry}.pages.TEA.description"),
        [
            new EventOption(
                this,
                DrinkTea,
                $"{Id.Entry}.pages.TEA.options.DRINK",
                HoverTipFactory.FromRelic<MercuryRelic>()),
            new EventOption(
                this,
                canBuyHat ? BuyHat : null,
                $"{Id.Entry}.pages.TEA.options.{hatOptionKey}",
                HoverTipFactory.FromRelic<SuspiciousHatRelic>()),
            new EventOption(this, Rest, $"{Id.Entry}.pages.TEA.options.REST")
        ]);
        return Task.CompletedTask;
    }

    private async Task DrinkTea()
    {
        await RelicCmd.Obtain<MercuryRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.DRINK.description"));
    }

    private async Task BuyHat()
    {
        await PlayerCmd.LoseGold(HatGoldCost, Owner!, GoldLossType.Spent);
        await RelicCmd.Obtain<SuspiciousHatRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.HAT.description"));
    }

    private async Task Rest()
    {
        await CreatureCmd.Heal(Owner!.Creature, RestHeal);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.REST.description"));
    }

    private static bool HasCopyableRelic(Player player)
    {
        return player.Relics.Any(IsCopyableRelic);
    }

    private static bool IsCopyableRelic(RelicModel relic)
    {
        return relic is not SuspiciousHatRelic
            && relic.Rarity != RelicRarity.Ancient
            && relic.GetType().Namespace == "MegaCrit.Sts2.Core.Models.Relics";
    }
}
