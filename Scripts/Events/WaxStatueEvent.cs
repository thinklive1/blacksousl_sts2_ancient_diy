using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Hive))]
[RegisterActEvent(typeof(Glory))]
public sealed class WaxStatueEvent : ModEventTemplate
{
    private const int TwinRequiredCount = 2;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://bs_ancient/assets/images/events/TwinInWard.png"
    );

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Any(player => PileType.Deck.GetPile(player).Cards.Count > 0);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        RelicOption<TwinWaxStatueRelic>(HasTwinPair(Owner!) ? ObtainTwinWaxStatue : null),
        RelicOption<LonelyWaxStatueRelic>(ObtainLonelyWaxStatue),
    ];

    private async Task ObtainTwinWaxStatue()
    {
        await RelicCmd.Obtain<TwinWaxStatueRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.DONE.description"));
    }

    private async Task ObtainLonelyWaxStatue()
    {
        await RelicCmd.Obtain<LonelyWaxStatueRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.DONE.description"));
    }

    private static bool HasTwinPair(Player player)
    {
        TwinEnchantment twin = ModelDb.Enchantment<TwinEnchantment>();
        return PileType.Deck.GetPile(player).Cards
            .Where(twin.CanEnchant)
            .GroupBy(card => card.Id)
            .Any(group => group.Count() >= TwinRequiredCount);
    }
}
