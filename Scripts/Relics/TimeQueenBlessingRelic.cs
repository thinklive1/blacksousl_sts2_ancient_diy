using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

// 注册遗物。如果要写自定义池看添加人物的开头
[RegisterRelic(typeof(SharedRelicPool))]
// [RegisterCharacterStarterRelic(typeof(TestCharacter))] // 注册起始遗物
public class TimeQueenBlessingRelic : ModRelicTemplate
{
    // 稀有度
    public override RelicRarity Rarity => RelicRarity.Ancient;

    // 遗物的数值。这里会替换本地化中的{Cards}。
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    public override RelicAssetProfile AssetProfile => new(
        // 小图标（原版85x85）
        IconPath: $"res://bs_ancient/assets/images/relics/{GetType().Name}.png",
        // 轮廓图标（原版85x85）
        IconOutlinePath: $"res://bs_ancient/assets/images/relics/{GetType().Name}.png",
        // 大图标（原版256x256）
        BigIconPath: $"res://bs_ancient/assets/images/relics/{GetType().Name}.png"
    );

    // 拾取时， 选择牌组的一张牌，为其添加"重演"
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