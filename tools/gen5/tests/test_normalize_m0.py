from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

import ndspy.narc

from tools.gen5.contract import repo_root
from tools.gen5.normalize_m0 import GROUP_SPECS, normalize_m0, resolve_output_path, summarize_narc_bytes
from tools.gen5.text_decoder import encode_text_bank
from tools.gen5.tests.test_map_decoder import (
    build_map_container_member,
    build_map_metadata_candidate_member,
    build_permission_grid_section,
    build_map_side_lookup_member,
    build_zone_header_payload,
)


class NormalizeM0Tests(unittest.TestCase):
    def test_summarize_narc_bytes_reports_member_hashes(self) -> None:
        narc = ndspy.narc.NARC.fromFilesAndNames([b"one", b"two", b"three"])
        summary = summarize_narc_bytes(ndspy.narc, narc.save())

        self.assertEqual(summary["containerType"], "narc")
        self.assertEqual(summary["memberCount"], 3)
        self.assertEqual(summary["members"][0]["index"], 0)
        self.assertEqual(summary["members"][1]["size"], 3)

    def test_resolve_output_path_accepts_repo_relative_and_absolute_paths(self) -> None:
        temp_parent = repo_root() / "Temp" / "PythonUnitTests"
        temp_parent.mkdir(parents=True, exist_ok=True)

        with tempfile.TemporaryDirectory(dir=temp_parent) as temp_dir:
            root = Path(temp_dir) / "External" / "Exports" / "BlackWhite" / "M0"
            raw_file = root / "raw" / "narc" / "a" / "0" / "0" / "2"
            raw_file.parent.mkdir(parents=True, exist_ok=True)
            raw_file.write_bytes(b"fixture")

            repo_relative = raw_file.resolve().relative_to(repo_root()).as_posix()

            self.assertEqual(resolve_output_path(root, repo_relative), raw_file)
            self.assertEqual(resolve_output_path(root, str(raw_file.resolve())), raw_file.resolve())

    def test_normalize_m0_writes_expected_outputs_and_updates_manifest(self) -> None:
        temp_parent = repo_root() / "Temp" / "PythonUnitTests"
        temp_parent.mkdir(parents=True, exist_ok=True)

        with tempfile.TemporaryDirectory(dir=temp_parent) as temp_dir:
            root = Path(temp_dir) / "External" / "Exports" / "BlackWhite" / "M0"
            (root / "raw" / "rom").mkdir(parents=True)
            (root / "raw" / "narc").mkdir(parents=True)
            (root / "normalized" / "metadata").mkdir(parents=True)
            (root / "manifests").mkdir(parents=True)
            (root / "logs").mkdir(parents=True)

            placeholder = root / "normalized" / "metadata" / "contract-placeholder.json"
            placeholder.write_text("{\"schemaVersion\":1}\n", encoding="utf-8")

            file_index_payload = {
                "rom": {
                    "filename": "pokeblack.nds",
                    "game": "Pokemon Black Workspace Baseline",
                    "sha1": "a68b3bedf5c1e53556e41e59cdf396c20b331896",
                    "size": 268435456,
                },
                "fileCount": 3,
                "entries": [
                    {"fileId": 0, "path": None, "stablePath": "__unnamed__/file-0000.bin", "size": 4, "sha1": "aaaa"},
                    {"fileId": 1, "path": "a/0/0/2", "stablePath": "a/0/0/2", "size": 4, "sha1": "bbbb"},
                    {"fileId": 2, "path": "a/0/5/7", "stablePath": "a/0/5/7", "size": 4, "sha1": "cccc"},
                ],
            }
            (root / "raw" / "rom" / "file-index.json").write_text(
                json.dumps(file_index_payload, indent=2, sort_keys=True) + "\n",
                encoding="utf-8",
            )

            exported_entries = []
            counter = 1
            for group_name, group_specs in GROUP_SPECS.items():
                for spec in group_specs:
                    if group_name == "text":
                        members = [
                            encode_text_bank([f"{spec['id']} first", f"{spec['id']} second"], flags=74),
                            encode_text_bank([f"{spec['id']} third"], flags=216),
                        ]
                    elif group_name == "maps" and spec["id"] == "map-containers":
                        members = [
                            build_map_container_member(
                                "WB",
                                b"BMD0model",
                                build_permission_grid_section(
                                    2,
                                    2,
                                    "0000000001008100",
                                    "0000000001008101",
                                    "0000000001008102",
                                    "0000000001008103",
                                    "0011223344556677",
                                ),
                                b"\x01\x00\x00\x00",
                            ),
                            build_map_container_member(
                                "NG",
                                b"BMD0model",
                                b"\x00\x00\x00\x00",
                            ),
                        ]
                    elif group_name == "maps" and spec["id"] == "zone-headers":
                        members = [
                            build_zone_header_payload(
                                (10, 11, 2),
                                (12, 13, 5),
                            )
                        ]
                    elif group_name == "maps" and spec["id"] == "map-lookup":
                        members = [
                            (0).to_bytes(2, "little"),
                            (1).to_bytes(2, "little"),
                            (1).to_bytes(2, "little"),
                        ]
                    elif group_name == "maps" and spec["id"] == "map-metadata-candidate":
                        members = [
                            build_map_metadata_candidate_member(
                                ((0, 257),),
                                ((0, 256),),
                                ((0, 256),),
                                ((0, 256),),
                            ),
                            build_map_metadata_candidate_member(
                                ((18, 1024),),
                                ((18, 1024),),
                                ((18, 1024),),
                                ((18, 1024),),
                                trailing_value=4,
                            ),
                        ]
                    elif group_name == "maps" and spec["id"] == "map-side-lookup-candidate":
                        members = [
                            build_map_side_lookup_member(0, 0),
                            build_map_side_lookup_member(10512, 110),
                            build_map_side_lookup_member(10512, 110),
                            build_map_side_lookup_member(16000, 127),
                            build_map_side_lookup_member(16000, 120),
                        ]
                    elif group_name == "scripts":
                        members = [
                            build_script_file(
                                [build_command(0x002E) + build_command(0x003D, 0, 4, 1, 0, 0) + build_command(0x0002)],
                                extra_chunks=[
                                    build_command(0x002E) + build_command(0x0016),
                                ],
                            )
                        ]
                    else:
                        members = [spec["sourcePath"].encode("utf-8"), b"member"]

                    narc = ndspy.narc.NARC.fromFilesAndNames(members)
                    output_path = root / "raw" / "narc" / Path(spec["sourcePath"])
                    output_path.parent.mkdir(parents=True, exist_ok=True)
                    output_path.write_bytes(narc.save())
                    exported_entries.append(
                        {
                            "fileId": counter,
                            "path": spec["sourcePath"],
                            "outputPath": output_path.resolve().relative_to(repo_root()).as_posix(),
                            "size": output_path.stat().st_size,
                            "sha1": f"sha1-{counter}",
                        }
                    )
                    counter += 1

            required_files_payload = {
                "rom": file_index_payload["rom"],
                "requiredFiles": [entry["path"] for entry in exported_entries],
                "exported": exported_entries,
            }
            (root / "raw" / "narc" / "required-files.json").write_text(
                json.dumps(required_files_payload, indent=2, sort_keys=True) + "\n",
                encoding="utf-8",
            )

            result = normalize_m0(root)

            self.assertTrue((root / "normalized" / "metadata" / "rom-info.json").is_file())
            self.assertTrue((root / "normalized" / "text" / "index.json").is_file())
            self.assertTrue((root / "manifests" / "manifest.json").is_file())

            manifest = json.loads((root / "manifests" / "manifest.json").read_text(encoding="utf-8"))
            maps_index = json.loads((root / "normalized" / "maps" / "index.json").read_text(encoding="utf-8"))
            text_index = json.loads((root / "normalized" / "text" / "index.json").read_text(encoding="utf-8"))
            scripts_index = json.loads((root / "normalized" / "scripts" / "index.json").read_text(encoding="utf-8"))
            self.assertEqual(manifest["rom"]["sha1"], "a68b3bedf5c1e53556e41e59cdf396c20b331896")
            self.assertGreaterEqual(len(manifest["normalizedOutputs"]), 5)
            self.assertTrue(result["sourceCatalog"].endswith("normalized/metadata/source-catalog.json"))
            self.assertEqual(maps_index["totalScriptTextBindings"], 4)
            self.assertEqual(maps_index["containers"][0]["mapContainerLayoutCount"], 2)
            self.assertEqual(maps_index["containers"][0]["permissionGridCandidateMapCount"], 1)
            self.assertEqual(maps_index["containers"][0]["permissionGridCandidateCount"], 1)
            self.assertEqual(maps_index["containers"][0]["mapContainerLayouts"][0]["containerTag"], "WB")
            self.assertEqual(maps_index["containers"][0]["mapContainerLayouts"][0]["sectionCount"], 3)
            self.assertEqual(maps_index["containers"][0]["mapContainerLayouts"][0]["modelSectionCount"], 1)
            self.assertEqual(maps_index["containers"][0]["mapContainerLayouts"][0]["permissionGridCandidates"][0]["width"], 2)
            self.assertEqual(maps_index["containers"][0]["mapContainerLayouts"][0]["permissionGridCandidates"][0]["height"], 2)
            self.assertEqual(maps_index["containers"][0]["mapContainerLayouts"][0]["permissionGridCandidates"][0]["recordCount"], 5)
            self.assertEqual(maps_index["containers"][0]["mapContainerLayouts"][0]["permissionGridCandidates"][0]["trailingRecordCount"], 1)
            self.assertEqual(maps_index["containers"][0]["mapContainerLayouts"][0]["permissionGridCandidates"][0]["recordTokens"][4], "0011223344556677")
            self.assertEqual(maps_index["containers"][0]["mapContainerLayouts"][1]["containerTag"], "NG")
            self.assertEqual(maps_index["containers"][0]["mapContainerLayouts"][1]["permissionGridCandidateCount"], 0)
            self.assertEqual(maps_index["containers"][1]["zoneCount"], 2)
            self.assertEqual(maps_index["containers"][1]["zones"][1]["eventTextBankIndex"], 5)
            self.assertEqual(maps_index["containers"][1]["scriptTextBindings"][2]["scriptMemberIndex"], 12)
            self.assertEqual(maps_index["containers"][2]["lookupCount"], 3)
            self.assertEqual(maps_index["containers"][2]["canonicalMapCount"], 2)
            self.assertEqual(maps_index["containers"][2]["identityMappingCount"], 2)
            self.assertEqual(maps_index["containers"][2]["aliasMappingCount"], 1)
            self.assertEqual(maps_index["containers"][2]["mapLookupEntries"][2]["logicalMapIndex"], 2)
            self.assertEqual(maps_index["containers"][2]["mapLookupEntries"][2]["resolvedMapIndex"], 1)
            self.assertFalse(maps_index["containers"][2]["mapLookupEntries"][2]["isIdentityMapping"])
            self.assertEqual(maps_index["containers"][3]["candidateCount"], 2)
            self.assertEqual(maps_index["containers"][3]["seasonSlotCount"], 4)
            self.assertEqual(maps_index["containers"][3]["seasonWordCount"], 31)
            self.assertEqual(maps_index["containers"][3]["mapMetadataCandidates"][0]["seasonProfiles"][0]["nonZeroWords"][0]["value"], 257)
            self.assertEqual(maps_index["containers"][3]["mapMetadataCandidates"][1]["trailingValue"], 4)
            self.assertEqual(maps_index["containers"][3]["mapMetadataCandidates"][1]["seasonProfiles"][3]["nonZeroWords"][0]["wordIndex"], 18)
            map_side_lookup_container = next(
                container for container in maps_index["containers"] if container["id"] == "map-side-lookup-candidate"
            )
            self.assertEqual(map_side_lookup_container["sideLookupEntryCount"], 5)
            self.assertEqual(map_side_lookup_container["distinctSideLookupPairCount"], 4)
            self.assertEqual(map_side_lookup_container["mapSideLookupEntries"][1]["entryIndex"], 1)
            self.assertEqual(map_side_lookup_container["mapSideLookupEntries"][1]["word0"], 10512)
            self.assertEqual(map_side_lookup_container["mapSideLookupEntries"][3]["word1"], 127)
            self.assertEqual(text_index["totalDecodedMessages"], 6)
            self.assertEqual(text_index["containers"][0]["members"][0]["messages"][0]["text"], "system-text first")
            self.assertEqual(text_index["containers"][1]["members"][1]["messages"][0]["text"], "event-text third")
            self.assertEqual(scripts_index["totalDecodedProcedures"], 1)
            self.assertEqual(scripts_index["totalParsedProcedures"], 1)
            self.assertEqual(scripts_index["containers"][0]["members"][0]["procedureCount"], 1)


