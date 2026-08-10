using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts;

/// <summary>Binds the character-select fairy-tale choice to the launched run.</summary>
public sealed class FairyTaleModeRunLifecyclePatch : IPatchMethod
{
    public static string PatchId => "fairy_tale_mode_run_lifecycle";

    public static string Description => "Bind the pending Fairy Tale Mode selection to one run.";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(RunManager), nameof(RunManager.Launch), ignoreIfMissing: true)];

    public static void Postfix(RunState __result)
    {
        BsAncientRunOptions.CaptureFairyTaleModeForRun(__result);
    }
}
