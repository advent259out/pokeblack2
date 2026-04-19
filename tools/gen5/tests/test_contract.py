from __future__ import annotations

import unittest
from pathlib import Path

from tools.gen5 import contract
from tools.gen5.manifest_schema import ManifestValidationError, build_manifest, validate_manifest


class ContractTests(unittest.TestCase):
    def test_repo_root_resolves_current_workspace(self) -> None:
        self.assertEqual(contract.repo_root(), Path(__file__).resolve().parents[3])

    def test_canonical_rom_points_to_workspace_roms_directory(self) -> None:
        self.assertEqual(
            contract.canonical_rom_path(),
            contract.repo_root() / "ROMs" / contract.CANONICAL_ROM_FILENAME,
        )

    def test_build_manifest_contains_required_fields(self) -> None:
        normalized_outputs = [{"path": "Assets/Editor/Gen5/TestData/EmptyM0Fixture/normalized/metadata/contract.json", "hash": "abc123"}]
        manifest = build_manifest(
            rom={"filename": "pokeblack.nds", "sha1": contract.CANONICAL_ROM_SHA1, "size": contract.CANONICAL_ROM_SIZE},
            export_root=contract.export_root(),
            normalized_outputs=normalized_outputs,
        )

        self.assertEqual(manifest["schemaVersion"], contract.SCHEMA_VERSION)
        self.assertEqual(manifest["game"], contract.CANONICAL_GAME_ID)
        self.assertEqual(manifest["hashes"]["Assets/Editor/Gen5/TestData/EmptyM0Fixture/normalized/metadata/contract.json"], "abc123")

    def test_validate_manifest_rejects_missing_hash_mapping(self) -> None:
        manifest = {
            "schemaVersion": contract.SCHEMA_VERSION,
            "game": contract.CANONICAL_GAME_ID,
            "rom": {"filename": "pokeblack.nds", "sha1": contract.CANONICAL_ROM_SHA1, "size": contract.CANONICAL_ROM_SIZE},
            "exportRoot": "External/Exports/BlackWhite/M0",
            "generatedAt": "2026-04-18T00:00:00Z",
            "normalizedOutputs": [{"path": "normalized/metadata/contract-placeholder.json", "hash": "deadbeef"}],
            "hashes": {},
        }

        with self.assertRaises(ManifestValidationError):
            validate_manifest(manifest)

