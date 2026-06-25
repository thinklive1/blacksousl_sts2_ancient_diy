using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class NorthWindAndSunRelic : ModRelicTemplate
{
    private const int MaxTransform = 3;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";

    private int _lastKnownMaxHp;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Count", MaxTransform)
    ];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override async Task AfterObtained()
    {
        if (Owner == null)
        {
            return;
        }

        Rng selectRng = Owner.RunState.Rng.Niche;
        Rng transformRng = Owner.PlayerRng.Transformations;
        IReadOnlyList<CardModel> deck = PileType.Deck.GetPile(Owner).Cards;

        List<CardModel> attackCards = deck
            .Where(c => c.IsTransformable && c.Type == CardType.Attack)
            .ToList();
        List<CardModel> skillCards = deck
            .Where(c => c.IsTransformable && c.Type == CardType.Skill)
            .ToList();

        List<CardModel> poolCards = Owner.Character.CardPool
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .ToList();

        List<CardModel> skillOptions = poolCards
            .Where(c => c.Type == CardType.Skill)
            .ToList();
        List<CardModel> attackOptions = poolCards
            .Where(c => c.Type == CardType.Attack)
            .ToList();

        List<CardTransformation> transformations = [];

        // Attack → Skill
        List<CardModel> selectedAttacks = ShuffleTake(attackCards, MaxTransform, selectRng);
        foreach (CardModel original in selectedAttacks)
        {
            if (TryCreateReplacement(original, skillOptions, transformRng, out CardModel? replacement))
            {
                CardCmd.Upgrade(replacement!);
                transformations.Add(new CardTransformation(original, replacement!));
            }
        }

        // Skill → Attack
        List<CardModel> selectedSkills = ShuffleTake(skillCards, MaxTransform, selectRng);
        foreach (CardModel original in selectedSkills)
        {
            if (TryCreateReplacement(original, attackOptions, transformRng, out CardModel? replacement))
            {
                CardCmd.Upgrade(replacement!);
                transformations.Add(new CardTransformation(original, replacement!));
            }
        }

        if (transformations.Count > 0)
        {
            await CardCmd.Transform(transformations, null);
        }
    }

    public override Task BeforeCombatStart()
    {
        if (Owner?.Creature != null)
        {
            _lastKnownMaxHp = Owner.Creature.MaxHp;
        }

        return Task.CompletedTask;
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (delta <= 0 || creature != Owner?.Creature)
        {
            return Task.CompletedTask;
        }

        int maxHpDelta = creature.MaxHp - _lastKnownMaxHp;
        _lastKnownMaxHp = creature.MaxHp;

        decimal effectiveHeal = delta - maxHpDelta;
        if (effectiveHeal > 0)
        {
            creature.SetCurrentHpInternal(creature.CurrentHp - effectiveHeal);
        }

        return Task.CompletedTask;
    }

    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
    {
        return creature == Owner?.Creature ? 0m : amount;
    }

    private static bool TryCreateReplacement(
        CardModel original,
        List<CardModel> options,
        Rng rng,
        out CardModel? replacement)
    {
        try
        {
            replacement = CardFactory.CreateRandomCardForTransform(
                original, options, isInCombat: false, rng);
            return true;
        }
        catch (InvalidOperationException)
        {
            replacement = null;
            return false;
        }
    }

    private static List<CardModel> ShuffleTake(List<CardModel> source, int count, Rng rng)
    {
        if (source.Count <= count)
        {
            return [.. source];
        }

        List<CardModel> pool = [.. source];
        List<CardModel> result = [];
        for (int i = 0; i < count; i++)
        {
            int index = rng.NextInt(pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }
}
