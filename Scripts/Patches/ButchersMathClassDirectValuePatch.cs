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
    private static readonly AsyncLocal<Stack<CardModel>?> ActiveCards = new();

    public static void Push(CardModel card)
    {
        (ActiveCards.Value ??= new Stack<CardModel>()).Push(card);
    }

    public static void Pop(CardModel card)
    {
        Stack<CardModel>? cards = ActiveCards.Value;
        if (cards?.TryPeek(out CardModel? activeCard) == true && activeCard == card)
        {
            cards.Pop();
        }
    }

    public static void Scale(Player player, ref decimal amount)
    {
        CardModel? card = ActiveCards.Value?.TryPeek(out CardModel? activeCard) == true ? activeCard : null;
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
        return ActiveCards.Value?.TryPeek(out CardModel? card) == true && card.Owner == player && card.Owner.GetRelic<ButchersMathClassRelic>()?.Affects(card) == true;
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
