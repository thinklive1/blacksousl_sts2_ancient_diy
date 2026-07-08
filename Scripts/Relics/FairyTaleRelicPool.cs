using MegaCrit.Sts2.Core.Models;

namespace BlackSouls.Scripts;

/// <summary>Defines the fairy tale relic pool.</summary>
public sealed class FairyTaleRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "Colorless";

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        foreach (RelicModel relic in FairyTaleRelicCatalog.All())
        {
            yield return relic;
        }
    }
}
