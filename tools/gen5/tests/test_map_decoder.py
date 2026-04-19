from __future__ import annotations

import unittest

from tools.gen5.map_decoder import (
    decode_map_container_layouts,
    decode_map_lookup,
    decode_map_metadata_candidates,
    decode_map_side_lookup,
    decode_zone_headers,
)


class MapDecoderTests(unittest.TestCase):
    def test_decode_zone_headers_derives_script_text_bindings(self) -> None:
        payload = build_zone_header_payload(
            (10, 11, 42),
            (12, 13, 77),
        )

        table = decode_zone_headers(payload)

        self.assertEqual(table.zone_count, 2)
        self.assertEqual(table.script_text_binding_count, 4)
        self.assertEqual(table.zones[0].zone_index, 0)
        self.assertEqual(table.zones[0].primary_script_member_index, 10)
        self.assertEqual(table.zones[0].secondary_script_member_index, 11)
        self.assertEqual(table.zones[0].event_text_bank_index, 42)
        self.assertEqual(table.script_text_bindings[0].script_member_index, 10)
        self.assertEqual(table.script_text_bindings[0].text_archive_id, "event-text")
        self.assertEqual(table.script_text_bindings[0].text_bank_index, 42)
        self.assertEqual(table.script_text_bindings[3].script_member_index, 13)
        self.assertEqual(table.script_text_bindings[3].text_bank_index, 77)

    def test_decode_zone_headers_rejects_payload_with_invalid_record_alignment(self) -> None:
        with self.assertRaises(ValueError):
            decode_zone_headers(b"\x00" * 47)

    def test_decode_map_lookup_reports_alias_and_identity_mappings(self) -> None:
        table = decode_map_lookup(
            (
                (0).to_bytes(2, "little"),
                (4).to_bytes(2, "little"),
                (4).to_bytes(2, "little"),
                (3).to_bytes(2, "little"),
            )
        )

        self.assertEqual(table.lookup_count, 4)
        self.assertEqual(table.canonical_map_count, 3)
        self.assertEqual(table.identity_mapping_count, 2)
        self.assertEqual(table.alias_mapping_count, 2)
        self.assertEqual(table.max_resolved_map_index, 4)
        self.assertEqual(table.entries[1].logical_map_index, 1)
        self.assertEqual(table.entries[1].resolved_map_index, 4)
        self.assertFalse(table.entries[1].is_identity_mapping)
        self.assertTrue(table.entries[3].is_identity_mapping)

    def test_decode_map_lookup_rejects_members_with_invalid_size(self) -> None:
        with self.assertRaises(ValueError):
            decode_map_lookup((b"\x00",))

    def test_decode_map_side_lookup_reports_distinct_pairs(self) -> None:
        table = decode_map_side_lookup(
            (
                build_map_side_lookup_member(0, 0),
                build_map_side_lookup_member(10512, 110),
                build_map_side_lookup_member(10512, 110),
                build_map_side_lookup_member(16000, 127),
            )
        )

        self.assertEqual(table.side_lookup_entry_count, 4)
        self.assertEqual(table.distinct_side_lookup_pair_count, 3)
        self.assertEqual(table.entries[0].entry_index, 0)
        self.assertEqual(table.entries[1].word0, 10512)
        self.assertEqual(table.entries[1].word1, 110)
        self.assertEqual(table.entries[3].pair, (16000, 127))

    def test_decode_map_side_lookup_rejects_invalid_member_size(self) -> None:
        with self.assertRaises(ValueError):
            decode_map_side_lookup((b"\x00" * 3,))

    def test_decode_map_container_layouts_reports_model_sections_and_permission_grid_candidates(self) -> None:
        table = decode_map_container_layouts(
            (
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
                    "GC",
                    b"BMD0model",
                    build_permission_grid_section(
                        2,
                        2,
                        "1000000001008100",
                        "1000000001008101",
                        "1000000001008102",
                        "1000000001008103",
                    ),
                    build_permission_grid_section(
                        2,
                        2,
                        "2000000001008100",
                        "2000000001008101",
                        "2000000001008102",
                        "2000000001008103",
                        "3000000001008100",
                        "3000000001008101",
                        "3000000001008102",
                        "3000000001008103",
                    ),
                    b"\x00\x00\x00\x00",
                ),
                build_map_container_member(
                    "NG",
                    b"BMD0model",
                    b"\x00\x00\x00\x00",
                ),
            )
        )

        self.assertEqual(table.layout_count, 3)
        self.assertEqual(table.permission_grid_candidate_map_count, 2)
        self.assertEqual(table.permission_grid_candidate_count, 3)
        self.assertEqual(table.entries[0].container_tag, "WB")
        self.assertEqual(table.entries[0].section_count, 3)
        self.assertEqual(table.entries[0].model_section_count, 1)
        self.assertEqual(table.entries[0].permission_grid_candidate_count, 1)
        self.assertTrue(table.entries[0].sections[1].is_permission_grid_candidate)
        self.assertEqual(table.entries[0].permission_grid_candidates[0].width, 2)
        self.assertEqual(table.entries[0].permission_grid_candidates[0].height, 2)
        self.assertEqual(table.entries[0].permission_grid_candidates[0].record_count, 5)
        self.assertEqual(table.entries[0].permission_grid_candidates[0].plane_count, 1)
        self.assertEqual(table.entries[0].permission_grid_candidates[0].trailing_record_count, 1)
        self.assertEqual(table.entries[0].permission_grid_candidates[0].record_tokens[0], "0000000001008100")
        self.assertEqual(table.entries[1].permission_grid_candidate_count, 2)
        self.assertEqual(table.entries[1].permission_grid_candidates[1].plane_count, 2)
        self.assertEqual(table.entries[1].permission_grid_candidates[1].trailing_record_count, 0)
        self.assertEqual(table.entries[1].permission_grid_candidates[1].record_tokens[7], "3000000001008103")
        self.assertEqual(table.entries[2].container_tag, "NG")
        self.assertEqual(table.entries[2].permission_grid_candidate_count, 0)

    def test_decode_map_container_layouts_rejects_invalid_header(self) -> None:
        with self.assertRaises(ValueError):
            decode_map_container_layouts((b"\x00" * 8,))

    def test_decode_map_metadata_candidates_reports_per_season_word_profiles(self) -> None:
        table = decode_map_metadata_candidates(
            (
                build_map_metadata_candidate_member(
                    ((0, 257), (18, 1024)),
                    ((18, 1024),),
                    ((18, 1024),),
                    ((18, 1024),),
                ),
                build_map_metadata_candidate_member(
                    ((10, 512), (13, 768)),
                    ((10, 512), (13, 768)),
                    ((10, 512), (13, 768)),
                    ((10, 512),),
                    trailing_value=4,
                ),
            )
        )

        self.assertEqual(table.candidate_count, 2)
        self.assertEqual(table.entries[0].logical_map_index, 0)
        self.assertEqual(table.entries[0].season_slot_count, 4)
        self.assertEqual(table.entries[0].distinct_season_profile_count, 2)
        self.assertEqual(table.entries[0].season_profiles[0].word_count, 31)
        self.assertEqual(table.entries[0].season_profiles[0].non_zero_word_count, 2)
        self.assertEqual(table.entries[0].season_profiles[0].non_zero_words[0].word_index, 0)
        self.assertEqual(table.entries[0].season_profiles[0].non_zero_words[0].value, 257)
        self.assertEqual(table.entries[0].season_profiles[0].non_zero_words[1].word_index, 18)
        self.assertEqual(table.entries[0].season_profiles[1].non_zero_words[0].value, 1024)
        self.assertEqual(table.entries[1].trailing_value, 4)
        self.assertEqual(table.entries[1].season_profiles[3].non_zero_word_count, 1)
        self.assertEqual(table.entries[1].season_profiles[3].non_zero_words[0].word_index, 10)

    def test_decode_map_metadata_candidates_rejects_invalid_member_size(self) -> None:
        with self.assertRaises(ValueError):
            decode_map_metadata_candidates((b"\x00" * 248,))