def build_script_file(entry_chunks: list[bytes], *, extra_chunks: list[bytes] | None = None) -> bytes:
    chunks = list(entry_chunks)
    if extra_chunks:
        chunks.extend(extra_chunks)

    marker_offset = len(entry_chunks) * 4
    current_offset = marker_offset + 2
    chunk_offsets: list[int] = []
    for chunk in chunks:
        chunk_offsets.append(current_offset)
        current_offset += len(chunk)

    payload = bytearray()
    for header_index, chunk_offset in enumerate(chunk_offsets[: len(entry_chunks)]):
        header_offset = header_index * 4
        stored_offset = chunk_offset - (header_offset + 4)
        payload.extend(stored_offset.to_bytes(4, "little"))

    payload.extend((0xFD13).to_bytes(2, "little"))
    for chunk in chunks:
        payload.extend(chunk)

    return bytes(payload)


def build_command(opcode: int, *operands: int) -> bytes:
    payload = bytearray()
    payload.extend(opcode.to_bytes(2, "little"))

    if opcode == 0x003D:
        payload.extend(int(operands[0]).to_bytes(1, "little"))
        payload.extend(int(operands[1]).to_bytes(1, "little"))
        for operand in operands[2:]:
            payload.extend(int(operand).to_bytes(2, "little"))
        return bytes(payload)

    for operand in operands:
        payload.extend(int(operand).to_bytes(2, "little"))

    return bytes(payload)
