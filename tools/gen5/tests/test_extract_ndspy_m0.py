from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from tools.gen5.extract_ndspy_m0 import build_file_index, export_named_files, sha1_bytes


class FakeFilenames:
    def __init__(self, path_by_id: dict[int, str | None], id_by_path: dict[str, int]):
        self._path_by_id = path_by_id
        self._id_by_path = id_by_path

    def filenameOf(self, file_id: int):
        return self._path_by_id.get(file_id)

    def idOf(self, name: str):
        return self._id_by_path.get(name)


class FakeRom:
    def __init__(self):
        self.files = [b"arm9", b"text-data", b"script-data"]
        self.filenames = FakeFilenames(
            {
                0: None,
                1: "a/0/0/2",
                2: "a/0/5/7",
            },
            {
                "a/0/0/2": 1,
                "a/0/5/7": 2,
            },
        )

    def getFileByName(self, name: str) -> bytes:
        file_id = self.filenames.idOf(name)
        if file_id is None:
            raise KeyError(name)
        return self.files[file_id]


class ExtractNdspyM0Tests(unittest.TestCase):
    def test_build_file_index_includes_named_and_unnamed_entries(self) -> None:
        index = build_file_index(FakeRom())

        self.assertEqual(index[0]["path"], None)
        self.assertEqual(index[0]["stablePath"], "__unnamed__/file-0000.bin")
        self.assertEqual(index[1]["path"], "a/0/0/2")
        self.assertEqual(index[1]["size"], len(b"text-data"))
        self.assertEqual(index[2]["sha1"], sha1_bytes(b"script-data"))

    def test_export_named_files_writes_expected_layout(self) -> None:
        rom = FakeRom()
        with tempfile.TemporaryDirectory() as temp_dir:
            raw_narc_root = Path(temp_dir)
            exported = export_named_files(rom, raw_narc_root, ["a/0/0/2", "a/0/5/7"])

            self.assertEqual(exported[0]["fileId"], 1)
            self.assertTrue((raw_narc_root / "a" / "0" / "0" / "2").is_file())
            self.assertTrue((raw_narc_root / "a" / "0" / "5" / "7").is_file())
            self.assertEqual((raw_narc_root / "a" / "0" / "5" / "7").read_bytes(), b"script-data")

    def test_export_named_files_rejects_missing_required_file(self) -> None:
        rom = FakeRom()
        with tempfile.TemporaryDirectory() as temp_dir:
            with self.assertRaises(FileNotFoundError):
                export_named_files(rom, Path(temp_dir), ["a/9/9/9"])
