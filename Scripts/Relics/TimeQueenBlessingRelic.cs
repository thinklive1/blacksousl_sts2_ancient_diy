using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Time Queen Blessing relic.</summary>
[RegisterRelic(typeof(EventRelicPool))]
public class TimeQueenBlessingRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<ReplayEnchantment>()
            .Append(HoverTipFactory.FromKeyword(CardKeyword.Retain));

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://bs_ancient/assets/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://bs_ancient/assets/images/relics/{GetType().Name}.png",
        BigIconPath: $"res://bs_ancient/assets/images/relics/{GetType().Name}.png"
    );

    // Enchant one deck card with Replay when the relic is obtained.
    public override async Task AfterObtained()
    {
        foreach (CardModel item in await CardSelectCmd.FromDeckForEnchantment(
                     prefs: new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, DynamicVars.Cards.IntValue),
                     player: Owner,
                     enchantment: ModelDb.Enchantment<ReplayEnchantment>(),
                     amount: 1))
        {
            CardCmd.Enchant<ReplayEnchantment>(item, 1m);
            NCardEnchantVfx? nCardEnchantVfx = NCardEnchantVfx.Create(item);
            if (nCardEnchantVfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(nCardEnchantVfx);
            }
        }
    }
}
