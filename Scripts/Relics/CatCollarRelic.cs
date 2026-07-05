using BlackSouls.Scripts.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public class CatCollarRelic : ModRelicTemplate
{
    public const int RequiredTransformCards = 2;

    private const int MaxTriggersPerTurn = 1;
    private const int PlaysForIntangible = 2;

    public override bool AddsPet => true;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://bs_ancient/assets/images/relics/Cat.png",
        IconOutlinePath: "res://bs_ancient/assets/images/relics/Cat.png",
        BigIconPath: "res://bs_ancient/assets/images/relics/Cat.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(RequiredTransformCards),
        new DynamicVar("MaxTriggers", MaxTriggersPerTurn),
        new DynamicVar("PlaysForIntangible", PlaysForIntangible)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<CatSmileCard>()
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<CatBiteCard>());

    public override async Task AfterObtained()
    {
        List<CardModel> selectedCards = (await CardSelectCmd.FromDeckGeneric(
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, RequiredTransformCards, RequiredTransformCards)
                {
                    Cancelable = false,
                    RequireManualConfirmation = true
                },
                IsTransformableDeckCard))
            .ToList();

        if (selectedCards.Count < RequiredTransformCards)
        {
            return;
        }

        CardModel smile = Owner.RunState.CreateCard<CatSmileCard>(Owner);
        CardModel bite = Owner.RunState.CreateCard<CatBiteCard>(Owner);

        await CardCmd.Transform(
            [
                new CardTransformation(selectedCards[0], smile),
                new CardTransformation(selectedCards[1], bite)
            ],
            null);

        if (CombatManager.Instance.IsInProgress)
        {
            await EnsureSmileCountdown(PlaysForIntangible);
            await SummonPet();
        }
    }

    public override async Task BeforeCombatStart()
    {
        await EnsureSmileCountdown(PlaysForIntangible);
        await SetTurnTriggerLimit(MaxTriggersPerTurn);
        await SummonPet();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            await SetTurnTriggerLimit(MaxTriggersPerTurn);
        }
    }

    public async Task<bool> TryTriggerCatCardEffect(string animationTrigger)
    {
        CatCollarTriggerLimitPower? triggerLimit = Owner.Creature.GetPower<CatCollarTriggerLimitPower>();
        if (triggerLimit == null || triggerLimit.Amount <= 0)
        {
            return false;
        }

        await PowerCmd.Decrement(triggerLimit);
        Flash();
        BsAncientAudio.PlayOneShot(BsAncientAudio.Cat);

        await PlayCatAnimation(animationTrigger);

        return true;
    }

    public async Task<bool> RecordSmileAndShouldGainIntangible()
    {
        int remainingTriggers = GetSmileCountdown();
        if (remainingTriggers > 1)
        {
            await SetSmileCountdown(remainingTriggers - 1);
            return false;
        }

        await SetSmileCountdown(PlaysForIntangible);
        return true;
    }

    public static bool CanBeOffered(Player player)
    {
        if (player.Relics.Any(relic => relic.AddsPet))
        {
            return false;
        }

        return PileType.Deck.GetPile(player).Cards.Count(IsTransformableDeckCard) >= RequiredTransformCards;
    }

    private static bool IsTransformableDeckCard(CardModel card)
    {
        return card.Type != CardType.Quest && card.IsTransformable;
    }

    private async Task SummonPet()
    {
        if (Owner == null)
        {
            return;
        }

        if (Owner.PlayerCombatState?.GetPet<CheshireCatPet>() == null)
        {
            await PlayerCmd.AddPet<CheshireCatPet>(Owner);
        }

        GetCatVisuals()?.ShowSmileIdle();
    }

    private Task PlayCatAnimation(string animationTrigger)
    {
        if (Owner?.PlayerCombatState?.GetPet<CheshireCatPet>() is not { } petCreature)
        {
            return Task.CompletedTask;
        }

        CheshireCatPetVisuals? visuals = NCombatRoom.Instance?.GetCreatureNode(petCreature)?.Visuals as CheshireCatPetVisuals;

        if (animationTrigger == "SmileTrigger")
        {
            visuals?.PlaySmile();
            return Task.CompletedTask;
        }

        if (animationTrigger == "BiteTrigger")
        {
            visuals?.PlayBite();
            return Task.CompletedTask;
        }

        return CreatureCmd.TriggerAnim(petCreature, animationTrigger, 0.15f);
    }

    private int GetSmileCountdown()
    {
        return Owner?.Creature.GetPower<CatSmileCountdownPower>()?.Amount ?? PlaysForIntangible;
    }

    private Task EnsureSmileCountdown(int remainingTriggers)
    {
        return SetSmileCountdown(remainingTriggers);
    }

    private Task SetSmileCountdown(int remainingTriggers)
    {
        if (Owner?.Creature == null || !CombatManager.Instance.IsInProgress)
        {
            return Task.CompletedTask;
        }

        return BsPowerCmd.SetAmount<CatSmileCountdownPower>(
            Owner.Creature,
            remainingTriggers,
            Owner.Creature,
            null);
    }

    private Task SetTurnTriggerLimit(int remainingTriggers)
    {
        if (Owner?.Creature == null || !CombatManager.Instance.IsInProgress)
        {
            return Task.CompletedTask;
        }

        return BsPowerCmd.SetAmount<CatCollarTriggerLimitPower>(
            Owner.Creature,
            remainingTriggers,
            Owner.Creature,
            null);
    }

    private CheshireCatPetVisuals? GetCatVisuals()
    {
        if (Owner?.PlayerCombatState?.GetPet<CheshireCatPet>() is not { } petCreature)
        {
            return null;
        }

        return NCombatRoom.Instance?.GetCreatureNode(petCreature)?.Visuals as CheshireCatPetVisuals;
    }
}
