using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Worm Smoke relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public class WormSmokeRelic : ModRelicTemplate
{
    private const int MaxUses = 3;
    private static readonly decimal[] MaxHpCosts = [3m, 5m, 8m];

    private int _useCount;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool IsUsedUp => BlackSouls_UseCount >= MaxUses;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new MaxHpVar(MaxHpCosts[0]),
        new DynamicVar("Uses", MaxUses)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/WormSmokeRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/WormSmokeRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/WormSmokeRelic.png"
    );

    [SavedProperty]
    public int BlackSouls_UseCount
    {
        get => _useCount;
        set
        {
            AssertMutable();
            _useCount = value;
            if (IsUsedUp)
            {
                Status = RelicStatus.Disabled;
            }
        }
    }

    public decimal CurrentMaxHpCost => MaxHpCosts[Math.Clamp(BlackSouls_UseCount, 0, MaxHpCosts.Length - 1)];

    public int UsesRemaining => Math.Max(0, MaxUses - BlackSouls_UseCount);

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count == 1;
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner || IsUsedUp || Owner.Deck.UpgradableCardCount == 0)
        {
            return false;
        }

        options.Add(new WormSmokeRestSiteOption(player));
        return true;
    }

    public async Task<bool> InhaleSmoke()
    {
        if (IsUsedUp)
        {
            return false;
        }

        CardSelectorPrefs prefs = new(CardSelectorPrefs.UpgradeSelectionPrompt, 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        CardModel? selected = (await CardSelectCmd.FromDeckForUpgrade(Owner, prefs)).FirstOrDefault();
        if (selected == null)
        {
            return false;
        }

        Flash();
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, CurrentMaxHpCost, isFromCard: false);
        CardCmd.Upgrade(selected, CardPreviewStyle.None);
        BlackSouls_UseCount++;
        return true;
    }
}

/// <summary>Implements the Worm Smoke rest site option.</summary>
public class WormSmokeRestSiteOption : RestSiteOption
{
    public const string SmokeIconPath = "res://bs_ancient/assets/images/events/Smoke.jpg";

    public override string OptionId => "TOKE";

    public override IEnumerable<string> AssetPaths => [SmokeIconPath];

    public override LocString Description
    {
        get
        {
            WormSmokeRelic relic = Owner.GetRelic<WormSmokeRelic>()!;
            LocString description = base.Description;
            description.Add("MaxHp", relic.CurrentMaxHpCost);
            description.Add("Uses", relic.UsesRemaining);
            return description;
        }
    }

    public WormSmokeRestSiteOption(Player owner)
        : base(owner)
    {
    }

    public override async Task<bool> OnSelect()
    {
        WormSmokeRelic? relic = Owner.GetRelic<WormSmokeRelic>();
        if (relic == null)
        {
            return false;
        }

        await relic.InhaleSmoke();
        return false;
    }
}
