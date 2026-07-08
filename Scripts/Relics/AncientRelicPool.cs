using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace BlackSouls.Scripts;

/// <summary>Defines the custom ancient relic pool.</summary>
[RegisterSharedRelicPool]
public sealed class AncientRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "Colorless";

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        // Grand Guignol
        yield return ModelDb.Relic<RethinkPokerRelic>();
        yield return ModelDb.Relic<WormSmokeRelic>();
        yield return ModelDb.Relic<MargaretRelic>();
        yield return ModelDb.Relic<AngelFeatherRelic>();
        yield return ModelDb.Relic<QuestionAngelFeatherRelic>();
        yield return ModelDb.Relic<BrutalizingAngelFeatherRelic>();
        yield return ModelDb.Relic<MabelSoldierRelic>();
        yield return ModelDb.Relic<GuignolsDollRelic>();
        // Node
        yield return ModelDb.Relic<NodeRibbonRelic>();
        yield return ModelDb.Relic<DreamOfKadathRelic>();
        yield return ModelDb.Relic<WhiteQueenSoldierRelic>();
        yield return ModelDb.Relic<WhiteQueenPromotionRelic>();
        yield return ModelDb.Relic<TimeQueenBlessingRelic>();
        yield return ModelDb.Relic<WinterBellAllyRelic>();
        yield return ModelDb.Relic<StagnantGearRelic>();
        yield return ModelDb.Relic<CatCollarRelic>();
        yield return ModelDb.Relic<QuillPenRelic>();
        yield return ModelDb.Relic<CovenantOfNodeRelic>();
        yield return ModelDb.Relic<UnicornRoyalCrestRelic>();
        yield return ModelDb.Relic<LionRoyalCrestRelic>();
        // Prickett
        yield return ModelDb.Relic<RedQueenAlbumRelic>();
        yield return ModelDb.Relic<CovenantOfPrickettRelic>();
        yield return ModelDb.Relic<RedQueenSoldierRelic>();
        yield return ModelDb.Relic<RedQueenPromotionRelic>();
        yield return ModelDb.Relic<RedQueenDiceRelic>();
        yield return ModelDb.Relic<RedQueenMirrorRelic>();
        yield return ModelDb.Relic<AliceRibbonRelic>();
        yield return ModelDb.Relic<OldFilmA>();
        yield return ModelDb.Relic<OldFilmB>();
        yield return ModelDb.Relic<AliceCurseRelic>();
        // Mabel
        yield return ModelDb.Relic<HlanithWineRelic>();
        yield return ModelDb.Relic<StageEndRelic>();
        yield return ModelDb.Relic<SilverKeyRelic>();
        yield return ModelDb.Relic<EternalVanityRelic>();
        yield return ModelDb.Relic<MysteryOfNightSkyRelic>();
        yield return ModelDb.Relic<GiftOfChaosRelic>();
        yield return ModelDb.Relic<RapunzelFavorRelic>();
        yield return ModelDb.Relic<LittleMermaidFavorRelic>();
        yield return ModelDb.Relic<PrincessFrogFavorRelic>();
        yield return ModelDb.Relic<SnowWhiteFavorRelic>();
        yield return ModelDb.Relic<CinderellaFavorRelic>();
        yield return ModelDb.Relic<BaphometFavorRelic>();
    }
}
