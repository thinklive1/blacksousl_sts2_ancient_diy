using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts;

public sealed class FairyTaleRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "Colorless";

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        yield return ModelDb.Relic<PinocchioRelic>();
        yield return ModelDb.Relic<AliceThroughLookingGlassRelic>();
        yield return ModelDb.Relic<ThreeLittlePigsRelic>();
        yield return ModelDb.Relic<EmperorsNewClothesRelic>();
        yield return ModelDb.Relic<AlicuxelsDogRelic>();
        yield return ModelDb.Relic<SongOfBoneRelic>();
        yield return ModelDb.Relic<FoxAndSourGrapesRelic>();
        yield return ModelDb.Relic<PiedPiperOfHamelinRelic>();
        yield return ModelDb.Relic<JackAndTheBeanstalkRelic>();
        yield return ModelDb.Relic<AladdinAndTheMagicLampRelic>();
        yield return ModelDb.Relic<BeautyAndTheBeastRelic>();
        yield return ModelDb.Relic<UglyDucklingRelic>();
        yield return ModelDb.Relic<HighJumperRelic>();
        yield return ModelDb.Relic<WolfAndLittleGoatsRelic>();
        yield return ModelDb.Relic<MyFormerRascalRelic>();
        yield return ModelDb.Relic<SinbadTheSailorRelic>();
        yield return ModelDb.Relic<TownMusiciansOfBremenRelic>();
        yield return ModelDb.Relic<IronHansRelic>();
        yield return ModelDb.Relic<FlandersDogRelic>();
        yield return ModelDb.Relic<LittlePrinceRelic>();
        yield return ModelDb.Relic<ArmoredKnightRelic>();
        yield return ModelDb.Relic<KingWithDonkeyEarsRelic>();
        yield return ModelDb.Relic<PeterPanRelic>();
        yield return ModelDb.Relic<MonkeyCrabBattleRelic>();
        yield return ModelDb.Relic<GreedyDogRelic>();
        yield return ModelDb.Relic<TortoiseAndHareRelic>();
        yield return ModelDb.Relic<KachiKachiYamaRelic>();
    }
}
