from __future__ import annotations

from pathlib import Path

CANONICAL_GAME_ID = "pokemon-black"
CANONICAL_GAME_LABEL = "Pokemon Black Workspace Baseline"
CANONICAL_ROM_FILENAME = "pokeblack.nds"
CANONICAL_ROM_SHA1 = "a68b3bedf5c1e53556e41e59cdf396c20b331896"
CANONICAL_ROM_SIZE = 268435456
SCHEMA_VERSION = 1

EXPORT_TOP_LEVEL_DIRS = ("raw", "normalized", "manifests", "logs")
MANIFEST_RELATIVE_PATH = Path("manifests/manifest.json")
PLACEHOLDER_CONTRACT_RELATIVE_PATH = Path("normalized/metadata/contract-placeholder.json")


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def roms_dir() -> Path:
    return repo_root() / "ROMs"


def canonical_rom_path() -> Path:
    return roms_dir() / CANONICAL_ROM_FILENAME


def export_root() -> Path:
    return repo_root() / "External" / "Exports" / "BlackWhite" / "M0"


def manifest_path(root: Path | None = None) -> Path:
    resolved_root = export_root() if root is None else root.resolve()
    return resolved_root / MANIFEST_RELATIVE_PATH


def placeholder_contract_path(root: Path | None = None) -> Path:
    resolved_root = export_root() if root is None else root.resolve()
    return resolved_root / PLACEHOLDER_CONTRACT_RELATIVE_PATH


def export_subdir(root: Path, name: str) -> Path:
    if name not in EXPORT_TOP_LEVEL_DIRS:
        raise ValueError(f"Unsupported export subdirectory '{name}'.")
    return root / name


def relative_to_repo(path: Path) -> str:
    return path.resolve().relative_to(repo_root()).as_posix()


def normalized_output_entry(path: Path, sha1: str) -> dict[str, str]:
    return {
        "path": relative_to_repo(path),
        "hash": sha1,
    }
