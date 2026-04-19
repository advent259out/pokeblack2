from __future__ import annotations

from dataclasses import dataclass
from struct import unpack_from


_ZONE_HEADER_ENTRY_SIZE = 48
_PRIMARY_SCRIPT_MEMBER_INDEX = 3
_SECONDARY_SCRIPT_MEMBER_INDEX = 4
_EVENT_TEXT_BANK_INDEX = 5
_MAP_METADATA_CANDIDATE_MEMBER_SIZE = 249
_MAP_METADATA_SEASON_SLOT_COUNT = 4
_MAP_METADATA_SEASON_WORD_COUNT = 31
_MAP_SIDE_LOOKUP_MEMBER_SIZE = 4
_MAP_CONTAINER_MODEL_MAGIC = b"BMD0"
_MAP_CONTAINER_GRID_RECORD_STRIDE = 8


@dataclass(frozen=True)
class DecodedZoneScriptTextBinding:
    zone_index: int
    script_member_index: int
    text_archive_id: str
    text_bank_index: int

    def to_dict(self) -> dict[str, object]:
        return {
            "zoneIndex": self.zone_index,
            "scriptMemberIndex": self.script_member_index,
            "textArchiveId": self.text_archive_id,
            "textBankIndex": self.text_bank_index,
        }


@dataclass(frozen=True)
class DecodedZoneHeader:
    zone_index: int
    primary_script_member_index: int
    secondary_script_member_index: int
    event_text_archive_id: str
    event_text_bank_index: int
    script_text_bindings: tuple[DecodedZoneScriptTextBinding, ...]

    def to_dict(self) -> dict[str, object]:
        return {
            "zoneIndex": self.zone_index,
            "primaryScriptMemberIndex": self.primary_script_member_index,
            "secondaryScriptMemberIndex": self.secondary_script_member_index,
            "eventTextArchiveId": self.event_text_archive_id,
            "eventTextBankIndex": self.event_text_bank_index,
            "scriptTextBindingCount": len(self.script_text_bindings),
            "scriptTextBindings": [binding.to_dict() for binding in self.script_text_bindings],
        }


@dataclass(frozen=True)
class DecodedZoneHeaderTable:
    zones: tuple[DecodedZoneHeader, ...]

    @property
    def zone_count(self) -> int:
        return len(self.zones)

    @property
    def script_text_bindings(self) -> tuple[DecodedZoneScriptTextBinding, ...]:
        bindings: list[DecodedZoneScriptTextBinding] = []
        for zone in self.zones:
            bindings.extend(zone.script_text_bindings)

        return tuple(bindings)

    @property
    def script_text_binding_count(self) -> int:
        return len(self.script_text_bindings)

    def to_dict(self) -> dict[str, object]:
        return {
            "zoneCount": self.zone_count,
            "zones": [zone.to_dict() for zone in self.zones],
            "scriptTextBindingCount": self.script_text_binding_count,
            "scriptTextBindings": [binding.to_dict() for binding in self.script_text_bindings],
        }


@dataclass(frozen=True)
class DecodedMapLookupEntry:
    logical_map_index: int
    resolved_map_index: int
    is_identity_mapping: bool

    def to_dict(self) -> dict[str, object]:
        return {
            "logicalMapIndex": self.logical_map_index,
            "resolvedMapIndex": self.resolved_map_index,
            "isIdentityMapping": self.is_identity_mapping,
        }


@dataclass(frozen=True)
class DecodedMapLookupTable:
    entries: tuple[DecodedMapLookupEntry, ...]

    @property
    def lookup_count(self) -> int:
        return len(self.entries)

    @property
    def canonical_map_count(self) -> int:
        return len({entry.resolved_map_index for entry in self.entries})

    @property
    def identity_mapping_count(self) -> int:
        return sum(1 for entry in self.entries if entry.is_identity_mapping)

    @property
    def alias_mapping_count(self) -> int:
        return self.lookup_count - self.identity_mapping_count

    @property
    def max_resolved_map_index(self) -> int:
        if not self.entries:
            return -1

        return max(entry.resolved_map_index for entry in self.entries)

    def to_dict(self) -> dict[str, object]:
        return {
            "lookupCount": self.lookup_count,
            "canonicalMapCount": self.canonical_map_count,
            "identityMappingCount": self.identity_mapping_count,
            "aliasMappingCount": self.alias_mapping_count,
            "maxResolvedMapIndex": self.max_resolved_map_index,
            "mapLookupEntries": [entry.to_dict() for entry in self.entries],
        }


