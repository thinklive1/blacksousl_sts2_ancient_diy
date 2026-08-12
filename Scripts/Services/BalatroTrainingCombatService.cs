using BlackSouls.Scripts.Cards;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Settings;

namespace BlackSouls.Scripts;

/// <summary>Owns the temporary deck and UI used by the poker dummy encounter.</summary>
internal static class BalatroTrainingCombatService
{
    private const string ControlsName = "BsAncientBalatroControls";
    private const string EndTurnTexturePath = "res://images/packed/combat_ui/end_turn_button.png";
    private const string EndTurnGlowTexturePath = "res://images/packed/combat_ui/end_turn_button_glow.png";
    private const int HandSize = 10;
    private const int MaxPlays = 4;
    private const int MaxDiscards = 4;
    private static bool _resolvingChoice;
    private static CombatState? _rulesDialogState;

    public static bool IsActive(ICombatState? state) => state?.Encounter is BalatroTrainingDummyEncounter;

    public static async Task InitializeDeck(ICombatState state, Player player)
    {
        if (state.Encounter is not BalatroTrainingDummyEncounter encounter || encounter.PokerDeckInitialized)
        {
            return;
        }

        encounter.PokerDeckInitialized = true;
        Creature? dummy = state.Enemies.FirstOrDefault(enemy => enemy.IsAlive);
        if (dummy != null && (dummy.MaxHp != encounter.ScoreTarget || dummy.CurrentHp != encounter.ScoreTarget))
        {
            await CreatureCmd.SetMaxAndCurrentHp(dummy, encounter.ScoreTarget);
        }

        List<CardModel> cardsToRemove = player.PlayerCombatState!.AllCards
            .Where(card => card.Pile?.IsCombatPile == true && card.Enchantment is not PlayingCardSuitEnchantment)
            .ToList();
        await CardPileCmd.RemoveFromCombat(cardsToRemove, skipVisuals: true);

        List<CardModel> pokerDeck = [];
        for (int suit = 0; suit < 4; suit++)
        {
            for (int rank = 1; rank <= 13; rank++)
            {
                BalatroPlayingCard card = state.CreateCard<BalatroPlayingCard>(player);
                ApplySuit(card, (PlayingCardSuit)suit, rank);
                pokerDeck.Add(card);
            }
        }

        await CardPileCmd.AddGeneratedCardsToCombat(pokerDeck, PileType.Draw, player, CardPilePosition.Random);
        CardPile drawPile = PileType.Draw.GetPile(player);
        drawPile.InvokeContentsChanged();
        drawPile.InvokeCardAddFinished();
        ThrowingPlayerChoiceContext powerContext = new();
        await PowerCmd.Apply<BalatroTrainingPlayLimitPower>(
            powerContext,
            player.Creature,
            MaxPlays,
            player.Creature,
            null,
            false);
        await PowerCmd.Apply<BalatroTrainingDiscardLimitPower>(
            powerContext,
            player.Creature,
            MaxDiscards,
            player.Creature,
            null,
            false);

    }

    public static decimal ModifyHandDraw(ICombatState state, Player player, decimal original)
    {
        if (!IsActive(state) || player.PlayerCombatState == null)
        {
            return original;
        }

        return Math.Max(0, HandSize - PileType.Hand.GetPile(player).Cards.Count);
    }

