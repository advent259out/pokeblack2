# PokeBlack2 Foundation

`pokeblack2` is a new Unity 6 foundation project for a near-1:1 recreation of Pokemon Black on top of a strict offline content pipeline.

## Core Rules

- The legal ROM dump lives in `ROMs/` and is treated as an input artifact only.
- Project-owned content must flow through `ROM -> raw exports -> normalized contract -> Unity Editor validation/import seam`.
- Runtime code must not read from `ROMs/` or `External/Exports/`.
- This phase builds only the project skeleton, contracts, and tests. It does not include playable content.

## Canonical ROM

- Title: `Pokemon Black workspace baseline`
- Preferred filename: `ROMs/pokeblack.nds`
- Expected SHA1: `a68b3bedf5c1e53556e41e59cdf396c20b331896`

## Repository Layout

- `Assets/`: Unity runtime, editor, and test code
- `Assets/Generated/`: local imported Unity assets derived from normalized contracts
- `Packages/`: Unity package manifest
- `ProjectSettings/`: Unity version pinning
- `ROMs/`: legal ROM input directory
- `External/Exports/BlackWhite/M0/`: unversioned raw and normalized exports
- `tools/gen5/`: Python contract, ROM probe, and export-root scaffolding tools
- `docs/architecture/`: written architecture and boundary docs

## Bootstrap Commands

```powershell
python -m tools.gen5.probe_rom --rom ROMs/pokeblack.nds
python -m tools.gen5.prepare_export_root --root External/Exports/BlackWhite/M0
python -m tools.gen5.extract_ndspy_m0 --rom ROMs/pokeblack.nds --root External/Exports/BlackWhite/M0
python -m tools.gen5.normalize_m0 --root External/Exports/BlackWhite/M0
"C:/Program Files/Unity/Hub/Editor/6000.4.0f1/Editor/Unity.exe" -batchmode -nographics -projectPath . -runTests -runSynchronously -testPlatform EditMode -testFilter PokeBlack2.Foundation.Editor.BlackWhiteFoundationSmokeTests -testResults Temp/BlackWhiteFoundationSmokeTests.xml -logFile Temp/BlackWhiteFoundationSmokeTests.log
.\tools\run_phase1_acceptance.ps1
```

Do not add `-quit` to the Unity test command. In Unity `6000.4.0f1`, the command-line test runner exits on its own after the run completes, and forcing `-quit` prevents the test run from starting.

Imported Unity assets generated from normalized contracts are written under `Assets/Generated/Resources/` so runtime loading stays inside Unity assets while the derived content remains local and unversioned.

## Current Scope

This repository intentionally does not copy implementation code or content from `E:\youxi\pokeblack`. That project may be consulted as research, but this foundation phase establishes a clean boundary-first baseline inside `pokeblack2`.
