from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from .contract import (
    EXPORT_TOP_LEVEL_DIRS,
    canonical_rom_path,
    manifest_path,
    normalized_output_entry,
    placeholder_contract_path,
)
from .manifest_schema import build_manifest
from .probe_rom import probe_rom


def _sha1_for_text(payload: str) -> str:
    return hashlib.sha1(payload.encode("utf-8")).hexdigest()


def _ensure_export_layout(root: Path) -> None:
    root.mkdir(parents=True, exist_ok=True)

    for child in root.iterdir():
        if child.is_dir() and child.name not in EXPORT_TOP_LEVEL_DIRS:
            raise ValueError(
                f"Unexpected export-root directory '{child.name}'. Only {', '.join(EXPORT_TOP_LEVEL_DIRS)} are allowed."
            )

    for name in EXPORT_TOP_LEVEL_DIRS:
        (root / name).mkdir(parents=True, exist_ok=True)


def _write_placeholder_contract(root: Path) -> Path:
    contract_path = placeholder_contract_path(root)
    contract_path.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        "schemaVersion": 1,
        "kind": "foundation-placeholder",
        "description": "This file reserves the normalized contract seam for future Gen5 importers.",
        "outputs": [],
    }
    contract_path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return contract_path


def prepare_export_root(root: Path, rom_path: Path | None = None) -> Path:
    resolved_root = root.resolve()
    rom_fingerprint = probe_rom((rom_path or canonical_rom_path()).resolve())
    _ensure_export_layout(resolved_root)
    placeholder_path = _write_placeholder_contract(resolved_root)

    placeholder_sha1 = _sha1_for_text(placeholder_path.read_text(encoding="utf-8"))
    normalized_outputs = [normalized_output_entry(placeholder_path, placeholder_sha1)]
    manifest = build_manifest(rom=rom_fingerprint, export_root=resolved_root, normalized_outputs=normalized_outputs)

    output_path = manifest_path(resolved_root)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return output_path


def main() -> int:
    parser = argparse.ArgumentParser(description="Create the M0 export-root scaffold and manifest placeholder.")
    parser.add_argument("--root", required=True, help="Export root directory to initialize.")
    args = parser.parse_args()

    manifest_output = prepare_export_root(Path(args.root))
    print(manifest_output.as_posix())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
