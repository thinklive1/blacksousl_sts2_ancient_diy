using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Runs a third-act Battleworn Dummy variant using poker scoring.</summary>
[RegisterActEvent(typeof(Glory))]
public sealed class BalatroTrainingDummyEvent : ModEventTemplate
{
    private const string PortraitPath = "res://bs_ancient/assets/images/events/BalatroTrainingDummyEvent.jpg";

    public override bool IsShared => true;
    public override EventAssetProfile AssetProfile => new(InitialPortraitPath: PortraitPath);

    public override bool IsAllowed(IRunState runState) =>
        BsAncientConfig.EnableModEvents && runState.CurrentActIndex == 2 && runState.Players.Count == 1;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(this, () => StartChallenge(1, 300), InitialOptionKey("SETTING_1")),
        new EventOption(this, () => StartChallenge(2, 600), InitialOptionKey("SETTING_2")),
        new EventOption(this, () => StartChallenge(3, 900), InitialOptionKey("SETTING_3")),
    ];

    private Task StartChallenge(int tier, int target)
    {
        BalatroTrainingDummyEncounter encounter = (BalatroTrainingDummyEncounter)ModelDb.Encounter<BalatroTrainingDummyEncounter>().ToMutable();
        encounter.RewardTier = tier;
        encounter.ScoreTarget = target;
        EnterCombatWithoutExitingEvent(encounter, [], shouldResumeAfterCombat: true);
        return Task.CompletedTask;
    }

    public override async Task Resume(AbstractRoom room)
    {
        BalatroTrainingDummyEncounter encounter = (BalatroTrainingDummyEncounter)((CombatRoom)room).Encounter;
        if (encounter.RanOutOfTime)
        {
            SetEventFinished(L10NLookup($"{Id.Entry}.pages.DEFEAT.description"));
            return;
        }

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.VICTORY.description"));
        switch (encounter.RewardTier)
        {
            case 1:
                IEnumerable<PotionModel> potions = Owner!.Character.PotionPool
                    .GetUnlockedPotions(Owner.UnlockState)
                    .Concat(ModelDb.PotionPool<SharedPotionPool>().GetUnlockedPotions(Owner.UnlockState));
                PotionModel? potion = Owner.PlayerRng.Rewards.NextItem(potions);
                if (potion != null)
                {
                    await RewardsCmd.OfferCustom(Owner, [new PotionReward(potion.ToMutable(), Owner)]);
                }
                break;
            case 2:
                foreach (CardModel card in Owner!.Deck.Cards.Where(card => card.IsUpgradable).ToList().StableShuffle(Rng).Take(2))
                {
                    CardCmd.Upgrade(card);
                }
                break;
            case 3:
                await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(Owner!).ToMutable(), Owner!);
                break;
        }
    }
}
