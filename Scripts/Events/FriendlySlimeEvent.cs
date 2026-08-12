using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Friendly Slime event.</summary>
[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Underdocks))]
[RegisterActEvent(typeof(Hive))]
public sealed class FriendlySlimeEvent : ModEventTemplate
{
    private const int NodCards = 1;
    private const int HandshakeCards = 2;
    private const int HugCards = 3;
    private const int HandshakeDamage = 7;
    private const int HugDamage = 15;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://bs_ancient/assets/images/events/FriendlySlime.jpg"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HandshakeDamage", HandshakeDamage),
        new DynamicVar("HugDamage", HugDamage),
    ];

    public override bool IsAllowed(IRunState runState)
    {
        return BsAncientConfig.EnableModEvents
            && runState.Players.All(player => CountDissolveCandidates(player) >= NodCards);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        int candidates = CountDissolveCandidates(Owner!);
        bool canNod = candidates >= NodCards;
        bool canHandshake = candidates >= HandshakeCards && Owner!.Creature.CurrentHp > HandshakeDamage;
        bool canHug = candidates >= HugCards && Owner!.Creature.CurrentHp > HugDamage;

        return
        [
            RelicOption<FriendlySlimeNodRelic>(
                canNod ? ObtainNodRelic : null,
                InitialOptionKey(canNod ? "NOD" : "NOD_LOCKED")),
            RelicOption<FriendlySlimeHandshakeRelic>(
                canHandshake ? ObtainHandshakeRelic : null,
                InitialOptionKey(canHandshake ? "HANDSHAKE" : "HANDSHAKE_LOCKED")),
            RelicOption<FriendlySlimeHugRelic>(
                canHug ? ObtainHugRelic : null,
                InitialOptionKey(canHug ? "HUG" : "HUG_LOCKED")),
        ];
    }

    private async Task ObtainNodRelic()
    {
        await RelicCmd.Obtain<FriendlySlimeNodRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.NOD.description"));
    }

    private async Task ObtainHandshakeRelic()
    {
        await RelicCmd.Obtain<FriendlySlimeHandshakeRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.HANDSHAKE.description"));
    }

    private async Task ObtainHugRelic()
    {
        await RelicCmd.Obtain<FriendlySlimeHugRelic>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.HUG.description"));
    }

    private static int CountDissolveCandidates(Player player)
    {
        return PileType.Deck.GetPile(player).Cards.Count(IsDissolveCandidate);
    }

    public static bool IsDissolveCandidate(CardModel? card)
    {
        return card != null && ModelDb.Enchantment<DissolveEnchantment>().CanEnchant(card);
    }
}
