using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts.Patches;

/// <summary>Applies draw-count modifiers once before the game's normal draw checks.</summary>
public sealed class SharedCardDrawPatch : IPatchMethod
{
    public static string PatchId => "shared_card_draw_count";
    public static string Description => "Apply Math Class scaling and Red Queen's Dance Shoes to draw commands.";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() => [
        new(typeof(CardPileCmd), nameof(CardPileCmd.Draw),
            [typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool)])
    ];

    public static void Prefix(ref decimal count, Player player)
    {
        ButchersMathClassExecutionContext.Scale(player, ref count);
        if (player.Creature.CombatState != null
            && player.GetRelic<RedQueenDanceShoesRelic>() is { } shoes)
        {
            count = shoes.ModifyDrawCount(count);
        }
    }
}
