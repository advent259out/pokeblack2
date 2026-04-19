from __future__ import annotations

import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from tools.gen5.contract import CANONICAL_ROM_SHA1, CANONICAL_ROM_SIZE, canonical_rom_path
from tools.gen5.probe_rom import UnsupportedRomError, probe_rom


class ProbeRomTests(unittest.TestCase):
    def test_probe_rom_returns_expected_fingerprint_for_canonical_rom(self) -> None:
        with patch("tools.gen5.probe_rom._sha1_for_file", return_value=CANONICAL_ROM_SHA1):
            result = probe_rom(canonical_rom_path())

        self.assertEqual(result["filename"], "pokeblack.nds")
        self.assertEqual(result["sha1"], CANONICAL_ROM_SHA1)
        self.assertEqual(result["size"], CANONICAL_ROM_SIZE)

    def test_probe_rom_rejects_non_baseline_file(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            rom_path = Path(temp_dir) / "fake.nds"
            rom_path.write_bytes(b"not a valid baseline rom")

            with self.assertRaises(UnsupportedRomError):
                probe_rom(rom_path)
