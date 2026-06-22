using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterEnchantment]
public sealed class AscensionEnchantment : ModEnchantmentTemplate
{
    private const int InitialRemainingNodes = 7;
    private const string AscensionIconPath = "res://bs_ancient/assets/images/enchantment/AscensionEnchantment.png";

    private int _remainingNodes = InitialRemainingNodes;

    public override bool ShowAmount => true;

    public override int DisplayAmount => BlackSouls_RemainingNodes;

    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(IconPath: AscensionIconPath);

    [SavedProperty]
    public int BlackSouls_RemainingNodes
    {
        get => _remainingNodes;
        set
        {
            AssertMutable();
            _remainingNodes = Math.Max(0, value);
            SyncAmountForDisplayAndLoc();
        }
    }

    protected override void OnEnchant()
    {
        SyncAmountForDisplayAndLoc();
    }

    public override bool CanEnchant(CardModel card)
    {
        return base.CanEnchant(card)
            && card.IsTransformable
            && GetHigherRarity(card.Rarity) != null;
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (IsCombatRoom(room) || IsCurrentPointCombat() || !ShouldCountCurrentNode() || Card.HasBeenRemovedFromState)
        {
            return;
        }

        await CountNode();
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        return IsCombatRoom(room) ? CountNode() : Task.CompletedTask;
    }

    private async Task CountNode()
    {
        if (Card.HasBeenRemovedFromState)
        {
            return;
        }

        BlackSouls_RemainingNodes--;
        if (BlackSouls_RemainingNodes <= 0)
        {
            CardModel? replacement = CreateHigherRarityReplacement();
            if (replacement == null)
            {
                return;
            }

            CardModel target = Card.DeckVersion ?? Card;
            if (!target.HasBeenRemovedFromState)
            {
                await CardCmd.Transform(target, replacement, CardPreviewStyle.MessyLayout);
            }
        }
    }

    private void SyncAmountForDisplayAndLoc()
    {
        Amount = BlackSouls_RemainingNodes;
    }

    private bool ShouldCountCurrentNode()
    {
        MapPoint? currentPoint = Card.Owner?.RunState.CurrentMapPoint;
        return currentPoint != null
            && currentPoint.PointType is not MapPointType.Ancient and not MapPointType.Unassigned;
    }

    private bool IsCurrentPointCombat()
    {
        MapPoint? currentPoint = Card.Owner?.RunState.CurrentMapPoint;
        return currentPoint?.PointType is MapPointType.Monster or MapPointType.Elite;
    }

    private static bool IsCombatRoom(CombatRoom room)
    {
        return room.RoomType is RoomType.Monster or RoomType.Elite;
    }

    private static bool IsCombatRoom(AbstractRoom room)
    {
        return room is CombatRoom || room.RoomType is RoomType.Monster or RoomType.Elite;
    }

    private CardModel? CreateHigherRarityReplacement()
    {
        if (Card.Owner?.RunState is not RunState runState)
        {
            return null;
        }

        CardRarity? targetRarity = GetHigherRarity(Card.Rarity);
        if (targetRarity == null)
        {
            return null;
        }

        IReadOnlyList<CardModel> candidates = GetCandidatePool(targetRarity.Value).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        CardModel? canonical = Card.Owner.RunState.Rng.Niche.NextItem(candidates);
        return canonical == null ? null : runState.CreateCard(canonical, Card.Owner);
    }

    private IEnumerable<CardModel> GetCandidatePool(CardRarity rarity)
    {
        return Card.Owner.Character.CardPool
            .GetUnlockedCards(Card.Owner.UnlockState, Card.Owner.RunState.CardMultiplayerConstraint)
            .Where(card => card.Rarity == rarity && IsValidReplacement(card));
    }

    private static CardRarity? GetHigherRarity(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Basic or CardRarity.Common => CardRarity.Uncommon,
            CardRarity.Uncommon => CardRarity.Rare,
            _ => null
        };
    }

    private static bool IsValidReplacement(CardModel card)
    {
        return card.Type != CardType.Quest
            && card.Rarity is not (CardRarity.Curse or CardRarity.Status or CardRarity.Event or CardRarity.Token);
    }
}
