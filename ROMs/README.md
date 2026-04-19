# ROM Input Folder

Put your legally dumped Pokemon Black ROM in this folder.

Recommended filename:
- `pokeblack.nds`

Recommended workflow:
1. Keep the original dump unchanged.
2. Use `python -m tools.gen5.probe_rom --rom ROMs/pokeblack.nds` to verify the dump matches the canonical baseline.
3. Extract working files into `E:\youxi\pokeblack2\External\Exports\BlackWhite\M0`.
4. Treat the ROM as an input artifact, not as something the Unity project edits directly.
5. Use `python -m tools.gen5.extract_ndspy_m0 --rom ROMs/pokeblack.nds --root External/Exports/BlackWhite/M0` to generate the first raw Gen5 source set.
6. Use `python -m tools.gen5.normalize_m0 --root External/Exports/BlackWhite/M0` to turn the raw source set into importer-facing normalized summaries.

Notes:
- This project does not ship with a ROM.
- This project should not download ROMs.
- The canonical baseline for this foundation phase is the current workspace ROM baseline with SHA1 `a68b3bedf5c1e53556e41e59cdf396c20b331896`.
- Dumps that do not match that SHA1 should be rejected by the tooling instead of being silently accepted.
