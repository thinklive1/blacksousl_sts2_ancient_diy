using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Hive))]
public sealed class LastWhiteKnightEvent : ModEventTemplate
{
    private const int BuffAmount = 3;
    private const decimal HealthThreshold = 0.3m;

    public override bool IsShared => true;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://bs_ancient/assets/images/events/LastWhiteKnightEvent.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<StrengthPower>(BuffAmount),
        new PowerVar<DexterityPower>(BuffAmount)
    ];

    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex is 0 or 1
            && runState.Players.Any(player =>
                player.Creature.CurrentHp > 0
                && player.Creature.CurrentHp <= Math.Floor(player.Creature.MaxHp * HealthThreshold));
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
    [
        new EventOption(this, AcceptProtection, InitialOptionKey("PROTECT"), [
            HoverTipFactory.FromPower<StrengthPower>(),
            HoverTipFactory.FromPower<DexterityPower>()
        ]),
        new EventOption(
            this,
            FightRedKnight,
            InitialOptionKey("FIGHT"),
            HoverTipFactory.FromRelic<KnightChessPieceRelic>()),
        new EventOption(
            this,
            WalkAlone,
            InitialOptionKey("ALONE"),
            AloneHoverTips()),
    ];

    private Task AcceptProtection()
    {
        AddModifier<WhiteKnightProtectionModifier>(Owner!.RunState, modifier => modifier.Configure(Owner!.RunState));
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.PROTECT.description"));
        return Task.CompletedTask;
    }

    private async Task FightRedKnight()
    {
        await RelicCmd.Obtain<KnightChessPieceRelic>(Owner!);
        AddModifier<KnightChessPieceModifier>(Owner!.RunState, modifier => modifier.Configure(Owner!.RunState));

        EncounterModel encounter = Owner!.RunState.Act.PullNextEncounter(RoomType.Elite).ToMutable();
        EnterCombatWithoutExitingEvent(encounter, [], shouldResumeAfterCombat: false);
    }

    private async Task WalkAlone()
    {
        await CardPileCmd.AddCurseToDeck<Injury>(Owner!);
        await RelicCmd.Obtain<AliceHandkerchiefRelic>(Owner!);
        AddModifier<AliceHandkerchiefModifier>(Owner!.RunState, modifier => modifier.Configure(Owner!.RunState));
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.ALONE.description"));
    }

    private static IEnumerable<IHoverTip> AloneHoverTips()
    {
        foreach (IHoverTip hoverTip in HoverTipFactory.FromCardWithCardHoverTips<Injury>())
        {
            yield return hoverTip;
        }

        foreach (IHoverTip hoverTip in HoverTipFactory.FromRelic<AliceHandkerchiefRelic>())
        {
            yield return hoverTip;
        }
    }

    private static void AddModifier<T>(IRunState runState, Action<T> configure) where T : ModifierModel
    {
        if (runState is not RunState mutableRunState)
        {
            return;
        }

        T modifier = (T)ModelDb.Modifier<T>().ToMutable();
        modifier.OnRunLoaded(mutableRunState);
        configure(modifier);
        mutableRunState.AddModifierDebug(modifier);
    }
}
