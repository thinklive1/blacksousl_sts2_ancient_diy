using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class CovenantOfPrickettRelic : ModRelicTemplate
{
    private const int GoldBonusPercent = 50;
    private const int CombatInterval = 3;
    private const int CardRewardOptions = 3;

    private int _combatsSeen;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount
    {
        get
        {
            int progress = CombatsSeen % CombatInterval;
            return progress == 0 ? CombatInterval : CombatInterval - progress;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("GoldBonusPercent", GoldBonusPercent),
        new DynamicVar("Combats", CombatInterval),
        new CardsVar(CardRewardOptions)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/CovenantOfPrickettRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/CovenantOfPrickettRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/CovenantOfPrickettRelic.png"
    );

    [SavedProperty]
    public int CombatsSeen
    {
        get => _combatsSeen;
        set
        {
            AssertMutable();
            _combatsSeen = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        CombatsSeen++;
        return Task.CompletedTask;
    }

    public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner)
        {
            return false;
        }

        bool modified = AddGoldBonusRewards(player, rewards);

        if (ShouldAddRareCardReward(room))
        {
            rewards.Add(new CardReward(CreateRareCardOptions(player), CardRewardOptions, player));
            modified = true;
        }

        return modified;
    }

    public override Task AfterModifyingRewards()
    {
        Flash();
        return Task.CompletedTask;
    }

    private static bool AddGoldBonusRewards(Player player, List<Reward> rewards)
    {
        bool modified = false;
        foreach (GoldReward goldReward in rewards.OfType<GoldReward>().ToList())
        {
            if (goldReward.Amount <= 0)
            {
                continue;
            }

            int bonusGold = (int)Math.Ceiling(goldReward.Amount * GoldBonusPercent / 100m);
            if (bonusGold <= 0)
            {
                continue;
            }

            rewards.Add(new GoldReward(bonusGold, player));
            modified = true;
        }

        return modified;
    }

    private bool ShouldAddRareCardReward(AbstractRoom? room)
    {
        return room != null
            && room.RoomType.IsCombatRoom()
            && CombatsSeen > 0
            && CombatsSeen % DynamicVars["Combats"].IntValue == 0;
    }

    private static CardCreationOptions CreateRareCardOptions(Player player)
    {
        return CardCreationOptions
            .ForNonCombatWithUniformOdds([player.Character.CardPool], card => card.Rarity == CardRarity.Rare)
            .WithFlags(CardCreationFlags.NoRarityModification);
    }
}
