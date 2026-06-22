using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class TownMusiciansOfBremenRelic : ModRelicTemplate
{
    private const int DazedCards = 5;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private bool _triggeredThisCombat;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Dazed>();

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

        foreach (Creature enemy in Owner.Creature.CombatState.Enemies.Where(enemy => enemy.IsAlive).ToList())
        {
            await CreatureCmd.Stun(enemy);
        }

        int addedToDraw = 0;
        int addedToDiscard = 0;

        for (int i = 0; i < DazedCards; i++)
        {
            CardModel dazed = Owner.Creature.CombatState.CreateCard<Dazed>(Owner);
            PileType pile = Owner.RunState.Rng.CombatCardSelection.NextInt(2) == 0
                ? PileType.Draw
                : PileType.Discard;

            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(dazed, pile, Owner, CardPilePosition.Random);
            if (!result.success)
            {
                continue;
            }

            if (pile == PileType.Draw)
            {
                addedToDraw++;
            }
            else
            {
                addedToDiscard++;
            }
        }

        RefreshPileCounter(PileType.Draw, addedToDraw);
        RefreshPileCounter(PileType.Discard, addedToDiscard);
    }

    private void RefreshPileCounter(PileType pileType, int addedCount)
    {
        if (addedCount <= 0)
        {
            return;
        }

        CardPile pile = pileType.GetPile(Owner);
        pile.InvokeContentsChanged();

        for (int i = 0; i < addedCount; i++)
        {
            pile.InvokeCardAddFinished();
        }
    }
}
