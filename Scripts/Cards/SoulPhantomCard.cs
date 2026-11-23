using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using Sts2Diy.Scripts.Powers;

namespace Sts2Diy.Scripts.Cards;

/// <summary>Soul plays grant chosen draw-pile cards Ethereal; upgrading also upgrades Souls.</summary>
[RegisterCard(typeof(NecrobinderCardPool))]
public sealed class SoulPhantomCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(PortraitPath: ModelDb.Card<Haunt>().PortraitPath);

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<Soul>(),
        HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
    ];

    public SoulPhantomCard() : base(0, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        SoulPhantomPower? power = await PowerCmd.Apply<SoulPhantomPower>(context, Owner.Creature, 1m,
            Owner.Creature, this);
        power?.Activate(IsUpgraded);
    }

    protected override void OnUpgrade() { }
}
