# PokeBlack2 Foundation Architecture

## Intent

This foundation phase creates the long-lived seams that later Gen5 importers, runtime systems, and regression suites will use. The priority is correctness of boundaries, not visible gameplay.

## Non-Negotiable Boundaries

- `ROMs/` is the only location for legal ROM input.
- `External/Exports/BlackWhite/M0/` is the only location for derived extraction artifacts in this phase.
- Unity runtime assemblies may only depend on `Assets/Scripts/Core` and `Assets/Scripts/Gen5/Contracts`.
- Unity Editor assemblies validate manifests and contracts but do not implement map, script, battle, or save import logic yet.
- Runtime code must never read from `ROMs/` or `External/Exports/`. Any attempt through the project bootstrap APIs must fail immediately.

## Export Root Layout

The `M0` export root is fixed to:

```text
External/Exports/BlackWhite/M0/
  raw/
  normalized/
  manifests/
  logs/
```

Only those top-level directories are valid. Future phases may add files under them, but may not create new top-level export-root categories without an explicit schema update.

The first real raw extraction step writes:

- `raw/rom/file-index.json` for a full ROM file inventory
- `raw/narc/<rom-path>` for the first required `a/...` source files
- `raw/narc/required-files.json` for a machine-readable summary of the extracted source set

The first normalization step writes:

- `normalized/metadata/rom-info.json`
- `normalized/metadata/file-index.json`
- `normalized/metadata/source-catalog.json`
- `normalized/metadata/normalization-report.json`
- `normalized/<group>/index.json` for `text`, `maps`, `scripts`, `trainers`, `pokemon`, `encounters`, and `visual`

## Manifest Contract

The foundation schema version is `1`. `manifests/manifest.json` must contain:

- `schemaVersion`
- `game`
- `rom.filename`
- `rom.sha1`
- `rom.size`
- `exportRoot`
- `generatedAt`
- `normalizedOutputs`
- `hashes`

`normalizedOutputs` is intentionally business-agnostic in this phase. It stores only file paths and hashes so later importers can adopt the seam without changing the manifest shape.

## Runtime Foundation

The runtime surface is intentionally small:

- `GameVersion`
- `GameContentProfile`
- `RuntimeContentAccessGuard`
- `FoundationBootstrap`
- Gen5 DTO/contract placeholders under `Assets/Scripts/Gen5/Contracts`

These types define the dependency direction for future world, script, battle, and save systems. They must stay free of FireRed, GBA tilemap, or ROM-reader assumptions.