    public static void AttachControls(NCombatUi ui, CombatState state)
    {
        if (!IsActive(state) || ui.GetNodeOrNull<Control>(ControlsName) != null)
        {
            return;
        }

        HBoxContainer controls = new()
        {
            Name = ControlsName,
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        controls.AddThemeConstantOverride("separation", 8);
        controls.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        // Sit immediately to the left of the native End Turn button without covering it.
        controls.OffsetLeft = -806f;
        controls.OffsetTop = -238f;
        controls.OffsetRight = -326f;
        controls.OffsetBottom = -142f;

        TextureButton playButton = CreateButton("playButton", "Play Hand");
        TextureButton discardButton = CreateButton("discardButton", "Discard");
        controls.AddChild(playButton);
        controls.AddChild(discardButton);
        ui.AddChild(controls);

        if (_rulesDialogState != state)
        {
            _rulesDialogState = state;
            ModSettingsUiFactory.ShowStyledConfirm(
                ui,
                "扑克训练规则",
                "手牌固定为 10 张。\n\n出牌：最多 4 次。弃牌：本场最多弃 4 张。\n只有打出符合德州扑克规则的牌型才能对木偶造成伤害。\n请选择 1 至 5 张牌组成牌型；单张牌不会造成伤害。\n\n有效牌型：对子、两对、三条、顺子、同花、葫芦、四条、同花顺和皇家同花顺。",
                "",
                "确认",
                false,
                static () => { },
                false,
                null,
                null,
                true,
                false);
        }

        playButton.Pressed += () => ResolveFromButton(ui, state, playButton, discardButton, scoreHand: true);
        discardButton.Pressed += () => ResolveFromButton(ui, state, playButton, discardButton, scoreHand: false);
    }

    private static TextureButton CreateButton(string localizationKey, string tooltip)
    {
        Texture2D? normal = ResourceLoader.Load<Texture2D>(EndTurnTexturePath);
        Texture2D? hover = ResourceLoader.Load<Texture2D>(EndTurnGlowTexturePath);
        TextureButton button = new()
        {
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(235f, 96f),
            IgnoreTextureSize = true,
            StretchMode = TextureButton.StretchModeEnum.Scale,
            TextureNormal = normal,
            TextureHover = hover,
            TexturePressed = normal,
            FocusMode = Control.FocusModeEnum.All,
        };

        Label label = new()
        {
            Text = new LocString(
                "events",
                $"BS_ANCIENT_EVENT_BALATRO_TRAINING_DUMMY_EVENT.{localizationKey}").GetFormattedText(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", 28);
        label.AddThemeColorOverride("font_color", new Color(0.91f, 0.84f, 0.62f));
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.OffsetTop = -2f;
        button.AddChild(label);
        return button;
    }

    private static void ResolveFromButton(
        NCombatUi ui,
        CombatState state,
        TextureButton playButton,
        TextureButton discardButton,
        bool scoreHand)
    {
        Player? player = state.Players.FirstOrDefault();
        if (player?.Creature.GetPower<BalatroTrainingPlayLimitPower>()?.Amount <= 0
            && scoreHand)
        {
            return;
        }

        if (player?.Creature.GetPower<BalatroTrainingDiscardLimitPower>()?.Amount <= 0
            && !scoreHand)
        {
            return;
        }

        if (_resolvingChoice
            || CombatManager.Instance.IsOverOrEnding
            || CombatManager.Instance.PlayerActionsDisabled
            || state.CurrentSide != CombatSide.Player
            || player?.PlayerCombatState?.Phase != PlayerTurnPhase.Play)
        {
            return;
        }

        TaskHelper.RunSafely(ResolveChoice(ui, state, playButton, discardButton, scoreHand));
    }

    private static async Task ResolveChoice(
        NCombatUi ui,
        CombatState state,
        TextureButton playButton,
        TextureButton discardButton,
        bool scoreHand)
    {
        _resolvingChoice = true;
        playButton.Disabled = true;
        discardButton.Disabled = true;
        try
        {
            Player player = state.Players[0];
            BalatroTrainingPlayLimitPower? playLimit = player.Creature.GetPower<BalatroTrainingPlayLimitPower>();
            BalatroTrainingDiscardLimitPower? discardLimit = player.Creature.GetPower<BalatroTrainingDiscardLimitPower>();
            int remaining = scoreHand
                ? (int)(playLimit?.Amount ?? 0)
                : (int)(discardLimit?.Amount ?? 0);
            int maxSelected = scoreHand ? 5 : Math.Min(5, remaining);
            if (remaining <= 0)
            {
                return;
            }

            CardSelectorPrefs prefs = new(
                new LocString("events", scoreHand
                    ? "BS_ANCIENT_EVENT_BALATRO_TRAINING_DUMMY_EVENT.playPrompt"
                    : "BS_ANCIENT_EVENT_BALATRO_TRAINING_DUMMY_EVENT.discardPrompt"),
                1,
                maxSelected)
            {
                RequireManualConfirmation = true,
                Cancelable = true,
                PretendCardsCanBePlayed = true,
            };

            List<CardModel> selected = (await ui.Hand.SelectCards(
                prefs,
                card => card.Enchantment is PlayingCardSuitEnchantment,
                null)).ToList();
            if (selected.Count == 0)
            {
                return;
            }

            ThrowingPlayerChoiceContext context = new();
            if (scoreHand)
            {
                int score = CalculateScore(selected);
                foreach (PlayingCardSuitEnchantment suit in selected
                    .Select(card => card.Enchantment)
                    .OfType<PlayingCardSuitEnchantment>())
                {
                    await suit.TriggerForPokerTraining(context, player.Creature);
                }
                await CardPileCmd.Add(selected, PileType.Discard, CardPilePosition.Top, skipVisuals: false);
                playLimit?.SetAmount(Math.Max(0, playLimit.Amount - 1), silent: true);
                Creature? dummy = state.HittableEnemies.FirstOrDefault();
                if (dummy != null)
                {
                    await CreatureCmd.Damage(
                        context,
                        dummy,
                        score,
                        ValueProp.Unpowered | ValueProp.Move,
                        player.Creature,
                        selected[0]);
                }
            }
            else
            {
                await CardCmd.Discard(context, selected);
                discardLimit?.SetAmount(
                    Math.Max(0, discardLimit.Amount - selected.Count),
                    silent: true);
            }

            if (!CombatManager.Instance.IsOverOrEnding && !player.Creature.IsDead)
            {
                await CardPileCmd.Draw(context, selected.Count, player, fromHandDraw: true);
                SortPokerHand(player);
            }
        }
        finally
        {
            _resolvingChoice = false;
            if (GodotObject.IsInstanceValid(playButton))
            {
                playButton.Disabled = false;
            }
            if (GodotObject.IsInstanceValid(discardButton))
            {
                discardButton.Disabled = false;
            }
        }
    }

    internal static int CalculateScore(IReadOnlyList<CardModel> cards)
    {
        List<PlayingCardPokerCard<CardModel>> pokerCards = cards
            .Select(card => (Card: card, Suit: card.Enchantment as PlayingCardSuitEnchantment))
            .Where(entry => entry.Suit != null)
            .Select(entry => new PlayingCardPokerCard<CardModel>(
                entry.Card,
                Math.Clamp(entry.Suit!.Amount, 1, 13),
                entry.Suit.PokerSuit))
            .ToList();
        return BalatroPokerScoring.Calculate(pokerCards);
    }

    private static void ApplySuit(CardModel card, PlayingCardSuit suit, int rank)
    {
        switch (suit)
        {
            case PlayingCardSuit.Heart:
                CardCmd.Enchant<HeartSuitEnchantment>(card, rank);
                break;
            case PlayingCardSuit.Diamond:
                CardCmd.Enchant<DiamondSuitEnchantment>(card, rank);
                break;
            case PlayingCardSuit.Club:
                CardCmd.Enchant<ClubSuitEnchantment>(card, rank);
                break;
            case PlayingCardSuit.Spade:
                CardCmd.Enchant<SpadeSuitEnchantment>(card, rank);
                break;
        }
    }

    /// <summary>Sorts the current poker hand from high rank to low rank.</summary>
    internal static void SortPokerHand(Player player)
    {
        CardPile hand = PileType.Hand.GetPile(player);
        List<CardModel> sorted = hand.Cards
            .Where(card => card.Enchantment is PlayingCardSuitEnchantment)
            .OrderByDescending(card => GetPokerRankValue(((PlayingCardSuitEnchantment)card.Enchantment!).Amount))
            .ThenBy(card => ((PlayingCardSuitEnchantment)card.Enchantment!).PokerSuit)
            .ToList();

        // Move in reverse order so the final hand order is ascending.
        foreach (CardModel card in sorted.AsEnumerable().Reverse())
        {
            hand.MoveToTopInternal(card);
        }

        if (sorted.Count > 0)
        {
            hand.InvokeContentsChanged();
            SortPokerHandVisuals(player, sorted);
        }
    }

    private static int GetPokerRankValue(int rank) => rank == 1 ? 14 : rank;

    /// <summary>Keeps the visible hand in the same order as its backend pile.</summary>
    private static void SortPokerHandVisuals(Player player, IReadOnlyList<CardModel> sorted)
    {
        NPlayerHand? visualHand = NPlayerHand.Instance;
        if (visualHand == null)
        {
            return;
        }

        for (int index = 0; index < sorted.Count; index++)
        {
            if (visualHand.GetCardHolder(sorted[index]) is { } holder
                && holder.GetParent() == visualHand.CardHolderContainer)
            {
                visualHand.CardHolderContainer.MoveChild(holder, index);
            }
        }

        visualHand.ForceRefreshCardIndices();
    }
}
