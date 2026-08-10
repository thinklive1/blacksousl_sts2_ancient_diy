using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts;

/// <summary>Scopes source-less command values to the card currently resolving.</summary>
internal static class ButchersMathClassExecutionContext
{
    private static readonly AsyncLocal<ContextState?> CurrentState = new();

    public static ResolutionScope BeginResolution(CardModel card)
    {
        if (card.Owner?.GetRelic<ButchersMathClassRelic>()?.Affects(card) != true)
        {
            return ResolutionScope.Empty;
        }

        ContextState state = CurrentState.Value ??= new ContextState();
        lock (state.SyncRoot)
        {
            long scopeId = ++state.NextScopeId;
            state.Resolutions.Add(new ResolutionFrame(scopeId, card));
            return new ResolutionScope(state, scopeId);
        }
    }

    public static void Push(CardModel card)
    {
        ContextState? state = CurrentState.Value;
        if (state == null)
        {
            return;
        }

        lock (state.SyncRoot)
        {
            ResolutionFrame? resolution = state.Resolutions.LastOrDefault(frame => frame.Card == card);
            if (resolution != null)
            {
                state.ActiveCards.Add(resolution);
            }
        }
    }

    public static void Pop(CardModel card)
    {
        ContextState? state = CurrentState.Value;
        if (state == null)
        {
            return;
        }

        lock (state.SyncRoot)
        {
            int index = state.ActiveCards.FindLastIndex(frame => frame.Card == card);
            if (index >= 0)
            {
                state.ActiveCards.RemoveAt(index);
            }
        }
    }

    public static void Scale(Player player, ref decimal amount)
    {
        CardModel? card = GetActiveCard();
        ButchersMathClassRelic? relic = card?.Owner == player ? player.GetRelic<ButchersMathClassRelic>() : null;
        if (relic?.Affects(card!) == true)
        {
            amount = relic.ScaleDirectAmount(card!, amount);
        }
    }

    public static int ScaleCount(Player player, int count)
    {
        decimal amount = count;
        Scale(player, ref amount);
        return Math.Max(0, (int)amount);
    }

    public static bool IsActiveFor(Player player)
    {
        CardModel? card = GetActiveCard();
        return card?.Owner == player && card.Owner.GetRelic<ButchersMathClassRelic>()?.Affects(card) == true;
    }

    private static CardModel? GetActiveCard()
    {
        ContextState? state = CurrentState.Value;
        if (state == null)
        {
            return null;
        }

        lock (state.SyncRoot)
        {
            return state.ActiveCards.LastOrDefault()?.Card;
        }
    }

    private sealed class ContextState
    {
        public object SyncRoot { get; } = new();

        public List<ResolutionFrame> Resolutions { get; } = [];

        public List<ResolutionFrame> ActiveCards { get; } = [];

        public long NextScopeId { get; set; }
    }

    private sealed record ResolutionFrame(long ScopeId, CardModel Card);

    internal sealed class ResolutionScope : IDisposable
    {
        internal static ResolutionScope Empty { get; } = new(null, 0);

        private ContextState? _state;
        private readonly long _scopeId;

        internal ResolutionScope(object? state, long scopeId)
        {
            _state = state as ContextState;
            _scopeId = scopeId;
        }

        public Task Wrap(Task task)
        {
            return _state == null ? task : AsyncTaskCleanup.Run(task, Dispose);
        }

        public void Dispose()
        {
            ContextState? state = Interlocked.Exchange(ref _state, null);
            if (state == null)
            {
                return;
            }

            lock (state.SyncRoot)
            {
                state.ActiveCards.RemoveAll(frame => frame.ScopeId == _scopeId);
                state.Resolutions.RemoveAll(frame => frame.ScopeId == _scopeId);
                if (state.Resolutions.Count == 0 && ReferenceEquals(CurrentState.Value, state))
                {
                    CurrentState.Value = null;
                }
            }
        }

    }
}

internal static class AsyncTaskCleanup
{
    public static async Task Run(Task task, Action cleanup)
    {
        try
        {
            await task;
        }
        finally
        {
            cleanup();
        }
    }
}

/// <summary>Guarantees that direct-value scaling is cleared when card resolution exits or fails.</summary>
[HarmonyPatch(
    typeof(CardModel),
    nameof(CardModel.OnPlayWrapper),
    [typeof(PlayerChoiceContext), typeof(Creature), typeof(bool), typeof(ResourceInfo), typeof(bool)])]
internal static class ButchersMathClassResolutionScopePatch
{
    [HarmonyPrefix]
    private static void Begin(CardModel __instance, out ButchersMathClassExecutionContext.ResolutionScope __state)
    {
        __state = ButchersMathClassExecutionContext.BeginResolution(__instance);
    }

    [HarmonyPostfix]
    private static void Complete(ref Task __result, ButchersMathClassExecutionContext.ResolutionScope __state)
    {
        __result = __state.Wrap(__result);
    }

    [HarmonyFinalizer]
    private static Exception? CleanUpSynchronousFailure(
        Exception? __exception,
        ButchersMathClassExecutionContext.ResolutionScope __state)
    {
        if (__exception != null)
        {
            __state.Dispose();
        }

        return __exception;
    }
}

