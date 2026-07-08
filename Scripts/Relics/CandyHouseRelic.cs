using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Candy House relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class CandyHouseRelic : ModRelicTemplate
{
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private bool _triggeredThisCombat;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<SweetCandyCard>()
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<BitterCandyCard>())
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<HallucinogenicCandyCard>());

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override Task BeforeCombatStart()
    {
        _triggeredThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (_triggeredThisCombat || player != Owner || Owner.Creature.CombatState == null)
        {
            return;
        }

        _triggeredThisCombat = true;
        Flash();

        await ShuffleCandy<SweetCandyCard>();
        await ShuffleCandy<BitterCandyCard>();
        await ShuffleCandy<HallucinogenicCandyCard>();

        RefreshDrawPileCounter(3);
    }

    private async Task ShuffleCandy<T>() where T : CardModel, new()
    {
        CardModel candy = Owner.Creature.CombatState!.CreateCard<T>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(candy, PileType.Draw, Owner, CardPilePosition.Random);
    }

    private void RefreshDrawPileCounter(int addedCount)
    {
        CardPile drawPile = PileType.Draw.GetPile(Owner);
        drawPile.InvokeContentsChanged();

        for (int i = 0; i < addedCount; i++)
        {
            drawPile.InvokeCardAddFinished();
        }
    }
}
