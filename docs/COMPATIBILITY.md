# BS Ancient Compatibility

## Supported Game Builds

| Game build | Reference assemblies | RitsuLib package | Implementation |
| --- | --- | --- | --- |
| Stable `0.107.1` | `BSchneppe.Sts2.ReferenceAssemblies` `0.107.1` | `STS2.RitsuLib.Compat.0.107.1` `0.6.9` | `compat/0.107.1/bs_ancient.impl.dll` |
| Beta `0.111.0` (`41cef1ea`) | `BSchneppe.Sts2.ReferenceAssemblies` `0.111.0-beta-41cef1ea` | `STS2.RitsuLib` `0.6.9` | `compat/0.111.0/bs_ancient.impl.dll` |

The root `bs_ancient.dll` is a small loader. At startup it reads the game's
`release_info.json`, selects the matching implementation, and invokes that
implementation's entry point. The PCK, assets, localization, and user config
are shared, while code compiled against incompatible game APIs remains in
separate assemblies.

The loader selects the stable implementation for builds below `0.111.0` and
the beta implementation for `0.111.0` or newer builds. Loading a future beta
through the current beta implementation is best effort, not guaranteed forward
compatibility. If the game API changes again, add and publish another compiled
implementation instead of adding more runtime reflection to shared gameplay
code.

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
```

RitsuLib `0.6.9` uses a loader plus shared and version-specific modules. Players
must install the complete RitsuLib Workshop package, including `shared/`,
`compat/`, `assets.zip`, and the module manifest. Copying only the root DLL is
insufficient. Its loader selects the matching API implementation.

## Build And Verification

Compile both implementations and the loader without exporting the PCK:

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

Run beta API target-resolution tests:

```powershell
dotnet test tests/bs_ancient.Beta.Tests/bs_ancient.Beta.Tests.csproj --no-restore
```

Both test suites verify that Ritsu patch targets, Harmony targets, and dynamic
target methods resolve against the locally installed game assembly. The beta
implementation is compiled against beta reference assemblies, but running its
tests while the stable game is installed does not validate the beta runtime.
Switch the installed game to the intended branch before runtime verification.
These tests do not replace in-game testing of asynchronous action order,
save/load, UI node lifetime, or multiplayer synchronization.

## RitsuLib 0.6.9 Dependency Update

Both implementation projects and their test projects use RitsuLib `0.6.9`.
The stable and beta game reference versions remain unchanged. This update
includes the 0.6.8 fixes for cloned-card free-play bindings, in-memory data
initialization, capability persistence registration, key-binding capture,
badges, and telemetry anonymization. Version 0.6.9 fixes BaseLib load-order
compatibility for Ancient dialogue indices, framework detection, settings
page discovery, and combat reward restoration.

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
