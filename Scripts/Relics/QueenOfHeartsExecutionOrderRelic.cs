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
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Adds Retain and Execution to four Attack cards when obtained.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public sealed class QueenOfHeartsExecutionOrderRelic : ModRelicTemplate
{
    private const int CardsToEnchant = 4;
    private const string RelicIconPath =
        "res://bs_ancient/assets/images/relics/QueenOfHeartsExecutionOrderRelic.png";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(CardsToEnchant)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..HoverTipFactory.FromEnchantment<ExecutionEnchantment>(),
        HoverTipFactory.FromKeyword(CardKeyword.Retain)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath);

    public override bool IsAllowed(IRunState runState)
    {
        // Lorina awards this relic directly; keep it out of random event relic pools.
        return false;
    }

    public override async Task AfterObtained()
    {
        ExecutionEnchantment execution = ModelDb.Enchantment<ExecutionEnchantment>();
        foreach (CardModel card in await CardSelectCmd.FromDeckForEnchantment(
            Owner,
            execution,
            1,
            card => card is { Type: CardType.Attack },
            new CardSelectorPrefs(
                SelectionScreenPrompt,
                CardsToEnchant,
                CardsToEnchant)))
        {
            CardCmd.ApplyKeyword(card, CardKeyword.Retain);
            CardCmd.Enchant<ExecutionEnchantment>(card, 1m);
            NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
            if (vfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
            }
        }
    }
}
