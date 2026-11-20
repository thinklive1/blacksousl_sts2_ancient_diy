# Necrobinder: Partial Rework

A standalone Slay the Spire 2 mod focused on Doom, self-inflicted Doom, and Osty. Changes to existing cards replace their effects rather than adding duplicate cards.

## Content

- Reworks Shroud and Negative Pulse.
- Increases Doom applied by vanilla cards and their ongoing effects to 150% of the original amount. The applier also receives one third of the original amount as Doom. Both values are rounded down.
- Area cards apply this additional self-inflicted Doom once per play, not once per enemy. This additional Doom is not amplified again.
- Adds Death and Life's Pact, Negative Energy Absorption, Together Unto Death, and Endless Hatred.
- Together Unto Death costs 0 Energy, has Retain and Exhaust, and loses Exhaust when upgraded.
- Includes Chinese and English localization.

Requires game version 0.113 and STS2 RitsuLib 0.6.11 or later. It does not require Black Souls Ancients. Some new artwork uses AI-generated assets; review drafts are not included in the release files.

## Build

- `dotnet build`: builds the DLL and synchronizes it, the manifest, both READMEs, and any existing local PCK to the game's `mods/sts2diy` directory and this project's `upload/content`. It does not rebuild the PCK.
- `dotnet build -t:ExportPck`: builds the DLL, exports a new PCK with headless Godot, and synchronizes both destinations. Use this after changing localization or artwork.
- This project targets the installed 0.113 game API; it does not use the older-version compatibility loader from `bs_ancient`.
- Test builds and Godot's internal publish do not deploy intermediate assemblies. Player configuration, `docs`, and review art are excluded from the upload. Building never publishes to Steam Workshop.
- PCK export has a 60-second timeout. A failed export does not proceed to copy its output. NuGet must be able to restore the runtime packs required by Godot's internal .NET publish.

## Tests

Run `dotnet test tests/sts2diy.Tests/sts2diy.Tests.csproj`. These checks do not replace in-game combat, multiplayer, or save/load testing.
