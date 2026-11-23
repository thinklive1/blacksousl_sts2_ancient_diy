# Necrobinder: Partial Rework

A standalone Slay the Spire 2 mod focused on Doom, self-inflicted Doom, and Osty. Changes to existing cards replace their effects rather than adding duplicate cards.

## Content

- Reworks Shroud and Negative Pulse.
- Increases Doom applied by vanilla cards and their ongoing effects to 150% of the original amount. The applier also receives one third of the original amount as Doom. Both values are rounded down.
- Area cards apply this additional self-inflicted Doom once per play, not once per enemy. This additional Doom is not amplified again.
- Adds Death and Life's Pact, Negative Energy Absorption, Together Unto Death, and Endless Hatred.
- Together Unto Death costs 0 Energy, has Retain, and does not Exhaust. When upgraded, it also prevents death from Doom for the rest of the turn, without preventing damage or death from other sources.
- Includes Chinese and English localization.

Supports game versions 0.111.x and 0.113.x, and requires STS2 RitsuLib 0.6.15 or later. The 0.111 implementation is compiled against the public API snapshot and still requires playtesting with a matching game installation. It does not require Black Souls Ancients. Some new artwork uses AI-generated assets; review drafts are not included in the release files.

## Build

- `dotnet build`: builds the DLL and synchronizes it, the manifest, both READMEs, and any existing local PCK to the game's `mods/sts2diy` directory and this project's `upload/content`. It does not rebuild the PCK.
- `dotnet build -t:ExportPck`: builds the DLL, exports a new PCK with headless Godot, and synchronizes both destinations. Use this after changing localization or artwork.
- The root loader automatically selects a separate implementation for game 0.111 or 0.113. Both share the same PCK, assets, and localization.
- Test builds and Godot's internal publish do not deploy intermediate assemblies. Player configuration, `docs`, and review art are excluded from the upload. Building never publishes to Steam Workshop.
- PCK export has a 60-second timeout. A failed export does not proceed to copy its output. NuGet must be able to restore the runtime packs required by Godot's internal .NET publish.

Shroud is a 1-Energy Uncommon Power: every 4 Doom applications grant 1 Energy, or 2 when upgraded. Its cost and trigger interval do not change on upgrade. Additional plays stack the Energy reward and share the same progress counter.

Negative Pulse grants 7 (9) Block and applies 16 (22) Doom to all enemies and 3 (5) Doom to yourself. It does not apply Doom to allies or Osty. The self-inflicted Doom occurs once per play, regardless of enemy count.

While Endless Hatred's damage bonus is active, Osty displays the original Rage icon and the current bonus percentage. The indicator is removed at turn end and does not apply the damage bonus a second time.

## Tests

Run `dotnet test tests/sts2diy.Tests/sts2diy.Tests.csproj`. These checks do not replace in-game combat, multiplayer, or save/load testing.
