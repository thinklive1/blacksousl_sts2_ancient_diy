# BS Ancient Compatibility

## Supported Game Builds

| Game build | Reference assemblies | RitsuLib package | Implementation |
| --- | --- | --- | --- |
| Previous stable `0.107.1` | `BSchneppe.Sts2.ReferenceAssemblies` `0.107.1` | `STS2.RitsuLib.Compat.0.107.1` `0.6.11` | `compat/0.107.1/bs_ancient.impl.dll` |
| Stable API target `0.111.0` | `BSchneppe.Sts2.ReferenceAssemblies` `0.111.0-beta-41cef1ea` | `STS2.RitsuLib.Compat.0.111.0` `0.6.11` | `compat/0.111.0/bs_ancient.impl.dll` |
| Beta API target `0.113.0` | Installed game `0.113.0`, commit `16ac0fe3` | `STS2.RitsuLib` `0.6.11` | `compat/0.113.0/bs_ancient.impl.dll` |

The `bs_ancient.Beta` project name and `BS_BETA_1110` compilation symbol are
retained for the existing 0.111 API implementation. Version 0.111 has now moved
to the game's stable branch. Its compilation references are still the publicly
available 0.111 beta snapshot; this is not a runtime verification of the new
stable executable.

The root `bs_ancient.dll` is a small loader. At startup it reads the game's
`release_info.json`, selects the matching implementation, and invokes that
implementation's entry point. The PCK, assets, localization, and user config
are shared, while code compiled against incompatible game APIs remains in
separate assemblies.

The loader selects the 0.107 implementation for builds below `0.111.0`,
the 0.111 implementation for builds below `0.113.0`, and the 0.113 implementation
for `0.113.x`. Builds at `0.114.0` or newer produce an explicit version error.

## 0.113 Implementation

RitsuLib 0.6.11's unqualified `STS2.RitsuLib` package now targets game 0.113.
It must not replace the `Compat.0.111.0` package in the existing implementation.
Game 0.113 changes damage-command signatures, relic eligibility, death callbacks,
maximum-HP commands, relic rarity rolls, Ancient option creation, and creature UI.
The new `bs_ancient.Beta113` project links the same gameplay sources, defines
`BS_BETA_1130` plus the shared 0.111 API symbol, and compiles against the 0.113
game assembly. API differences stay in conditional compatibility code.

On 2026-11-13, the public game reference package only supplied builds through
`0.111.0-beta-41cef1ea`, and RitsuLib's `BAKAOLC/STS2-API-Signatures` repository
was inaccessible. The installed 0.113 API is therefore retained locally in
`archive/game-api/0.113.0`, including `release_info.json` and the required DLLs
under `data_sts2_windows_x86_64/`. This directory is excluded from Git and PCK
exports. It allows ordinary builds after switching the installed game branch.
On another development machine, populate it from a 0.113 installation, or pass
`-p:Sts2Api113Root=<0.113 installation directory>`. Refresh the cached DLLs and
release metadata together when intentionally updating the 0.113 API baseline.

`ModRelicTemplate113` routes existing eligibility overrides into the native
`IsAllowedInternal` hook. Native callers still enter through `RelicModel.IsAllowed`,
preserving the game's outer mode restrictions. Maximum-HP penalties now pass a
null card source instead of the old `isFromCard: false` argument, and relic
rolls in 0.113 use their event's explicit RNG instead of perturbing future
combat rewards. Gentle Gift, Queen of Hearts, the Playing Card Gardeners and
Neow's replacement selection also use the event RNG to avoid consuming a
shared stream in different player callback orders.

## Package Layout

```text
bs_ancient/
  bs_ancient.dll
  bs_ancient.pck
  bs_ancient.json
  compat/
    0.107.1/
      bs_ancient.impl.dll
    0.111.0/
      bs_ancient.impl.dll
    0.113.0/
      bs_ancient.impl.dll
```

RitsuLib `0.6.11` uses a loader plus shared and version-specific modules. Players
must install the complete RitsuLib Workshop package, including `shared/`,
`compat/`, `assets.zip`, and the module manifest. Copying only the root DLL is
insufficient. Its loader selects the matching API implementation.

## Build And Verification

Compile all three implementations and the loader without exporting the PCK:

```powershell
dotnet build bs_ancient.csproj --no-restore
```

Export the PCK after a successful build:

```powershell
dotnet build bs_ancient.csproj --no-restore -t:ExportPck
```

Run stable localization and loader tests:

```powershell
dotnet test tests/bs_ancient.Tests/bs_ancient.Tests.csproj --no-restore
```

Run 0.111 API target-resolution tests with a matching installed runtime:

