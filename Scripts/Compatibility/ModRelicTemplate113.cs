#if BS_BETA_1130
using MegaCrit.Sts2.Core.Runs;

namespace BlackSouls.Scripts.Compatibility;

/// <summary>Routes existing relic eligibility rules through the 0.113 internal hook.</summary>
public abstract class ModRelicTemplate113 : STS2RitsuLib.Scaffolding.Content.ModRelicTemplate
{
    public new virtual bool IsAllowed(IRunState runState) => base.IsAllowedInternal(runState);

    protected override bool IsAllowedInternal(IRunState runState) => IsAllowed(runState);
}
#endif
