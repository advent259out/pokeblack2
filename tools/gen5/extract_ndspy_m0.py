from __future__ import annotations

import argparse
import hashlib
import importlib
import json
from pathlib import Path
from typing import Iterable, Sequence

from .contract import canonical_rom_path, relative_to_repo
from .prepare_export_root import prepare_export_root
from .probe_rom import probe_rom

REQUIRED_SOURCE_FILES: tuple[str, ...] = (
    "a/0/0/2",
    "a/0/0/3",
    "a/0/0/8",
    "a/0/1/2",
    "a/0/1/4",
    "a/0/1/8",
    "a/0/2/0",
    "a/0/5/7",
    "a/0/9/2",
    "a/0/9/3",
    "a/1/2/6",
    "a/1/7/8",
    "a/1/9/4",
)


class MissingDependencyError(RuntimeError):
    """Raised when an optional local dependency such as ndspy is unavailable."""


def load_ndspy_rom_class():
    try:
        module = importlib.import_module("ndspy.rom")
    except ModuleNotFoundError as exc:
        raise MissingDependencyError(
            "ndspy is not installed. Install it with 'python -m pip install ndspy' before running the extractor."
        ) from exc

    return module.NintendoDSRom


def sha1_bytes(payload: bytes) -> str:
    return hashlib.sha1(payload).hexdigest()


def serialize_output_path(path: Path) -> str:
    try:
        return relative_to_repo(path)
    except ValueError:
        return path.resolve().as_posix()


def build_file_index(rom) -> list[dict[str, object]]:
    index: list[dict[str, object]] = []
    for file_id, data in enumerate(rom.files):
        path = rom.filenames.filenameOf(file_id)
        stable_path = path if path is not None else f"__unnamed__/file-{file_id:04d}.bin"
        payload = data or b""
        index.append(
            {
                "fileId": file_id,
                "path": path,
                "stablePath": stable_path,
                "size": len(payload),
                "sha1": sha1_bytes(payload),
            }
        )
    return index


def export_named_files(rom, raw_narc_root: Path, names: Sequence[str]) -> list[dict[str, object]]:
    exported: list[dict[str, object]] = []
    for name in names:
        file_id = rom.filenames.idOf(name)
        if file_id is None:
            raise FileNotFoundError(f"ROM does not contain the required source file '{name}'.")

        data = rom.getFileByName(name)
        output_path = raw_narc_root / Path(name)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_bytes(data)
        exported.append(
            {
                "fileId": file_id,
                "path": name,
                "outputPath": serialize_output_path(output_path),
                "size": len(data),
                "sha1": sha1_bytes(data),
            }
        )
    return exported


def write_json(path: Path, payload: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def extract_ndspy_m0(root: Path, rom_path: Path | None = None) -> dict[str, str]:
    resolved_root = root.resolve()
    resolved_rom = (rom_path or canonical_rom_path()).resolve()

    # Fail fast if the ROM is missing or does not match the workspace baseline.
    rom_fingerprint = probe_rom(resolved_rom)
    prepare_export_root(resolved_root, resolved_rom)

    rom_class = load_ndspy_rom_class()
    rom = rom_class.fromFile(str(resolved_rom))

    raw_rom_root = resolved_root / "raw" / "rom"
    raw_narc_root = resolved_root / "raw" / "narc"

    file_index_path = raw_rom_root / "file-index.json"
    extracted_files_path = raw_narc_root / "required-files.json"

    file_index = build_file_index(rom)
    exported_files = export_named_files(rom, raw_narc_root, REQUIRED_SOURCE_FILES)

    write_json(
        file_index_path,
        {
            "rom": rom_fingerprint,
            "fileCount": len(file_index),
            "entries": file_index,
        },
    )
    write_json(
        extracted_files_path,
        {
            "rom": rom_fingerprint,
            "requiredFiles": list(REQUIRED_SOURCE_FILES),
            "exported": exported_files,
        },
    )

    return {
        "fileIndex": serialize_output_path(file_index_path),
        "requiredFiles": serialize_output_path(extracted_files_path),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Extract the first M0 raw source set from the canonical Pokemon Black ROM.")
    parser.add_argument("--root", default="External/Exports/BlackWhite/M0", help="Export root directory.")
    parser.add_argument("--rom", default=str(canonical_rom_path()), help="ROM file to validate and extract from.")
    args = parser.parse_args()

    result = extract_ndspy_m0(Path(args.root), Path(args.rom))
    print(json.dumps(result, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
