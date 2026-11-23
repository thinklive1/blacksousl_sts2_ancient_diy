using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Sts2Diy.Scripts.Cards;

/// <summary>Escapes imminent Doom by transferring it to every enemy.</summary>
[RegisterCard(typeof(NecrobinderCardPool))]
public sealed class BorrowedBodyCard : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://sts2diy/assets/images/cards/BorrowedBodyCard.jpg");

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DoomPower>(), HoverTipFactory.FromPower<IntangiblePower>()
    ];
    protected override bool IsPlayable => IsMutable && Owner?.Creature is { IsAlive: true } creature
        && creature.GetPowerAmount<DoomPower>() >= creature.CurrentHp;
    protected override bool ShouldGlowGoldInternal => IsPlayable;

    public BorrowedBodyCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies) { }

    // The native manual-play check uses IsPlayable; this also prevents autoplay from bypassing the condition.
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) => card != this || IsPlayable;

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!IsPlayable) return;
        var creature = Owner.Creature;
        var enemies = creature.CombatState?.HittableEnemies.ToArray() ?? [];
        if (enemies.Length == 0) return;

        int transferred = creature.GetPowerAmount<DoomPower>();
        await PowerCmd.Apply<DoomPower>(context, enemies, transferred, creature, this);

        // Keep any new Doom gained during enemy-application reactions rather than clearing it wholesale.
        if (creature.GetPower<DoomPower>() is { } remaining)
        {
            int removed = Math.Min(transferred, Math.Max(0, remaining.Amount));
            if (removed > 0)
            {
                await PowerCmd.ModifyAmount(context, remaining, -removed, creature, this);
            }
        }

        if (creature.IsAlive)
        {
            await PowerCmd.Apply<IntangiblePower>(context, creature, 1m, creature, this);
        }
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
