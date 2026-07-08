using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

/// <summary>Implements the Evil Qi affliction.</summary>
[RegisterAffliction]
public sealed class EvilQiAffliction : ModAfflictionTemplate
{
    public override bool HasExtraCardText => true;

    public override AfflictionAssetProfile AssetProfile => new(
        OverlayScenePath: "res://bs_ancient/assets/scenes/cards/overlays/afflictions/bs_ancient_affliction_evil_qi_affliction.tscn"
    );

    public override Task OnPlay(PlayerChoiceContext choiceContext, Creature? target)
    {
        return EvilQiEffect.Apply(choiceContext, Card);
    }
}
