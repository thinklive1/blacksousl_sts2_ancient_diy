using BlackSouls.Scripts.Patches;
using HarmonyLib;
using STS2RitsuLib.Patching.Builders;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

namespace BlackSouls.Scripts;

/// <summary>Registers BS Ancient patch groups.</summary>
public sealed class BsAncientPatchSet : IModPatches
{
    public static void AddTo(ModPatcher patcher)
    {
        patcher.RegisterPatch<EvilQiAfflictionOverwritePatch>();
        patcher.RegisterPatch<ChainsOfBindingEvilQiOverwritePatch>();
        patcher.RegisterPatch<GrandGuignolAncientSpawnPatch>();
        patcher.RegisterPatch<GrandGuignolRelicCollectionPatch>();
        patcher.RegisterPatch<NeowRethinkPokerPatch>();
        patcher.RegisterPatch<FairyTaleModeCharacterSelectPatch>();
        patcher.RegisterPatch<FairyTaleModeRunLifecyclePatch>();
        patcher.RegisterPatch<OnlyUseModAncientsPatch>();
        patcher.RegisterPatch<WormSmokeRestSiteIconPatch>();
        patcher.RegisterPatch<UnicornHookBeforeDamageReceivedPatch>();
        patcher.RegisterPatch<UnicornHookAfterDamageReceivedPatch>();
        patcher.RegisterPatch<UnicornThornsBeforeDamageReceivedPatch>();
        patcher.RegisterPatch<UnicornPersonalHiveAfterDamageReceivedPatch>();
        patcher.RegisterPatch<UnlockEnchantmentCanPlayPatch>();
        patcher.RegisterPatch<BaphometFavorCanPlayPatch>();
        patcher.RegisterPatch<MirrorSanFairyResetPatch>();
        patcher.RegisterPatch<MirrorSanLizardTailResetPatch>();
        patcher.RegisterPatch<RapunzelPowerSetAmountPatch>();
        patcher.RegisterPatch<RapunzelPowerRemovePatch>();
        patcher.RegisterPatch<HelmsmansPageRelicPatch>();
        patcher.RegisterPatch<BalatroBeforeHandDrawPatch>();
        patcher.RegisterPatch<BalatroModifyHandDrawPatch>();
        patcher.RegisterPatch<BalatroAfterPlayerTurnStartPatch>();
        patcher.RegisterPatch<BalatroAfterCardDrawnPatch>();
        patcher.RegisterPatch<CroquetMalletAfterCardDrawnPatch>();
        patcher.RegisterPatch<CroquetMalletBeforeCardPlayWrapperPatch>();
        patcher.RegisterPatch<CroquetMalletAfterCardPlayedPatch>();
        patcher.RegisterPatch<CroquetMalletAfterPlayerTurnStartPatch>();
        patcher.RegisterPatch<BalatroCombatUiPatch>();
        patcher.RegisterPatch<BalatroDummyTimeLimitPatch>();
        patcher.RegisterPatch<BalatroDirectCardPlayPatch>();
        patcher.RegisterPatch<QueenOfHeartsNebulaDeckRewardPatch>();
        patcher.RegisterPatch<QueenOfHeartsNebulaDeckCardRewardCountPatch>();
        RegisterMercuryCardDescriptionPatch(patcher);
    }

    private static void RegisterMercuryCardDescriptionPatch(ModPatcher patcher)
    {
        HarmonyMethod postfix = DynamicPatchBuilder.FromMethod(
            typeof(MercuryCardDescriptionPatch),
            nameof(MercuryCardDescriptionPatch.Postfix));
        postfix.priority = HarmonyLib.Priority.High;

        DynamicPatchBuilder builder = new("mercury_card_description");
        int index = 0;
        foreach (System.Reflection.MethodBase target in MercuryCardDescriptionPatch.TargetMethods())
        {
            builder.Add(
                target,
                null,
                postfix,
                null,
                null,
                false,
                $"mercury_card_description_{index++}",
                $"Show copied card description on Mercury -> {target.Name}");
        }

        patcher.ApplyDynamic(builder, rollbackOnCriticalFailure: false);
    }
}
