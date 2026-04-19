from __future__ import annotations

import argparse
import hashlib
import json
from functools import lru_cache
from pathlib import Path

from .contract import (
    CANONICAL_GAME_LABEL,
    CANONICAL_ROM_SHA1,
    CANONICAL_ROM_SIZE,
)


class UnsupportedRomError(ValueError):
    """Raised when the provided ROM does not match the canonical baseline."""


@lru_cache(maxsize=4)
def _sha1_for_file(path_text: str) -> str:
    digest = hashlib.sha1()
    with Path(path_text).open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def probe_rom(rom_path: Path) -> dict[str, object]:
    rom_path = rom_path.resolve()
    if not rom_path.is_file():
        raise FileNotFoundError(f"ROM not found: {rom_path}")

    size = rom_path.stat().st_size
    sha1 = _sha1_for_file(str(rom_path))

    if size != CANONICAL_ROM_SIZE:
        raise UnsupportedRomError(
            f"Unsupported ROM size {size}. Expected {CANONICAL_ROM_SIZE} bytes for the canonical baseline."
        )

    if sha1 != CANONICAL_ROM_SHA1:
        raise UnsupportedRomError(
            f"Unsupported ROM SHA1 {sha1}. Expected {CANONICAL_ROM_SHA1} for the canonical baseline."
        )

    return {
        "game": CANONICAL_GAME_LABEL,
        "filename": rom_path.name,
        "sha1": sha1,
        "size": size,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate the canonical Pokemon Black ROM and print its fingerprint.")
    parser.add_argument("--rom", required=True, help="Path to the ROM file to validate.")
    args = parser.parse_args()

    result = probe_rom(Path(args.rom))
    print(json.dumps(result, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

