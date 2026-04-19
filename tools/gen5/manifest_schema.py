from __future__ import annotations

from datetime import UTC, datetime
from typing import Mapping, Sequence

from .contract import (
    CANONICAL_GAME_ID,
    SCHEMA_VERSION,
    relative_to_repo,
)


class ManifestValidationError(ValueError):
    """Raised when a manifest does not match the project-owned schema."""


def _require_string(mapping: Mapping[str, object], key: str) -> str:
    value = mapping.get(key)
    if not isinstance(value, str) or not value:
        raise ManifestValidationError(f"Manifest field '{key}' must be a non-empty string.")
    return value


def _require_int(mapping: Mapping[str, object], key: str) -> int:
    value = mapping.get(key)
    if not isinstance(value, int) or value < 0:
        raise ManifestValidationError(f"Manifest field '{key}' must be a non-negative integer.")
    return value


def build_manifest(
    *,
    rom: Mapping[str, object],
    export_root,
    normalized_outputs: Sequence[Mapping[str, str]],
    generated_at: datetime | None = None,
) -> dict[str, object]:
    timestamp = (generated_at or datetime.now(UTC)).replace(microsecond=0).isoformat().replace("+00:00", "Z")
    hashes = {entry["path"]: entry["hash"] for entry in normalized_outputs}
    manifest = {
        "schemaVersion": SCHEMA_VERSION,
        "game": CANONICAL_GAME_ID,
        "rom": {
            "filename": rom["filename"],
            "sha1": rom["sha1"],
            "size": rom["size"],
        },
        "exportRoot": relative_to_repo(export_root),
        "generatedAt": timestamp,
        "normalizedOutputs": list(normalized_outputs),
        "hashes": hashes,
    }
    validate_manifest(manifest)
    return manifest


def validate_manifest(manifest: Mapping[str, object]) -> None:
    if manifest.get("schemaVersion") != SCHEMA_VERSION:
        raise ManifestValidationError(
            f"Manifest schemaVersion must be {SCHEMA_VERSION}, got {manifest.get('schemaVersion')!r}."
        )

    if manifest.get("game") != CANONICAL_GAME_ID:
        raise ManifestValidationError(f"Manifest game must be '{CANONICAL_GAME_ID}'.")

    rom = manifest.get("rom")
    if not isinstance(rom, Mapping):
        raise ManifestValidationError("Manifest field 'rom' must be an object.")

    _require_string(rom, "filename")
    _require_string(rom, "sha1")
    _require_int(rom, "size")
    _require_string(manifest, "exportRoot")
    _require_string(manifest, "generatedAt")

    normalized_outputs = manifest.get("normalizedOutputs")
    if not isinstance(normalized_outputs, list):
        raise ManifestValidationError("Manifest field 'normalizedOutputs' must be a list.")

    hashes = manifest.get("hashes")
    if not isinstance(hashes, Mapping):
        raise ManifestValidationError("Manifest field 'hashes' must be an object.")

    for entry in normalized_outputs:
        if not isinstance(entry, Mapping):
            raise ManifestValidationError("Each normalized output entry must be an object.")
        path = _require_string(entry, "path")
        digest = _require_string(entry, "hash")
        if hashes.get(path) != digest:
            raise ManifestValidationError(f"Manifest hashes map is missing the digest for '{path}'.")

