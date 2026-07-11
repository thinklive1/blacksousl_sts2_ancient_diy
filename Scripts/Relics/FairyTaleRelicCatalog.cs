using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts;

/// <summary>Provides the shared fairy tale and myth relic catalog.</summary>
public static class FairyTaleRelicCatalog
{
    public static IEnumerable<RelicModel> All()
    {
        yield return ModelDb.Relic<PinocchioRelic>();
        yield return ModelDb.Relic<AliceThroughLookingGlassRelic>();
        yield return ModelDb.Relic<ThreeLittlePigsRelic>();
        yield return ModelDb.Relic<EmperorsNewClothesRelic>();
        yield return ModelDb.Relic<AlicuxelsDogRelic>();
        yield return ModelDb.Relic<SongOfBoneRelic>();
        yield return ModelDb.Relic<FrogPrincessRelic>();
        yield return ModelDb.Relic<FoxAndSourGrapesRelic>();
        yield return ModelDb.Relic<PiedPiperOfHamelinRelic>();
        yield return ModelDb.Relic<JackAndTheBeanstalkRelic>();
        yield return ModelDb.Relic<AladdinAndTheMagicLampRelic>();
        yield return ModelDb.Relic<BeautyAndTheBeastRelic>();
        yield return ModelDb.Relic<UglyDucklingRelic>();
        yield return ModelDb.Relic<HighJumperRelic>();
        yield return ModelDb.Relic<RapunzelFairyTaleRelic>();
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
        yield return ModelDb.Relic<RobinHoodRelic>();
        yield return ModelDb.Relic<WhiteRabbitOfInabaRelic>();
        yield return ModelDb.Relic<DaddyLongLegsRelic>();
        yield return ModelDb.Relic<TheBoyWhoCriedWolfRelic>();
        yield return ModelDb.Relic<CandyHouseRelic>();
        yield return ModelDb.Relic<SnowQueenRelic>();
        yield return ModelDb.Relic<MermaidPrincessRelic>();
        yield return ModelDb.Relic<NorthWindAndSunRelic>();
        yield return ModelDb.Relic<SnowWhiteRelic>();
        yield return ModelDb.Relic<CinderellaRelic>();
        yield return ModelDb.Relic<TurnipRelic>();
        yield return ModelDb.Relic<SleepGodMythRelic>();
        yield return ModelDb.Relic<LakeGodMythRelic>();
        yield return ModelDb.Relic<DarkGoatOfTheWoodsMythRelic>();
        yield return ModelDb.Relic<GreatStagGoddessMythRelic>();
        yield return ModelDb.Relic<BlackThingMythRelic>();
        yield return ModelDb.Relic<ShadowDemonessMythRelic>();
        yield return ModelDb.Relic<HaraldShipmanNewsRelic>();
        yield return ModelDb.Relic<JackKetchNewsRelic>();
    }

    public static bool IsEnabledByConfig(RelicModel relic)
    {
        if (IsNegative(relic))
        {
            return BsAncientConfig.EnableNegativeFairyTaleRelics && IsEnabledByIndividualConfig(relic);
        }

        return BsAncientConfig.EnablePositiveFairyTaleRelics;
    }

    private static bool IsEnabledByIndividualConfig(RelicModel relic)
    {
        if (relic is AliceThroughLookingGlassRelic)
            return BsAncientConfig.AllowAliceThroughLookingGlass;
        if (relic is CinderellaRelic)
            return BsAncientConfig.AllowCinderella;
        if (relic is FrogPrincessRelic)
            return BsAncientConfig.AllowFrogPrincess;
        if (relic is GreedyDogRelic)
            return BsAncientConfig.AllowGreedyDog;
        if (relic is MermaidPrincessRelic)
            return BsAncientConfig.AllowMermaidPrincess;
        if (relic is MonkeyCrabBattleRelic)
            return BsAncientConfig.AllowMonkeyCrabBattle;
        if (relic is NorthWindAndSunRelic)
            return BsAncientConfig.AllowNorthWindAndSun;
        if (relic is PeterPanRelic)
            return BsAncientConfig.AllowPeterPan;
        if (relic is UglyDucklingRelic)
            return BsAncientConfig.AllowUglyDuckling;
        if (relic is SleepGodMythRelic)
            return BsAncientConfig.AllowSleepGodMyth;
        if (relic is LakeGodMythRelic)
            return BsAncientConfig.AllowLakeGodMyth;
        if (relic is DarkGoatOfTheWoodsMythRelic)
            return BsAncientConfig.AllowDarkGoatOfTheWoodsMyth;
        if (relic is GreatStagGoddessMythRelic)
            return BsAncientConfig.AllowGreatStagGoddessMyth;
        if (relic is BlackThingMythRelic)
            return BsAncientConfig.AllowBlackThingMyth;
        if (relic is ShadowDemonessMythRelic)
            return BsAncientConfig.AllowShadowDemonessMyth;
        if (relic is HaraldShipmanNewsRelic)
            return BsAncientConfig.AllowHaraldShipmanNews;
        if (relic is JackKetchNewsRelic)
            return BsAncientConfig.AllowJackKetchNews;

        return true;
    }

    private static bool IsNegative(RelicModel relic)
    {
        return relic is AliceThroughLookingGlassRelic
            or ThreeLittlePigsRelic
            or EmperorsNewClothesRelic
            or FrogPrincessRelic
            or FoxAndSourGrapesRelic
            or PiedPiperOfHamelinRelic
            or JackAndTheBeanstalkRelic
            or BeautyAndTheBeastRelic
            or UglyDucklingRelic
            or HighJumperRelic
            or RapunzelFairyTaleRelic
            or MyFormerRascalRelic
            or SinbadTheSailorRelic
            or IronHansRelic
            or FlandersDogRelic
            or LittlePrinceRelic
            or PeterPanRelic
            or MonkeyCrabBattleRelic
            or GreedyDogRelic
            or TheBoyWhoCriedWolfRelic
            or CandyHouseRelic
            or MermaidPrincessRelic
            or NorthWindAndSunRelic
            or CinderellaRelic
            or SleepGodMythRelic
            or LakeGodMythRelic
            or DarkGoatOfTheWoodsMythRelic
            or GreatStagGoddessMythRelic
            or BlackThingMythRelic
            or ShadowDemonessMythRelic
            or HaraldShipmanNewsRelic
            or JackKetchNewsRelic;
    }
}
