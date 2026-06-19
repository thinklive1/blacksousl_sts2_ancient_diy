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
    }
}
