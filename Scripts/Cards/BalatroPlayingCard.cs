using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

/// <summary>Combat-only blank card used by the Balatro training encounter.</summary>
[RegisterCard(typeof(EventCardPool))]
public sealed class BalatroPlayingCard : ModCardTemplate
{
    private const string CardPortraitRoot = "res://bs_ancient/assets/images/cards/balatro";

    public BalatroPlayingCard() : base(0, CardType.Skill, CardRarity.Event, TargetType.None) { }
    public override int MaxUpgradeLevel => 0;
    public override CardAssetProfile AssetProfile => new(PortraitPath: GetPortraitPath("Heart", 1));

    /// <summary>Uses the card's suit enchantment to select its compact 250x190 face.</summary>
    public override string PortraitPath
    {
        get
        {
            return Enchantment is PlayingCardSuitEnchantment suitEnchantment
                ? GetPortraitPath(suitEnchantment.PokerSuit.ToString(), suitEnchantment.Amount)
                : GetPortraitPath("Heart", 1);
        }
    }

    /// <summary>Feeds the same instance-specific path through RitsuLib's asset override API.</summary>
    public override string CustomPortraitPath => PortraitPath;

    /// <summary>Preloads every generated face used by the temporary 52-card deck.</summary>
    public override IEnumerable<string> AllPortraitPaths => Enum.GetNames<PlayingCardSuit>()
        .SelectMany(suit => Enumerable.Range(1, PlayingCardSuitEnchantment.MaxTriggersPerCombat)
            .Select(rank => GetPortraitPath(suit, rank)));

    private static string GetPortraitPath(string suit, int rank) =>
        $"{CardPortraitRoot}/{suit}_{Math.Clamp(rank, 1, PlayingCardSuitEnchantment.MaxTriggersPerCombat):00}.png";
}
