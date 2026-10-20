using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Ui.Toast;

namespace BlackSouls.Scripts;

/// <summary>Silently carries a resolved poker-hand name to the combat toast layer.</summary>
[RegisterPower]
public sealed class PokerHandAnnouncementPower : ModPowerTemplate
{
    private const string LocalizationPrefix = "BS_ANCIENT_POWER_POKER_HAND_ANNOUNCEMENT_POWER";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override bool IsVisibleInternal => false;

    /// <summary>Displays the resolved hand every time it is played.</summary>
    internal static void ShowHand(PlayingCardPokerHandRank rank)
    {
        string handName = new LocString("powers", $"{LocalizationPrefix}.hands.{GetHandKey(rank)}")
            .GetFormattedText();
        LocString message = new("powers", $"{LocalizationPrefix}.toast");
        message.Add("Hand", handName);
        RitsuToastService.ShowInfo(message.GetFormattedText());
    }

    private static string GetHandKey(PlayingCardPokerHandRank rank) => rank switch
    {
        PlayingCardPokerHandRank.Pair => nameof(PlayingCardPokerHandRank.Pair),
        PlayingCardPokerHandRank.TwoPair => nameof(PlayingCardPokerHandRank.TwoPair),
        PlayingCardPokerHandRank.ThreeOfAKind => nameof(PlayingCardPokerHandRank.ThreeOfAKind),
        PlayingCardPokerHandRank.Straight => nameof(PlayingCardPokerHandRank.Straight),
        PlayingCardPokerHandRank.Flush => nameof(PlayingCardPokerHandRank.Flush),
        PlayingCardPokerHandRank.FullHouse => nameof(PlayingCardPokerHandRank.FullHouse),
        PlayingCardPokerHandRank.FourOfAKind => nameof(PlayingCardPokerHandRank.FourOfAKind),
        PlayingCardPokerHandRank.StraightFlush => nameof(PlayingCardPokerHandRank.StraightFlush),
        PlayingCardPokerHandRank.RoyalFlush => nameof(PlayingCardPokerHandRank.RoyalFlush),
        _ => "Generic"
    };
}