@dataclass(frozen=True)
class DecodedMapSideLookupEntry:
    entry_index: int
    word0: int
    word1: int

    @property
    def pair(self) -> tuple[int, int]:
        return (self.word0, self.word1)

    def to_dict(self) -> dict[str, object]:
        return {
            "entryIndex": self.entry_index,
            "word0": self.word0,
            "word1": self.word1,
        }


@dataclass(frozen=True)
class DecodedMapSideLookupTable:
    entries: tuple[DecodedMapSideLookupEntry, ...]

    @property
    def side_lookup_entry_count(self) -> int:
        return len(self.entries)

    @property
    def distinct_side_lookup_pair_count(self) -> int:
        return len({entry.pair for entry in self.entries})

    def to_dict(self) -> dict[str, object]:
        return {
            "sideLookupEntryCount": self.side_lookup_entry_count,
            "distinctSideLookupPairCount": self.distinct_side_lookup_pair_count,
            "mapSideLookupEntries": [entry.to_dict() for entry in self.entries],
        }


@dataclass(frozen=True)
class DecodedMapContainerSection:
    section_index: int
    offset: int
    size: int
    starts_with_model_magic: bool
    is_permission_grid_candidate: bool

    def to_dict(self) -> dict[str, object]:
        return {
            "sectionIndex": self.section_index,
            "offset": self.offset,
            "size": self.size,
            "startsWithModelMagic": self.starts_with_model_magic,
            "isPermissionGridCandidate": self.is_permission_grid_candidate,
        }


@dataclass(frozen=True)
class DecodedMapPermissionGridCandidate:
    section_index: int
    width: int
    height: int
    record_stride_bytes: int
    record_count: int
    plane_count: int
    trailing_record_count: int
    record_tokens: tuple[str, ...]

    @property
    def primary_cell_count(self) -> int:
        return self.width * self.height

    @property
    def record_token_count(self) -> int:
        return len(self.record_tokens)

    def to_dict(self) -> dict[str, object]:
        return {
            "sectionIndex": self.section_index,
            "width": self.width,
            "height": self.height,
            "primaryCellCount": self.primary_cell_count,
            "recordStrideBytes": self.record_stride_bytes,
            "recordCount": self.record_count,
            "planeCount": self.plane_count,
            "trailingRecordCount": self.trailing_record_count,
            "recordTokenCount": self.record_token_count,
            "recordTokens": list(self.record_tokens),
        }


@dataclass(frozen=True)
class DecodedMapContainerLayout:
    map_container_index: int
    container_tag: str
    sections: tuple[DecodedMapContainerSection, ...]
    permission_grid_candidates: tuple[DecodedMapPermissionGridCandidate, ...]

    @property
    def section_count(self) -> int:
        return len(self.sections)

    @property
    def model_section_count(self) -> int:
        return sum(1 for section in self.sections if section.starts_with_model_magic)

    @property
    def permission_grid_candidate_count(self) -> int:
        return len(self.permission_grid_candidates)

    def to_dict(self) -> dict[str, object]:
        return {
            "mapContainerIndex": self.map_container_index,
            "containerTag": self.container_tag,
            "sectionCount": self.section_count,
            "modelSectionCount": self.model_section_count,
            "permissionGridCandidateCount": self.permission_grid_candidate_count,
            "sections": [section.to_dict() for section in self.sections],
            "permissionGridCandidates": [candidate.to_dict() for candidate in self.permission_grid_candidates],
        }


