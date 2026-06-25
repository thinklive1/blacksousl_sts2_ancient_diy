using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BlackSouls.Scripts;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class UnnamedFairyTaleBookRelic : ModRelicTemplate
{
    private const int NodesPerReward = 4;
    private const string RelicIconPath = "res://bs_ancient/assets/images/relics/FairyTaleRelic.png";
    private const char ObtainedRelicSeparator = '|';

    private int _remainingNodes = NodesPerReward;
    private string _obtainedFairyTaleRelicIds = string.Empty;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool ShowCounter => true;

    public override int DisplayAmount => BlackSouls_RemainingNodes;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Nodes", NodesPerReward)
    ];

    [SavedProperty]
    public int BlackSouls_RemainingNodes
    {
        get => _remainingNodes;
        set
        {
            AssertMutable();
            _remainingNodes = Math.Clamp(value, 0, NodesPerReward);
            InvokeDisplayAmountChanged();
        }
    }

    [SavedProperty]
    public string BlackSouls_ObtainedFairyTaleRelicIds
    {
        get => _obtainedFairyTaleRelicIds;
        set
        {
            AssertMutable();
            _obtainedFairyTaleRelicIds = value ?? string.Empty;
        }
    }

    public override RelicAssetProfile AssetProfile => new(
        IconPath: RelicIconPath,
        IconOutlinePath: RelicIconPath,
        BigIconPath: RelicIconPath
    );

    public override bool IsAllowed(IRunState runState)
    {
        return false;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        return IsCombatRoom(room) ? CountCombat() : Task.CompletedTask;
    }

    private async Task CountCombat()
    {
        if (Owner == null)
        {
            return;
        }

        BlackSouls_RemainingNodes--;
        if (BlackSouls_RemainingNodes > 0)
        {
            return;
        }

        Flash();
        await GiveRandomFairyTale();
        BlackSouls_RemainingNodes = NodesPerReward;
    }

    private static bool IsCombatRoom(CombatRoom room)
    {
        return room.RoomType is RoomType.Monster or RoomType.Elite or RoomType.Boss;
    }

    private async Task GiveRandomFairyTale()
    {
        if (Owner == null)
        {
            return;
        }

        HashSet<string> obtainedRelicIds = ObtainedFairyTaleRelicIds();
        HashSet<string> ownedRelicIds = Owner.Relics
            .Select(relic => relic.Id.Entry)
            .ToHashSet(StringComparer.Ordinal);

        List<RelicModel> candidates = FairyTaleRelics()
            .Where(relic => !obtainedRelicIds.Contains(relic.Id.Entry) && !ownedRelicIds.Contains(relic.Id.Entry))
            .ToList();

        RelicModel? relic = Owner.RunState.Rng.Niche.NextItem(candidates);
        if (relic != null)
        {
            RecordObtainedFairyTale(relic);
            await RelicCmd.Obtain(relic.ToMutable(), Owner);
        }
    }

    private HashSet<string> ObtainedFairyTaleRelicIds()
    {
        return BlackSouls_ObtainedFairyTaleRelicIds
            .Split(ObtainedRelicSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }

    private void RecordObtainedFairyTale(RelicModel relic)
    {
        HashSet<string> obtainedRelicIds = ObtainedFairyTaleRelicIds();
        if (!obtainedRelicIds.Add(relic.Id.Entry))
        {
            return;
        }

        BlackSouls_ObtainedFairyTaleRelicIds = string.Join(ObtainedRelicSeparator, obtainedRelicIds.Order(StringComparer.Ordinal));
    }

    private static List<RelicModel> FairyTaleRelics() =>
    [
        ModelDb.Relic<PinocchioRelic>(),
        ModelDb.Relic<AliceThroughLookingGlassRelic>(),
        ModelDb.Relic<ThreeLittlePigsRelic>(),
        ModelDb.Relic<EmperorsNewClothesRelic>(),
        ModelDb.Relic<AlicuxelsDogRelic>(),
        ModelDb.Relic<SongOfBoneRelic>(),
        ModelDb.Relic<FrogPrincessRelic>(),
        ModelDb.Relic<FoxAndSourGrapesRelic>(),
        ModelDb.Relic<PiedPiperOfHamelinRelic>(),
        ModelDb.Relic<JackAndTheBeanstalkRelic>(),
        ModelDb.Relic<AladdinAndTheMagicLampRelic>(),
        ModelDb.Relic<BeautyAndTheBeastRelic>(),
        ModelDb.Relic<UglyDucklingRelic>(),
        ModelDb.Relic<HighJumperRelic>(),
        ModelDb.Relic<RapunzelFairyTaleRelic>(),
        ModelDb.Relic<WolfAndLittleGoatsRelic>(),
        ModelDb.Relic<MyFormerRascalRelic>(),
        ModelDb.Relic<SinbadTheSailorRelic>(),
        ModelDb.Relic<TownMusiciansOfBremenRelic>(),
        ModelDb.Relic<IronHansRelic>(),
        ModelDb.Relic<FlandersDogRelic>(),
        ModelDb.Relic<LittlePrinceRelic>(),
        ModelDb.Relic<ArmoredKnightRelic>(),
        ModelDb.Relic<KingWithDonkeyEarsRelic>(),
        ModelDb.Relic<PeterPanRelic>(),
        ModelDb.Relic<MonkeyCrabBattleRelic>(),
        ModelDb.Relic<GreedyDogRelic>(),
        ModelDb.Relic<TortoiseAndHareRelic>(),
        ModelDb.Relic<KachiKachiYamaRelic>(),
        ModelDb.Relic<RobinHoodRelic>(),
        ModelDb.Relic<WhiteRabbitOfInabaRelic>(),
        ModelDb.Relic<DaddyLongLegsRelic>(),
        ModelDb.Relic<TheBoyWhoCriedWolfRelic>(),
        ModelDb.Relic<CandyHouseRelic>(),
        ModelDb.Relic<SnowQueenRelic>(),
        ModelDb.Relic<MermaidPrincessRelic>(),
        ModelDb.Relic<NorthWindAndSunRelic>(),
        ModelDb.Relic<SnowWhiteRelic>()
    ];
}
