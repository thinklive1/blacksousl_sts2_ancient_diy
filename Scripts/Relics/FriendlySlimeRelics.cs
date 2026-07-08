using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Provides shared behavior for Friendly Slime relic variants.</summary>
public abstract class FriendlySlimeRelicBase : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool HasUponPickupEffect => true;

    protected abstract int CardsToEnchant { get; }

    protected abstract int DamageTaken { get; }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(CardsToEnchant),
        new HpLossVar(DamageTaken),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<DissolveEnchantment>();

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/FriendlySlimeRelic.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/FriendlySlimeRelic.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/FriendlySlimeRelic.png"
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override async Task AfterObtained()
    {
        if (DamageTaken > 0)
        {
            Owner.Creature.LoseHpInternal(DamageTaken, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.SkipHurtAnim);
        }

        foreach (CardModel card in await CardSelectCmd.FromDeckForEnchantment(
            Owner,
            ModelDb.Enchantment<DissolveEnchantment>(),
            1,
            FriendlySlimeEvent.IsDissolveCandidate,
            new CardSelectorPrefs(SelectionScreenPrompt, CardsToEnchant, CardsToEnchant)))
        {
            CardCmd.Enchant<DissolveEnchantment>(card, 1m);
            NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
            if (vfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
            }
        }
    }
}

/// <summary>Implements the Friendly Slime Nod relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class FriendlySlimeNodRelic : FriendlySlimeRelicBase
{
    protected override int CardsToEnchant => 1;

    protected override int DamageTaken => 0;
}

/// <summary>Implements the Friendly Slime Handshake relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class FriendlySlimeHandshakeRelic : FriendlySlimeRelicBase
{
    protected override int CardsToEnchant => 2;

    protected override int DamageTaken => 7;
}

/// <summary>Implements the Friendly Slime Hug relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class FriendlySlimeHugRelic : FriendlySlimeRelicBase
{
    protected override int CardsToEnchant => 3;

    protected override int DamageTaken => 15;
}