@dataclass(frozen=True)
class DecodedMapContainerLayoutTable:
    entries: tuple[DecodedMapContainerLayout, ...]

    @property
    def layout_count(self) -> int:
        return len(self.entries)

    @property
    def permission_grid_candidate_map_count(self) -> int:
        return sum(1 for entry in self.entries if entry.permission_grid_candidate_count > 0)

    @property
    def permission_grid_candidate_count(self) -> int:
        return sum(entry.permission_grid_candidate_count for entry in self.entries)

    def to_dict(self) -> dict[str, object]:
        return {
            "mapContainerLayoutCount": self.layout_count,
            "permissionGridCandidateMapCount": self.permission_grid_candidate_map_count,
            "permissionGridCandidateCount": self.permission_grid_candidate_count,
            "mapContainerLayouts": [entry.to_dict() for entry in self.entries],
        }


@dataclass(frozen=True)
class DecodedSeasonWordValue:
    word_index: int
    value: int

    def to_dict(self) -> dict[str, object]:
        return {
            "wordIndex": self.word_index,
            "value": self.value,
        }


@dataclass(frozen=True)
class DecodedSeasonSlotProfile:
    season_slot_index: int
    word_values: tuple[int, ...]

    @property
    def word_count(self) -> int:
        return len(self.word_values)

    @property
    def non_zero_words(self) -> tuple[DecodedSeasonWordValue, ...]:
        return tuple(
            DecodedSeasonWordValue(word_index=index, value=value)
            for index, value in enumerate(self.word_values)
            if value != 0
        )

    @property
    def non_zero_word_count(self) -> int:
        return len(self.non_zero_words)

    def to_dict(self) -> dict[str, object]:
        return {
            "seasonSlotIndex": self.season_slot_index,
            "wordCount": self.word_count,
            "wordValues": list(self.word_values),
            "nonZeroWordCount": self.non_zero_word_count,
            "nonZeroWords": [word.to_dict() for word in self.non_zero_words],
        }


@dataclass(frozen=True)
class DecodedMapMetadataCandidate:
    logical_map_index: int
    season_profiles: tuple[DecodedSeasonSlotProfile, ...]
    trailing_value: int

    @property
    def season_slot_count(self) -> int:
        return len(self.season_profiles)

    @property
    def distinct_season_profile_count(self) -> int:
        return len({profile.word_values for profile in self.season_profiles})

    def to_dict(self) -> dict[str, object]:
        return {
            "logicalMapIndex": self.logical_map_index,
            "seasonSlotCount": self.season_slot_count,
            "distinctSeasonProfileCount": self.distinct_season_profile_count,
            "trailingValue": self.trailing_value,
            "seasonProfiles": [profile.to_dict() for profile in self.season_profiles],
        }


@dataclass(frozen=True)
class DecodedMapMetadataCandidateTable:
    entries: tuple[DecodedMapMetadataCandidate, ...]

    @property
    def candidate_count(self) -> int:
        return len(self.entries)

    def to_dict(self) -> dict[str, object]:
        return {
            "candidateCount": self.candidate_count,
            "seasonSlotCount": _MAP_METADATA_SEASON_SLOT_COUNT,
            "seasonWordCount": _MAP_METADATA_SEASON_WORD_COUNT,
            "mapMetadataCandidates": [entry.to_dict() for entry in self.entries],
        }


