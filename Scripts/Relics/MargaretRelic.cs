using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Margaret relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public class MargaretRelic : ModRelicTemplate
{
    private const int RequiredSupplies = 3;
    private const int LowGold = 200;
    private const int MidGold = 300;
    private const int HighGold = 500;
    private const int LowScoreThreshold = 12;
    private const int HighScoreThreshold = 18;

    private int _supplyCount;
    private int _supplyScore;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount => IsUsedUp ? 0 : RequiredSupplies - BlackSouls_SupplyCount;

    public override bool IsUsedUp => IsMutable
        && (Owner?.GetRelic<Driftwood>() != null || BlackSouls_SupplyCount >= RequiredSupplies);

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Supplies", RequiredSupplies),
        new GoldVar(LowGold),
        new DynamicVar("MidGold", MidGold),
        new DynamicVar("HighGold", HighGold)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/MargaretRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/MargaretRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/MargaretRelic.png"
    );

    [SavedProperty]
    public int BlackSouls_SupplyCount
    {
        get => _supplyCount;
        set
        {
            AssertMutable();
            _supplyCount = value;
            InvokeDisplayAmountChanged();
            UpdateStatus();
        }
    }

    [SavedProperty]
    public int BlackSouls_SupplyScore
    {
        get => _supplyScore;
        set
        {
            AssertMutable();
            _supplyScore = value;
        }
    }

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count == 1;
    }

    public override bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
    {
        if (player != Owner || IsUsedUp)
        {
            UpdateStatus();
            return false;
        }

        if (alternatives.Count >= 2)
        {
            return false;
        }

        alternatives.Add(new CardRewardAlternative("SUPPLY", () => Supply(cardReward), PostAlternateCardRewardAction.EndSelectionAndCompleteReward));
        return true;
    }

    private async Task Supply(CardReward cardReward)
    {
        if (IsUsedUp)
        {
            UpdateStatus();
            return;
        }

        Flash();
        BlackSouls_SupplyScore += cardReward.Cards.Sum(GetCardScore);
        BlackSouls_SupplyCount++;

        if (BlackSouls_SupplyCount >= RequiredSupplies)
        {
            await GrantSupplyRewards();
            UpdateStatus();
        }
    }

    private async Task GrantSupplyRewards()
    {
        int score = BlackSouls_SupplyScore;
        if (score <= LowScoreThreshold)
        {
            await PlayerCmd.GainGold(LowGold, Owner);
            await RewardsCmd.OfferCustom(Owner, [CreateCardReward(CardRarity.Uncommon)]);
            return;
        }

        if (score <= HighScoreThreshold)
        {
            await PlayerCmd.GainGold(MidGold, Owner);
            await RewardsCmd.OfferCustom(Owner, [CreateCardReward(CardRarity.Rare)]);
            return;
        }

        await PlayerCmd.GainGold(HighGold, Owner);
        await RewardsCmd.OfferCustom(Owner, [CreateCardReward(CardRarity.Rare), CreateCardReward(CardRarity.Rare)]);
    }

    private CardReward CreateCardReward(CardRarity rarity)
    {
        CardCreationOptions options = new(
            [Owner.Character.CardPool],
            CardCreationSource.Other,
            CardRarityOddsType.Uniform,
            card => card.Rarity == rarity);
        options = options.WithFlags(CardCreationFlags.NoUpgradeRoll | CardCreationFlags.NoRarityModification);
        return new CardReward(options, 3, Owner);
    }

    private static int GetCardScore(CardModel card)
    {
        return card.Rarity switch
        {
            CardRarity.Rare => 3,
            CardRarity.Uncommon => 2,
            _ => 1
        };
    }

    private void UpdateStatus()
    {
        Status = IsUsedUp ? RelicStatus.Disabled : RelicStatus.Normal;
    }
}
