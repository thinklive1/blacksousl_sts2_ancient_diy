using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class QuillPenRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.All(player => !HasPowerOfRewrite(player));
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<PowerOfRewrite>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/QuillPenRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/QuillPenRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/QuillPenRelic.png"
    );

    public override async Task AfterObtained()
    {
        if (HasPowerOfRewrite(Owner))
        {
            return;
        }

        CardModel card = Owner.RunState.CreateCard<PowerOfRewrite>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck, MegaCrit.Sts2.Core.Entities.Cards.CardPilePosition.Top, this, false), 2f);
    }

    public static bool CanBeOffered(Player player)
    {
        return !HasPowerOfRewrite(player);
    }

    private static bool HasPowerOfRewrite(Player player)
    {
        return PileType.Deck.GetPile(player).Cards.Any(card => card is PowerOfRewrite);
    }
}
