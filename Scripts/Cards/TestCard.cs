using Blacksouls.Scripts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts.Cards;

// 注册卡牌到指定池（这里是无色）。如果要写自定义池看添加人物的开头
[RegisterCard(typeof(ColorlessCardPool))]
// 注册成人物起始卡，后面是数量。不需要删除即可。
// [RegisterCharacterStarterCard(typeof(TestCharacter), 5)]
public class TestCard : ModCardTemplate
{
    // 基础耗能
    private const int energyCost = 1;
    // 卡牌类型
    private const CardType type = CardType.Attack;
    // 卡牌稀有度
    private const CardRarity rarity = CardRarity.Common;
    // 目标类型（AnyEnemy表示任意敌人）
    private const TargetType targetType = TargetType.AnyEnemy;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<string> RegisteredKeywordIds => [MyKeywords.Replay]; // 这里添加关键词ID，和上面定义的保持一致

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://bs_ancient/assets/images/cards/{GetType().Name}.png"
    // 卡框等，有需求自己添加。需要自行判断卡牌类型（攻击、技能、能力等）设置，建议写在基类里。
    // 如果使用自定义卡池，需要改下material（TODO）
    // FramePath: "", // 卡牌背景
    // PortraitBorderPath: "", // 边框（状态牌感染使用的）
    // BannerTexturePath: "" // 横幅（不同类型）
    );

    // 卡牌基础数值
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(12, ValueProp.Move)
    ];

    public TestCard() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .Execute(choiceContext);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }

    // 通过HoverTipFactory添加各种提示文本
    //protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        /*
        HoverTipFactory.FromCard<Shiv>(),
        HoverTipFactory.FromPower<TestPower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        */
        //ModKeywordRegistry.CreateHoverTip(MyKeywords.Replay), // 自定义关键词
    //];


}