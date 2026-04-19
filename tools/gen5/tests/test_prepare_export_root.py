from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from tools.gen5.contract import (
    CANONICAL_ROM_SHA1,
    CANONICAL_ROM_SIZE,
    EXPORT_TOP_LEVEL_DIRS,
    manifest_path,
    placeholder_contract_path,
    repo_root,
)
from tools.gen5.prepare_export_root import prepare_export_root


class PrepareExportRootTests(unittest.TestCase):
    def test_prepare_export_root_creates_layout_and_manifest(self) -> None:
        temp_parent = repo_root() / "Temp" / "PythonUnitTests"
        temp_parent.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(dir=temp_parent) as temp_dir:
            root = Path(temp_dir) / "External" / "Exports" / "BlackWhite" / "M0"
            rom_fingerprint = {
                "game": "Pokemon Black Version 1.0 USA/EUR",
                "filename": "pokeblack.nds",
                "sha1": CANONICAL_ROM_SHA1,
                "size": CANONICAL_ROM_SIZE,
            }
            with patch("tools.gen5.prepare_export_root.probe_rom", return_value=rom_fingerprint):
                output_path = prepare_export_root(root, repo_root() / "ROMs" / "pokeblack.nds")

            self.assertEqual(output_path, manifest_path(root))
            for name in EXPORT_TOP_LEVEL_DIRS:
                self.assertTrue((root / name).is_dir(), f"missing export subdirectory {name}")
            self.assertTrue(placeholder_contract_path(root).is_file())

            manifest = json.loads(output_path.read_text(encoding="utf-8"))
            self.assertEqual(manifest["rom"]["filename"], "pokeblack.nds")
            self.assertEqual(len(manifest["normalizedOutputs"]), 1)

    def test_prepare_export_root_rejects_unexpected_top_level_directory(self) -> None:
        temp_parent = repo_root() / "Temp" / "PythonUnitTests"
        temp_parent.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(dir=temp_parent) as temp_dir:
            root = Path(temp_dir) / "External" / "Exports" / "BlackWhite" / "M0"
            (root / "unexpected").mkdir(parents=True)

            with self.assertRaises(ValueError):
                prepare_export_root(root, repo_root() / "ROMs" / "pokeblack.nds")
