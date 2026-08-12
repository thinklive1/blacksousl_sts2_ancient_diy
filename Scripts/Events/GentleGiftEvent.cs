using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Gentle Gift event.</summary>
[RegisterActEvent(typeof(Glory))]
public sealed class GentleGiftEvent : ModEventTemplate
{
    private const string PortraitPath = "res://bs_ancient/assets/images/events/GentleGiftEvent.jpg";
    private const string DefaultPortraitPath = "res://images/events/bs_ancient_event_gentle_gift_event.png";
    private const string MiniSnowmanIconPath = "res://bs_ancient/assets/images/relics/MiniSnowmanRelic.png";
    private const string EvilQiIconPath = "res://bs_ancient/assets/images/powers/EvilQiEnchantment.png";
    private const string EvilQiOverlayPath = "res://bs_ancient/assets/scenes/cards/overlays/afflictions/bs_ancient_affliction_evil_qi_affliction.tscn";
    private const string EvilQiVfxPath = "res://bs_ancient/assets/scenes/vfx/ui/card/afflictions/evil_qi/vfx_ui_card_affliction_evil_qi.tscn";

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: PortraitPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return BsAncientConfig.EnableModEvents
            && runState.CurrentActIndex == 2
            && runState.Players.All(HasEnchantedCard);
    }

    public override IEnumerable<string> GetAssetPaths(IRunState runState)
    {
        return base.GetAssetPaths(runState)
            .Select(path => path == DefaultPortraitPath ? PortraitPath : path)
            .Concat(new[]
            {
                PortraitPath,
                MiniSnowmanIconPath,
                EvilQiIconPath,
                EvilQiOverlayPath,
                EvilQiVfxPath,
            })
            .Distinct();
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        RelicOption<MiniSnowmanRelic>(AcceptGift, InitialOptionKey("ACCEPT")),
        new EventOption(
            this,
            HasEvilQiCandidate(Owner!) ? RefuseGift : null,
            InitialOptionKey(HasEvilQiCandidate(Owner!) ? "REFUSE" : "REFUSE_LOCKED")),
    ];

    private async Task AcceptGift()
    {
        await RelicCmd.Obtain<MiniSnowmanRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.ACCEPT.description"));
    }

    private Task RefuseGift()
    {
        List<CardModel> candidates = PileType.Deck.GetPile(Owner!).Cards
            .Where(CanReceiveEvilQi)
            .ToList();
        CardModel? selected = candidates.Count == 0
            ? null
            : Owner!.RunState.Rng.Niche.NextItem(candidates);
        if (selected != null)
        {
            if (selected.Enchantment != null)
            {
                CardCmd.ClearEnchantment(selected);
            }

            CardCmd.Enchant<EvilQiEnchantment>(selected, 1m);
            NCardEnchantVfx? vfx = NCardEnchantVfx.Create(selected);
            if (vfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
            }
        }

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.REFUSE.description"));
        return Task.CompletedTask;
    }

    private static bool HasEnchantedCard(Player player)
    {
        return PileType.Deck.GetPile(player).Cards.Any(card => card.Enchantment != null);
    }

    private static bool HasEvilQiCandidate(Player player)
    {
        return PileType.Deck.GetPile(player).Cards.Any(CanReceiveEvilQi);
    }

    private static bool CanReceiveEvilQi(CardModel card)
    {
        return card.Type is not (CardType.Status or CardType.Curse or CardType.Quest)
            && !card.Keywords.Contains(CardKeyword.Unplayable)
            && (card.Enchantment != null || ModelDb.Enchantment<EvilQiEnchantment>().CanEnchant(card));
    }
}
