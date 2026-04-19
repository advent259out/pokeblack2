from __future__ import annotations

import unittest
from pathlib import Path

import ndspy.narc

from tools.gen5.contract import repo_root
from tools.gen5.script_decoder import decode_script_file


class ScriptDecoderTests(unittest.TestCase):
    def test_decode_script_file_parses_header_and_dialogue_instruction(self) -> None:
        payload = build_script_file(
            [build_command(0x002E) + build_command(0x00A6, 0x0547) + build_command(0x0074) + build_command(0x003D, 0, 4, 9, 0, 0) + build_command(0x0002)]
        )

        decoded = decode_script_file(payload, program_id="fixture-script")

        self.assertEqual(decoded.header_entry_count, 1)
        self.assertEqual(decoded.header_marker_offset, 4)
        self.assertEqual(decoded.header_entries[0].start_offset, 6)
        self.assertEqual(decoded.procedure_count, 1)
        self.assertEqual(decoded.parsed_procedure_count, 1)
        self.assertEqual(
            [instruction.mnemonic for instruction in decoded.procedures[0].instructions],
            ["LockAll", "PlaySound", "FacePlayer", "Message2", "End"],
        )
        self.assertEqual(decoded.dialogue_line_count, 1)
        self.assertEqual(decoded.dialogue_lines[0].message_id, 9)
        self.assertEqual(decoded.dialogue_lines[0].message_type, 0)

    def test_decode_script_file_discovers_branch_target_procedure(self) -> None:
        script_body = build_command(0x001E, 6)
        function_body = build_command(0x002E) + build_command(0x0016)
        payload = build_script_file([script_body], extra_chunks=[function_body])

        decoded = decode_script_file(payload, program_id="fixture-branch")

        self.assertEqual(decoded.header_entry_count, 1)
        self.assertEqual(decoded.procedure_count, 2)
        self.assertEqual(decoded.procedures[0].entry_kind, "script")
        self.assertEqual(decoded.procedures[1].entry_kind, "function")
        self.assertEqual(decoded.procedures[0].instructions[0].branch_target_offset, decoded.procedures[1].start_offset)
        self.assertEqual(decoded.procedures[1].instructions[0].mnemonic, "LockAll")
        self.assertEqual(decoded.procedures[1].instructions[1].mnemonic, "Return")

    def test_decode_script_file_treats_zero_filled_payload_as_empty(self) -> None:
        decoded = decode_script_file(b"\x00\x00\x00\x00")

        self.assertEqual(decoded.header_entry_count, 0)
        self.assertEqual(decoded.procedure_count, 0)
        self.assertEqual(decoded.dialogue_line_count, 0)

    def test_decode_script_file_reads_known_canonical_europe_member_prefix(self) -> None:
        raw_path = repo_root() / "External" / "Exports" / "BlackWhite" / "M0" / "raw" / "narc" / "a" / "0" / "5" / "7"
        if not raw_path.is_file():
            self.skipTest(f"Canonical script archive is missing at '{raw_path}'.")

        narc = ndspy.narc.NARC(raw_path.read_bytes())
        decoded = decode_script_file(narc.files[0], program_id="canonical:0")

        self.assertEqual(decoded.header_entries[0].start_offset, 16)
        self.assertGreaterEqual(decoded.procedure_count, 1)
        self.assertEqual(decoded.procedures[0].instructions[0].mnemonic, "LockAll")
        self.assertEqual(decoded.procedures[0].instructions[1].mnemonic, "PrepareSoundCue")
        self.assertEqual(decoded.procedures[0].instructions[2].mnemonic, "FacePlayer")


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

    if opcode == 0x001E:
        payload.extend(int(operands[0]).to_bytes(4, "little", signed=True))
        return bytes(payload)

    if opcode == 0x003D:
        payload.extend(int(operands[0]).to_bytes(1, "little"))
        payload.extend(int(operands[1]).to_bytes(1, "little"))
        for operand in operands[2:]:
            payload.extend(int(operand).to_bytes(2, "little"))
        return bytes(payload)

    for operand in operands:
        payload.extend(int(operand).to_bytes(2, "little"))

    return bytes(payload)


if __name__ == "__main__":
    unittest.main()