```powershell
dotnet test tests/bs_ancient.Beta.Tests/bs_ancient.Beta.Tests.csproj --no-restore
```

Run the 0.113 suite while the installed game is 0.113:

```powershell
dotnet test tests/bs_ancient.Beta113.Tests/bs_ancient.Beta113.Tests.csproj --no-restore
```

The suites verify Ritsu patch targets, Harmony targets, dynamic target methods,
model state and localization. The 0.113 suite also checks patch argument names
and return types, the changed eligibility slot, and death callback signatures.
Tests must use a matching runtime. For the legacy 0.107 suite, an archived
runtime can be selected with `-p:Sts2DataDir=<runtime directory>`.
Do not run old implementation suites against the installed 0.113 runtime and
interpret resulting missing-method failures as legacy compatibility regressions.
These tests do not replace in-game testing of asynchronous action order,
save/load, UI node lifetime, or multiplayer synchronization.

## RitsuLib 0.6.11 Dependency Update

All implementation projects and their test projects use RitsuLib `0.6.11`,
with each project selecting the package for its own game API version.
The stable and beta game reference versions remain unchanged. This update
includes the 0.6.8 fixes for cloned-card free-play bindings, in-memory data
initialization, capability persistence registration, key-binding capture,
badges, and telemetry anonymization. Version 0.6.9 fixes BaseLib load-order
compatibility for Ancient dialogue indices, framework detection, settings
page discovery, and combat reward restoration. Version 0.6.10 fixes startup
hangs and crashes with some mod combinations, settings registration reliability,
debug-tool overhead, and unresponsive main-menu buttons. Version 0.6.11 adds
initialization-order fixes for Ancient portraits and content pools, per-run
hook model instances, and framework compatibility with game 0.113. The latter
requires a separately compiled implementation for this mod, now provided.

No additional gameplay patches are introduced by this dependency update.
Regression tests cover the split runtime/shared/UI/settings modules, facade
type forwarding, and representative fixed content IDs. The mod keeps its
existing `bs_ancient` registration ID. Retest Ancient dialogue with modded
characters and BaseLib installed, settings pages, cloned free-play cards,
and combat reward save/load in game; automated tests cannot validate these
UI and runtime interactions completely.

The legacy `RitsuModSettingsButtonCompatPatch` was removed. RitsuLib now creates
its own native selection reticle before the settings button is ready, so the
mod no longer patches `NSettingsButton.ConnectSignals` for this workaround.
Retest the settings entry, keyboard/controller focus, and custom outline color
editor in game after upgrading.

## High-Risk Compatibility Areas

Gameplay configuration uses RitsuLib's public `RunSavedDataStore` and lobby
staging/preparing lifecycle events. Only the host stages the run settings;
Ritsu carries the snapshot in the vanilla start-run message trailer and saves
it with the run. No configuration Modifier or custom packet patch is added.
Client-local settings remain separate. Snapshot serialization, repeated commits,
and local-file isolation are tested, but actual two-client networking still
requires a manual regression test.

Suspicious Hat owns an independently serialized relic proxy. UI and save getters
never create or switch proxies. Inventory command completion, full player restore,
and gameplay hooks refresh the target. Nested relic acquisitions defer this refresh
until that player's pickup initialization finishes. New targets snapshot the source's
saved state without replaying pickup effects; subsequent combat counters belong to
the hat. Permanent Girya training and a spent Maw Bank follow the source's persistent
restrictions. Invalid legacy training, pet health, and bank snapshots are repaired.
Saved identity includes the target ID, acquisition floor, inventory index, and owner
ID; legacy saves without an owner ID remain supported. Shopping, potion-use, and rest
site hooks are forwarded, but this is not complete coverage of every vanilla relic.
Test Pen Nib, trained Girya, Wuppy, Maw Bank after shopping, save/continue, and target
replacement/removal in game, including two clients with different UI interactions.
Ordinary .NET tests cannot initialize Godot's native relic UI or verify real networking.

The following systems still depend on private fields, private methods, IL
shape, or runtime reflection and should be retested after every game update:

- Helmsman's Page enemy move interception and dialogue playback.
- Mod Ancient room filtering and shared Ancient subsets.
- Hidden event option state transitions.
- Croquet Mallet card playability checks.
- Boojum run-history metadata and save-file handling.
- Butcher's Math Class command-value patches.

Prefer public game APIs and RitsuLib helpers when replacements become
available. Keep compatibility shims inside `Scripts/Compatibility` so stable
and beta differences do not spread through gameplay classes.

For detailed runtime risks, fallback behavior, and the manual release checklist,
see [Maintenance Risks](MAINTENANCE_RISKS.md).