def decode_zone_headers(payload: bytes) -> DecodedZoneHeaderTable:
    if len(payload) == 0:
        return DecodedZoneHeaderTable(zones=())

    if len(payload) % _ZONE_HEADER_ENTRY_SIZE != 0:
        raise ValueError(
            f"Zone header payload length '{len(payload)}' is not aligned to the {_ZONE_HEADER_ENTRY_SIZE}-byte record size."
        )

    zones: list[DecodedZoneHeader] = []
    for zone_index in range(len(payload) // _ZONE_HEADER_ENTRY_SIZE):
        entry_offset = zone_index * _ZONE_HEADER_ENTRY_SIZE
        words = unpack_from("<24H", payload, entry_offset)
        event_text_bank_index = words[_EVENT_TEXT_BANK_INDEX]
        bindings = (
            DecodedZoneScriptTextBinding(
                zone_index=zone_index,
                script_member_index=words[_PRIMARY_SCRIPT_MEMBER_INDEX],
                text_archive_id="event-text",
                text_bank_index=event_text_bank_index,
            ),
            DecodedZoneScriptTextBinding(
                zone_index=zone_index,
                script_member_index=words[_SECONDARY_SCRIPT_MEMBER_INDEX],
                text_archive_id="event-text",
                text_bank_index=event_text_bank_index,
            ),
        )
        zones.append(
            DecodedZoneHeader(
                zone_index=zone_index,
                primary_script_member_index=words[_PRIMARY_SCRIPT_MEMBER_INDEX],
                secondary_script_member_index=words[_SECONDARY_SCRIPT_MEMBER_INDEX],
                event_text_archive_id="event-text",
                event_text_bank_index=event_text_bank_index,
                script_text_bindings=bindings,
            )
        )

    return DecodedZoneHeaderTable(zones=tuple(zones))


def decode_map_lookup(members: list[bytes] | tuple[bytes, ...]) -> DecodedMapLookupTable:
    entries: list[DecodedMapLookupEntry] = []
    for logical_map_index, member in enumerate(members):
        if len(member) != 2:
            raise ValueError(
                f"Map lookup entry '{logical_map_index}' must contain exactly 2 bytes, found '{len(member)}'."
            )

        resolved_map_index = unpack_from("<H", member, 0)[0]
        entries.append(
            DecodedMapLookupEntry(
                logical_map_index=logical_map_index,
                resolved_map_index=resolved_map_index,
                is_identity_mapping=logical_map_index == resolved_map_index,
            )
        )

    return DecodedMapLookupTable(entries=tuple(entries))


def decode_map_side_lookup(members: list[bytes] | tuple[bytes, ...]) -> DecodedMapSideLookupTable:
    entries: list[DecodedMapSideLookupEntry] = []
    for entry_index, member in enumerate(members):
        if len(member) != _MAP_SIDE_LOOKUP_MEMBER_SIZE:
            raise ValueError(
                "Map side lookup entry "
                f"'{entry_index}' must contain exactly {_MAP_SIDE_LOOKUP_MEMBER_SIZE} bytes, "
                f"found '{len(member)}'."
            )

        word0, word1 = unpack_from("<HH", member, 0)
        entries.append(
            DecodedMapSideLookupEntry(
                entry_index=entry_index,
                word0=word0,
                word1=word1,
            )
        )

    return DecodedMapSideLookupTable(entries=tuple(entries))


def decode_map_container_layouts(
    members: list[bytes] | tuple[bytes, ...],
) -> DecodedMapContainerLayoutTable:
    entries: list[DecodedMapContainerLayout] = []
    for map_container_index, member in enumerate(members):
        if len(member) < 12:
            raise ValueError(
                f"Map container entry '{map_container_index}' must contain at least 12 bytes, found '{len(member)}'."
            )

        raw_tag_value, header_size = unpack_from("<II", member, 0)
        if header_size < 12 or header_size % 4 != 0:
            raise ValueError(
                f"Map container entry '{map_container_index}' declares invalid header size '{header_size}'."
            )

        if header_size > len(member):
            raise ValueError(
                f"Map container entry '{map_container_index}' header size '{header_size}' exceeds payload size '{len(member)}'."
            )

        section_descriptor_count = header_size // 4
        if section_descriptor_count < 3:
            raise ValueError(
                f"Map container entry '{map_container_index}' must declare at least one section, found descriptor count '{section_descriptor_count}'."
            )

        section_end_offsets = unpack_from(f"<{section_descriptor_count}I", member, 0)[2:]
        previous_end_offset = header_size
        sections: list[DecodedMapContainerSection] = []
        permission_grid_candidates: list[DecodedMapPermissionGridCandidate] = []
        for section_index, end_offset in enumerate(section_end_offsets):
            if end_offset < previous_end_offset or end_offset > len(member):
                raise ValueError(
                    f"Map container entry '{map_container_index}' section '{section_index}' end offset '{end_offset}' is outside the valid range '{previous_end_offset}..{len(member)}'."
                )

            section = member[previous_end_offset:end_offset]
            permission_grid_candidate = _try_decode_permission_grid_candidate(section_index, section)
            sections.append(
                DecodedMapContainerSection(
                    section_index=section_index,
                    offset=previous_end_offset,
                    size=len(section),
                    starts_with_model_magic=section.startswith(_MAP_CONTAINER_MODEL_MAGIC),
                    is_permission_grid_candidate=permission_grid_candidate is not None,
                )
            )
            if permission_grid_candidate is not None:
                permission_grid_candidates.append(permission_grid_candidate)

            previous_end_offset = end_offset

        if previous_end_offset != len(member):
            raise ValueError(
                f"Map container entry '{map_container_index}' ended at offset '{previous_end_offset}', but payload size is '{len(member)}'."
            )

        entries.append(
            DecodedMapContainerLayout(
                map_container_index=map_container_index,
                container_tag=raw_tag_value.to_bytes(4, "little")[:2].decode("ascii", "replace"),
                sections=tuple(sections),
                permission_grid_candidates=tuple(permission_grid_candidates),
            )
        )

    return DecodedMapContainerLayoutTable(entries=tuple(entries))


def decode_map_metadata_candidates(
    members: list[bytes] | tuple[bytes, ...],
) -> DecodedMapMetadataCandidateTable:
    entries: list[DecodedMapMetadataCandidate] = []
    for logical_map_index, member in enumerate(members):
        if len(member) != _MAP_METADATA_CANDIDATE_MEMBER_SIZE:
            raise ValueError(
                "Map metadata candidate entry "
                f"'{logical_map_index}' must contain exactly {_MAP_METADATA_CANDIDATE_MEMBER_SIZE} bytes, "
                f"found '{len(member)}'."
            )

        words = unpack_from(f"<{_MAP_METADATA_SEASON_SLOT_COUNT * _MAP_METADATA_SEASON_WORD_COUNT}H", member, 0)
        season_profiles = []
        for season_slot_index in range(_MAP_METADATA_SEASON_SLOT_COUNT):
            start = season_slot_index * _MAP_METADATA_SEASON_WORD_COUNT
            end = start + _MAP_METADATA_SEASON_WORD_COUNT
            season_profiles.append(
                DecodedSeasonSlotProfile(
                    season_slot_index=season_slot_index,
                    word_values=tuple(words[start:end]),
                )
            )

        entries.append(
            DecodedMapMetadataCandidate(
                logical_map_index=logical_map_index,
                season_profiles=tuple(season_profiles),
                trailing_value=member[-1],
            )
        )

    return DecodedMapMetadataCandidateTable(entries=tuple(entries))


def _try_decode_permission_grid_candidate(
    section_index: int,
    section: bytes,
) -> DecodedMapPermissionGridCandidate | None:
    if len(section) < 4:
        return None

    width, height = unpack_from("<HH", section, 0)
    primary_cell_count = width * height
    if width <= 0 or height <= 0 or primary_cell_count <= 0:
        return None

    data_bytes = len(section) - 4
    if data_bytes < primary_cell_count * _MAP_CONTAINER_GRID_RECORD_STRIDE:
        return None

    if data_bytes % _MAP_CONTAINER_GRID_RECORD_STRIDE != 0:
        return None

    record_count = data_bytes // _MAP_CONTAINER_GRID_RECORD_STRIDE
    plane_count = record_count // primary_cell_count
    trailing_record_count = record_count % primary_cell_count
    if plane_count <= 0:
        return None

    record_tokens = []
    for record_index in range(record_count):
        record_offset = 4 + (record_index * _MAP_CONTAINER_GRID_RECORD_STRIDE)
        record_tokens.append(
            section[record_offset:record_offset + _MAP_CONTAINER_GRID_RECORD_STRIDE].hex()
        )

    return DecodedMapPermissionGridCandidate(
        section_index=section_index,
        width=width,
        height=height,
        record_stride_bytes=_MAP_CONTAINER_GRID_RECORD_STRIDE,
        record_count=record_count,
        plane_count=plane_count,
        trailing_record_count=trailing_record_count,
        record_tokens=tuple(record_tokens),
    )
