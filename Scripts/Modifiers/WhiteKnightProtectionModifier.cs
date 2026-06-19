using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

public sealed class WhiteKnightProtectionModifier : ModModifierTemplate
{
    private const string ModifierIconPath = "res://bs_ancient/assets/images/relics/KnightChessPieceRelic.png";
    private const int BuffAmount = 3;

    private bool _active;
    private int _actIndex = -1;

    [SavedProperty]
    public bool BlackSouls_Active
    {
        get => _active;
        set
        {
            AssertMutable();
            _active = value;
        }
    }

    [SavedProperty]
    public int BlackSouls_ActIndex
    {
        get => _actIndex;
        set
        {
            AssertMutable();
            _actIndex = value;
        }
    }

    public override ModifierAssetProfile AssetProfile => new(ModifierIconPath);

    public void Configure(IRunState runState)
    {
        AssertMutable();
        BlackSouls_Active = true;
        BlackSouls_ActIndex = runState.CurrentActIndex;
    }

    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        if (BlackSouls_Active && room is RestSiteRoom)
        {
            BlackSouls_Active = false;
        }

        return Task.CompletedTask;
    }

    public override async Task BeforeCombatStart()
    {
        if (!BlackSouls_Active || RunState.CurrentActIndex != BlackSouls_ActIndex)
        {
            return;
        }

        foreach (MegaCrit.Sts2.Core.Entities.Players.Player player in RunState.Players)
        {
            await PowerCmd.Apply<StrengthPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), player.Creature, BuffAmount, player.Creature, null, false);
            await PowerCmd.Apply<DexterityPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), player.Creature, BuffAmount, player.Creature, null, false);
        }
    }
}
