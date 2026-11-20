# Workshop Workspace

This directory belongs to the standalone mod `sts2diy` (亡灵契约师部分重做), not `bs_ancient`.

## Files

- `workshop.json`: title, bilingual description, visibility, tags, and Workshop dependencies.
- `content/`: files uploaded to subscribers. A normal build synchronizes the DLL, manifest, both READMEs, and any existing local PCK. `dotnet build -t:ExportPck` also exports and synchronizes a new PCK.
- `image.png`: add the approved Workshop cover before the first upload. Keep it below 1 MB. No placeholder cover is included.
- `mod_id.txt`: created by the uploader after the first successful publication. It is intentionally absent until then. Never copy the ID from another mod.

The draft is private. Change `visibility` to `public` when ready to publish. RitsuLib is registered as dependency `3747602295`.

## Before Publishing

1. The four approved card portraits are integrated as compressed 1000x760 JPEGs. Raiment of Doom reuses the original Shroud power icons. The other three approved power icons are integrated as transparent 512x512 PNGs; Shared Doom uses the original curved-arrow design. Verify their appearance in game.
2. Add the Workshop cover as `image.png`.
3. Run `dotnet build -t:ExportPck` from the project root and confirm the export succeeded. The build synchronizes both the local game directory and `content`. A normal `dotnet build` does not refresh changed resources inside the PCK.
4. Check that `content` contains no player configuration files, saves, logs, source files, or development documents other than the READMEs. The build removes player `.cfg` files, `*_config.json`, `settings.json`, and the `docs` subdirectory from the upload workspace, without touching settings in the local game directory.

The uploader can be reused without copying its files into this project:

```powershell
& 'D:\game\bsmod\bs_ancient\upload\ModUploader-win-x64\ModUploader.exe' upload -w 'D:\game\bsmod\sts2diy\upload'
```

The workspace argument must point here. The command above is documentation only; preparing this workspace does not execute an upload.
