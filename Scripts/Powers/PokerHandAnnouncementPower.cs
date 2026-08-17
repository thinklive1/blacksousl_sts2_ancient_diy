using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override bool IsVisibleInternal => false;

    /// <summary>Displays the resolved hand every time it is played.</summary>
    internal static void ShowHand(PlayingCardPokerHandRank rank)
    {
        RitsuToastService.ShowInfo($"打出：{GetHandName((int)rank)}");
    }

    private static string GetHandName(int rank) => rank switch
    {
        (int)PlayingCardPokerHandRank.Pair => "对子",
        (int)PlayingCardPokerHandRank.TwoPair => "两对",
        (int)PlayingCardPokerHandRank.ThreeOfAKind => "三条",
        (int)PlayingCardPokerHandRank.Straight => "顺子",
        (int)PlayingCardPokerHandRank.Flush => "同花",
        (int)PlayingCardPokerHandRank.FullHouse => "葫芦",
        (int)PlayingCardPokerHandRank.FourOfAKind => "四条",
        (int)PlayingCardPokerHandRank.StraightFlush => "同花顺",
        (int)PlayingCardPokerHandRank.RoyalFlush => "皇家同花顺",
        _ => "牌型"
    };
}
