# Necrobinder: Partial Rework

A standalone Slay the Spire 2 mod focused on Doom, self-inflicted Doom, and Osty. Changes to existing cards replace their effects rather than adding duplicate cards.

## Content

- Reworks Shroud and Negative Pulse.
- Increases Doom applied by vanilla cards and their ongoing effects to 150% of the original amount. The applier also receives one third of the original amount as Doom. Both values are rounded down.
- Neurosurge, Death, and Underworld, including their ongoing powers, keep their vanilla Doom behavior: no amplification and no additional self-inflicted Doom.
- Area cards apply this additional self-inflicted Doom once per play, not once per enemy. This additional Doom is not amplified again.
- Adds ten cards: Death and Life's Pact, Negative Energy Absorption, Together Unto Death, Endless Hatred, Blood Oath, Dark Ritual, Poisoned Draught, Borrowed Body, Soul Phantom, and Shared Vengeance. Soul Phantom replaces Haunt in the normal card pool.
- Together Unto Death costs 0 Energy, has Retain, and does not Exhaust. When upgraded, it also prevents death from Doom for the rest of the turn, without preventing damage or death from other sources.
- Includes Chinese and English localization.

Supports game versions 0.111.x and 0.113.x, and requires STS2 RitsuLib 0.6.15 or later. The 0.111 implementation is compiled against the public API snapshot and still requires playtesting with a matching game installation. It does not require Black Souls Ancients. Some new artwork uses AI-generated assets; review drafts are not included in the release files.

## Build

Shared Vengeance is a 2-Energy Common Attack: deal 6 (8) damage, then Osty deals 9 (12) damage to the same target, using each attacker's own damage modifiers. Your attack still works without Osty. The second attack is skipped if the first kills the target or ends combat. Whenever your Osty dies, copies in your combat piles cost 1 less Energy for this combat, to a minimum of 0. Other players' Osty deaths and prevented deaths do not count. Its custom portrait shows the Necrobinder wielding her scythe alongside Osty's attack.

Soul Phantom replaces Haunt in the normal card pool. It is a 0-Energy Uncommon Power: after a Soul finishes drawing cards, you may give one non-Ethereal card in your draw pile Ethereal, or skip. The upgraded version also upgrades your current and future Souls for this combat, without automatically giving them Ethereal. Additional plays increase the number of cards you may choose; playing a normal copy never removes the upgraded effect. Only combat copies change. It reuses Haunt's portrait and power icon. Haunt remains registered for existing saves, but no longer appears in normal rewards, shops, or transformations.

- `dotnet build`: builds the DLL and synchronizes it, the manifest, both READMEs, and any existing local PCK to the game's `mods/sts2diy` directory and this project's `upload/content`. It does not rebuild the PCK.
- `dotnet build -t:ExportPck`: builds the DLL, exports a new PCK with headless Godot, and synchronizes both destinations. Use this after changing localization or artwork.
- The root loader automatically selects a separate implementation for game 0.111 or 0.113. Both share the same PCK, assets, and localization.
- Test builds and Godot's internal publish do not deploy intermediate assemblies. Player configuration, `docs`, and review art are excluded from the upload. Building never publishes to Steam Workshop.
- PCK export has a 60-second timeout. A failed export does not proceed to copy its output. NuGet must be able to restore the runtime packs required by Godot's internal .NET publish.

Shroud is a 1-Energy Uncommon Power: every 4 Doom applications grant 1 Energy, or 2 when upgraded. Its cost and trigger interval do not change on upgrade. Additional plays stack the Energy reward and share the same progress counter.

Negative Pulse grants 7 (9) Block and applies 16 (22) Doom to all enemies and 3 (5) Doom to yourself. It does not apply Doom to allies or Osty. The self-inflicted Doom occurs once per play, regardless of enemy count.

While Endless Hatred's damage bonus is active, Osty displays the original Rage icon and the current bonus percentage. The indicator is removed at turn end and does not apply the damage bonus a second time.

Blood Oath is an Uncommon Skill costing 1 (0) Energy: lose HP equal to your Doom and gain that much Block. It neither consumes Doom nor reduces maximum HP. Actual HP lost across multiple plays is repaid at the end of combat, up to maximum HP. The HP loss can still be lethal; this effect does not revive you. A saved status tracks the pending recovery. It has its own card portrait and a heart-and-return-arrow status icon.

Dark Ritual is a 1-Energy Uncommon Power with Ethereal: at the start of your turn, lose 1 Max HP and Summon 8 (10). It first triggers on your next turn. Each played copy resolves independently and pays its own Max HP cost, which is not refunded after combat. It has its own card portrait and a forbidden sacrificial seal status icon.

Poisoned Draught is a 0-Energy Common Skill with Exhaust: apply 6 (3) Doom to yourself and draw 2 cards. As a new card, it is not subject to the vanilla Doom amplification or backlash rules. It uses custom card art.

Blight Strike now deals 9 (12) base damage plus half your current Doom, rounded down, as a single hit. The native damage preview includes this bonus; Strength, Weak, and other normal modifiers still apply. It no longer inherently applies Doom or backlash, and does not consume your Doom.

Borrowed Body is a 0-Energy Uncommon Skill with Exhaust; upgrading adds Retain. It can only be played when your Doom is at least your current HP, and immediately transfers all your Doom to all enemies, then grants 1 Intangible. Each enemy receives the full amount without the vanilla amplification or backlash rules. It does not take Doom from allies or Osty, and autoplay cannot bypass its condition. It uses custom card art.

Negative Energy Absorption is a 1-Energy Common Skill: choose an enemy, transfer half its Doom to yourself, and gain that much Block, rounding down. Other enemies are unaffected. It has Exhaust; upgrading removes Exhaust.

Reaper Form retains the modified Doom and backlash rules, and also prevents its owner from dying to Doom while the power is active. It does not clear Doom, protect allies, or prevent damage or death from other sources.

## Tests

Run `dotnet test tests/sts2diy.Tests/sts2diy.Tests.csproj`. These checks do not replace in-game combat, multiplayer, or save/load testing.