/// <summary>Applies the active card's prediction multiplier to source-less resource commands.</summary>
[HarmonyPatch]
internal static class ButchersMathClassDirectValuePatch
{
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal))]
    [HarmonyPrefix]
    private static void ScaleHeal(Creature creature, ref decimal amount)
    {
        if (creature.Player is { } player)
        {
            ButchersMathClassExecutionContext.Scale(player, ref amount);
        }
    }

    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.GainMaxHp))]
    [HarmonyPrefix]
    private static void ScaleMaxHpGain(Creature creature, ref decimal amount)
    {
        if (creature.Player is { } player)
        {
            ButchersMathClassExecutionContext.Scale(player, ref amount);
        }
    }

    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.LoseMaxHp))]
    [HarmonyPrefix]
    private static void ScaleMaxHpLoss(PlayerChoiceContext choiceContext, Creature creature, ref decimal amount)
    {
        if (creature.Player is { } player)
        {
            ButchersMathClassExecutionContext.Scale(player, ref amount);
        }
    }

    [HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.GainEnergy))]
    [HarmonyPrefix]
    private static void ScaleEnergyGain(ref decimal amount, Player player)
    {
        ButchersMathClassExecutionContext.Scale(player, ref amount);
    }

    [HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.LoseEnergy))]
    [HarmonyPrefix]
    private static void ScaleEnergyLoss(ref decimal amount, Player player)
    {
        ButchersMathClassExecutionContext.Scale(player, ref amount);
    }

    [HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.GainGold))]
    [HarmonyPrefix]
    private static void ScaleGoldGain(ref decimal amount, Player player)
    {
        ButchersMathClassExecutionContext.Scale(player, ref amount);
    }

    [HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.LoseGold))]
    [HarmonyPrefix]
    private static void ScaleGoldLoss(ref decimal amount, Player player)
    {
        ButchersMathClassExecutionContext.Scale(player, ref amount);
    }

    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Draw), new Type[] { typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool) })]
    [HarmonyPrefix]
    private static void ScaleDraw(PlayerChoiceContext choiceContext, ref decimal count, Player player)
    {
        ButchersMathClassExecutionContext.Scale(player, ref count);
    }
}

/// <summary>Scales discard selections made by the active card.</summary>
[HarmonyPatch]
internal static class ButchersMathClassCardSelectionPatch
{
    [HarmonyPatch(typeof(CardSelectCmd), nameof(CardSelectCmd.FromHandForDiscard))]
    [HarmonyPrefix]
    private static void ScaleDiscardSelection(Player player, ref CardSelectorPrefs prefs)
    {
        ScaleSelection(player, ref prefs);
    }

    internal static void ScaleSelection(Player player, ref CardSelectorPrefs prefs)
    {
        if (!ButchersMathClassExecutionContext.IsActiveFor(player))
        {
            return;
        }

        int min = ButchersMathClassExecutionContext.ScaleCount(player, prefs.MinSelect);
        int max = Math.Max(min, ButchersMathClassExecutionContext.ScaleCount(player, prefs.MaxSelect));
        prefs = new CardSelectorPrefs(prefs.Prompt, min, max)
        {
            Cancelable = prefs.Cancelable,
            Comparison = prefs.Comparison,
            PretendCardsCanBePlayed = prefs.PretendCardsCanBePlayed,
            RequireManualConfirmation = prefs.RequireManualConfirmation,
            ShouldGlowGold = prefs.ShouldGlowGold,
            UnpoweredPreviews = prefs.UnpoweredPreviews
        };
    }
}

/// <summary>Scales only combat-pile selections that explicitly use the Exhaust prompt.</summary>
[HarmonyPatch]
internal static class ButchersMathClassExhaustSelectionPatch
{
    private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        return AccessTools.GetDeclaredMethods(typeof(CardSelectCmd))
            .Where(method => method.Name == nameof(CardSelectCmd.FromCombatPile)
                && method.GetParameters().Any(parameter => parameter.ParameterType == typeof(CardSelectorPrefs)));
    }

    [HarmonyPrefix]
    private static void ScaleExhaustSelection(Player player, ref CardSelectorPrefs prefs)
    {
        if (prefs.Prompt.LocEntryKey == CardSelectorPrefs.ExhaustSelectionPrompt.LocEntryKey)
        {
            ButchersMathClassCardSelectionPatch.ScaleSelection(player, ref prefs);
        }
    }
}

/// <summary>Scales automatic discards and batches of cards generated by the active card.</summary>
[HarmonyPatch]
internal static class ButchersMathClassPileQuantityPatch
{
    [HarmonyPatch(typeof(CardCmd), nameof(CardCmd.DiscardAndDraw))]
    [HarmonyPrefix]
    private static void ScaleAutomaticDiscard(ref IEnumerable<CardModel> cardsToDiscard)
    {
        List<CardModel> cards = cardsToDiscard.ToList();
        if (cards.Count == 0 || !ButchersMathClassExecutionContext.IsActiveFor(cards[0].Owner))
        {
            return;
        }

        Player player = cards[0].Owner;
        int targetCount = ButchersMathClassExecutionContext.ScaleCount(player, cards.Count);
        if (targetCount > cards.Count)
        {
            cards.AddRange(PileType.Hand.GetPile(player).Cards.Where(card => !cards.Contains(card)).Take(targetCount - cards.Count));
        }

        cardsToDiscard = cards.Take(targetCount);
    }

    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardsToCombat))]
    [HarmonyPrefix]
    private static void ScaleGeneratedCards(ref IEnumerable<CardModel> cards, Player? creator)
    {
        if (creator is null || !ButchersMathClassExecutionContext.IsActiveFor(creator))
        {
            return;
        }

        List<CardModel> generatedCards = cards.ToList();
        int targetCount = ButchersMathClassExecutionContext.ScaleCount(creator, generatedCards.Count);
        for (int index = generatedCards.Count; index < targetCount; index++)
        {
            generatedCards.Add(generatedCards[index % generatedCards.Count].CreateClone());
        }

        cards = generatedCards.Take(targetCount);
    }
}
