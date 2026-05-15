using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(SharedRelicPool))]
public class WormSmokeRelic : ModRelicTemplate
{
    private const int MaxHpLoss = 3;
    private const int UpgradeCount = 3;

    private bool _wasUsed;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool IsUsedUp => WasUsed;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new MaxHpVar(MaxHpLoss),
        new CardsVar(UpgradeCount)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/WormSmokeRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/WormSmokeRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/WormSmokeRelic.png"
    );

    [SavedProperty]
    public bool WasUsed
    {
        get => _wasUsed;
        set
        {
            AssertMutable();
            _wasUsed = value;
            if (IsUsedUp)
            {
                Status = RelicStatus.Disabled;
            }
        }
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner || WasUsed)
        {
            return false;
        }

        options.Add(new WormSmokeRestSiteOption(player));
        return true;
    }

    public async Task InhaleSmoke()
    {
        Flash();
        WasUsed = true;
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars.MaxHp.BaseValue, isFromCard: false);

        IEnumerable<CardModel> cards = PileType.Deck.GetPile(Owner).Cards
            .Where(card => card?.IsUpgradable ?? false)
            .ToList()
            .StableShuffle(Owner.RunState.Rng.Niche)
            .Take(DynamicVars.Cards.IntValue);

        foreach (CardModel card in cards)
        {
            CardCmd.Upgrade(card, CardPreviewStyle.MessyLayout);
        }
    }
}

public class WormSmokeRestSiteOption : RestSiteOption
{
    public override string OptionId => "TOKE";

    public override LocString Description
    {
        get
        {
            WormSmokeRelic relic = Owner.GetRelic<WormSmokeRelic>()!;
            LocString description = base.Description;
            description.Add("MaxHp", relic.DynamicVars.MaxHp.BaseValue);
            description.Add("Cards", relic.DynamicVars.Cards.IntValue);
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
        return true;
    }
}