def build_zone_header_payload(*zone_specs: tuple[int, int, int]) -> bytes:
    payload = bytearray()
    for primary_script_member_index, secondary_script_member_index, event_text_bank_index in zone_specs:
        words = [0] * 24
        words[3] = primary_script_member_index
        words[4] = secondary_script_member_index
        words[5] = event_text_bank_index
        for word in words:
            payload.extend(int(word).to_bytes(2, "little"))

    return bytes(payload)


def build_map_metadata_candidate_member(
    *season_specs: tuple[tuple[int, int], ...],
    trailing_value: int = 0,
) -> bytes:
    if len(season_specs) != 4:
        raise ValueError("Map metadata candidate members require exactly four season slot specifications.")

    payload = bytearray()
    for season_spec in season_specs:
        words = [0] * 31
        for word_index, value in season_spec:
            words[word_index] = value

        for word in words:
            payload.extend(int(word).to_bytes(2, "little"))

    payload.extend(int(trailing_value).to_bytes(1, "little"))
    return bytes(payload)


def build_map_side_lookup_member(word0: int, word1: int) -> bytes:
    payload = bytearray()
    payload.extend(int(word0).to_bytes(2, "little"))
    payload.extend(int(word1).to_bytes(2, "little"))
    return bytes(payload)


def build_map_container_member(container_tag: str, *sections: bytes) -> bytes:
    if len(container_tag) != 2:
        raise ValueError("Map container tags must contain exactly two ASCII characters.")

    header_size = (len(sections) + 2) * 4
    first_word = int.from_bytes(container_tag.encode("ascii") + bytes([len(sections), 0]), "little")

    payload = bytearray()
    payload.extend(first_word.to_bytes(4, "little"))
    payload.extend(header_size.to_bytes(4, "little"))

    running_offset = header_size
    for section in sections:
        running_offset += len(section)
        payload.extend(running_offset.to_bytes(4, "little"))

    for section in sections:
        payload.extend(section)

    return bytes(payload)


def build_permission_grid_section(width: int, height: int, *record_tokens: str) -> bytes:
    payload = bytearray()
    payload.extend(int(width).to_bytes(2, "little"))
    payload.extend(int(height).to_bytes(2, "little"))
    for token in record_tokens:
        payload.extend(bytes.fromhex(token))

    return bytes(payload)
