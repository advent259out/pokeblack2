from __future__ import annotations

import argparse
import hashlib
import importlib
import json
from datetime import UTC, datetime
from pathlib import Path
from typing import Sequence

from .contract import manifest_path, placeholder_contract_path, relative_to_repo, repo_root
from .map_decoder import (
    decode_map_container_layouts,
    decode_map_lookup,
    decode_map_metadata_candidates,
    decode_map_side_lookup,
    decode_zone_headers,
)
from .manifest_schema import build_manifest
from .script_decoder import decode_script_file
from .text_decoder import decode_text_bank

GROUP_SPECS: dict[str, tuple[dict[str, str], ...]] = {
    "text": (
        {"id": "system-text", "sourcePath": "a/0/0/2"},
        {"id": "event-text", "sourcePath": "a/0/0/3"},
    ),
    "maps": (
        {"id": "map-containers", "sourcePath": "a/0/0/8"},
        {"id": "zone-headers", "sourcePath": "a/0/1/2"},
        {"id": "map-lookup", "sourcePath": "a/0/2/0"},
        {"id": "map-metadata-candidate", "sourcePath": "a/1/7/8"},
        {"id": "map-side-lookup-candidate", "sourcePath": "a/1/9/4"},
    ),
    "scripts": (
        {"id": "script-containers", "sourcePath": "a/0/5/7"},
    ),
    "trainers": (
        {"id": "trainer-metadata", "sourcePath": "a/0/9/2"},
        {"id": "trainer-parties", "sourcePath": "a/0/9/3"},
    ),
    "pokemon": (
        {"id": "learnsets", "sourcePath": "a/0/1/8"},
    ),
    "encounters": (
        {"id": "wild-encounters", "sourcePath": "a/1/2/6"},
    ),
    "visual": (
        {"id": "texture-bundles", "sourcePath": "a/0/1/4"},
    ),
}


class MissingDependencyError(RuntimeError):
    """Raised when ndspy is unavailable for normalization."""


def load_ndspy_narc_module():
    try:
        return importlib.import_module("ndspy.narc")
    except ModuleNotFoundError as exc:
        raise MissingDependencyError(
            "ndspy is not installed. Install it with 'python -m pip install ndspy' before running normalization."
        ) from exc


def load_json(path: Path) -> object:
    return json.loads(path.read_text(encoding="utf-8"))


def sha1_bytes(payload: bytes) -> str:
    return hashlib.sha1(payload).hexdigest()


def write_json(path: Path, payload: object) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def resolve_output_path(root: Path, output_path: str) -> Path:
    candidate = Path(output_path)
    if candidate.is_absolute():
        return candidate

    candidate_paths = (
        repo_root() / candidate,
        root / candidate,
    )
    for resolved_candidate in candidate_paths:
        if resolved_candidate.is_file():
            return resolved_candidate

    checked_paths = ", ".join(path.as_posix() for path in candidate_paths)
    raise FileNotFoundError(f"Unable to resolve exported output path '{output_path}'. Checked: {checked_paths}.")


def summarize_narc_bytes(narc_module, payload: bytes) -> dict[str, object]:
    narc = narc_module.NARC(payload)
    members = []
    total_member_bytes = 0
    largest_member_size = 0

    for index, member in enumerate(narc.files):
        member_size = len(member)
        total_member_bytes += member_size
        largest_member_size = max(largest_member_size, member_size)
        members.append(
            {
                "index": index,
                "size": member_size,
                "sha1": sha1_bytes(member),
            }
        )

    return {
        "containerType": "narc",
        "memberCount": len(members),
        "totalMemberBytes": total_member_bytes,
        "largestMemberSize": largest_member_size,
        "members": members,
    }


def summarize_text_narc_bytes(narc_module, payload: bytes) -> dict[str, object]:
    narc = narc_module.NARC(payload)
    members = []
    total_member_bytes = 0
    largest_member_size = 0
    total_decoded_messages = 0

    for index, member in enumerate(narc.files):
        member_size = len(member)
        decoded_bank = decode_text_bank(member)
        total_member_bytes += member_size
        largest_member_size = max(largest_member_size, member_size)
        total_decoded_messages += decoded_bank.message_count
        members.append(
            {
                "index": index,
                "size": member_size,
                "sha1": sha1_bytes(member),
                **decoded_bank.to_dict(),
            }
        )

    return {
        "containerType": "narc",
        "memberCount": len(members),
        "totalMemberBytes": total_member_bytes,
        "largestMemberSize": largest_member_size,
        "members": members,
        "decodedMessageCount": total_decoded_messages,
    }


def summarize_zone_header_narc_bytes(narc_module, payload: bytes) -> dict[str, object]:
    narc = narc_module.NARC(payload)
    if len(narc.files) != 1:
        raise ValueError(f"Zone header container must contain exactly one member, found '{len(narc.files)}'.")

    member = narc.files[0]
    zone_headers = decode_zone_headers(member)
    return {
        "containerType": "narc",
        "memberCount": 1,
        "totalMemberBytes": len(member),
        "largestMemberSize": len(member),
        "members": [
            {
                "index": 0,
                "size": len(member),
                "sha1": sha1_bytes(member),
            }
        ],
        **zone_headers.to_dict(),
    }


def summarize_map_lookup_narc_bytes(narc_module, payload: bytes) -> dict[str, object]:
    narc = narc_module.NARC(payload)
    map_lookup = decode_map_lookup(tuple(narc.files))
    members = []
    total_member_bytes = 0
    largest_member_size = 0

    for index, member in enumerate(narc.files):
        member_size = len(member)
        total_member_bytes += member_size
        largest_member_size = max(largest_member_size, member_size)
        members.append(
            {
                "index": index,
                "size": member_size,
                "sha1": sha1_bytes(member),
            }
        )

    return {
        "containerType": "narc",
        "memberCount": len(members),
        "totalMemberBytes": total_member_bytes,
        "largestMemberSize": largest_member_size,
        "members": members,
        **map_lookup.to_dict(),
    }


def summarize_map_container_narc_bytes(narc_module, payload: bytes) -> dict[str, object]:
    narc = narc_module.NARC(payload)
    map_container_layouts = decode_map_container_layouts(tuple(narc.files))
    members = []
    total_member_bytes = 0
    largest_member_size = 0

    for index, member in enumerate(narc.files):
        member_size = len(member)
        total_member_bytes += member_size
        largest_member_size = max(largest_member_size, member_size)
        members.append(
            {
                "index": index,
                "size": member_size,
                "sha1": sha1_bytes(member),
            }
        )

    return {
        "containerType": "narc",
        "memberCount": len(members),
        "totalMemberBytes": total_member_bytes,
        "largestMemberSize": largest_member_size,
        "members": members,
        **map_container_layouts.to_dict(),
    }


def summarize_map_metadata_candidate_narc_bytes(narc_module, payload: bytes) -> dict[str, object]:
    narc = narc_module.NARC(payload)
    map_metadata_candidates = decode_map_metadata_candidates(tuple(narc.files))
    members = []
    total_member_bytes = 0
    largest_member_size = 0

    for index, member in enumerate(narc.files):
        member_size = len(member)
        total_member_bytes += member_size
        largest_member_size = max(largest_member_size, member_size)
        members.append(
            {
                "index": index,
                "size": member_size,
                "sha1": sha1_bytes(member),
            }
        )

    return {
        "containerType": "narc",
        "memberCount": len(members),
        "totalMemberBytes": total_member_bytes,
        "largestMemberSize": largest_member_size,
        "members": members,
        **map_metadata_candidates.to_dict(),
    }


def summarize_map_side_lookup_narc_bytes(narc_module, payload: bytes) -> dict[str, object]:
    narc = narc_module.NARC(payload)
    map_side_lookup = decode_map_side_lookup(tuple(narc.files))
    members = []
    total_member_bytes = 0
    largest_member_size = 0

    for index, member in enumerate(narc.files):
        member_size = len(member)
        total_member_bytes += member_size
        largest_member_size = max(largest_member_size, member_size)
        members.append(
            {
                "index": index,
                "size": member_size,
                "sha1": sha1_bytes(member),
            }
        )

    return {
        "containerType": "narc",
        "memberCount": len(members),
        "totalMemberBytes": total_member_bytes,
        "largestMemberSize": largest_member_size,
        "members": members,
        **map_side_lookup.to_dict(),
    }


def summarize_script_narc_bytes(narc_module, payload: bytes, *, container_id: str) -> dict[str, object]:
    narc = narc_module.NARC(payload)
    members = []
    total_member_bytes = 0
    largest_member_size = 0
    total_procedures = 0
    total_parsed_procedures = 0
    total_dialogue_lines = 0

    for index, member in enumerate(narc.files):
        member_size = len(member)
        try:
            decoded_script = decode_script_file(member, program_id=f"{container_id}:{index}")
        except ValueError as exc:
            decoded_script = {
                "headerEntryCount": 0,
                "procedureCount": 0,
                "parsedProcedureCount": 0,
                "dialogueLineCount": 0,
                "headerEntries": [],
                "procedures": [],
                "dialogueLines": [],
                "parseWarningCount": 1,
                "parseWarnings": [str(exc)],
            }
            decoded_procedure_count = 0
            parsed_procedure_count = 0
            dialogue_line_count = 0
        else:
            decoded_script = decoded_script.to_dict()
            decoded_procedure_count = int(decoded_script["procedureCount"])
            parsed_procedure_count = int(decoded_script["parsedProcedureCount"])
            dialogue_line_count = int(decoded_script["dialogueLineCount"])
        total_member_bytes += member_size
        largest_member_size = max(largest_member_size, member_size)
        total_procedures += decoded_procedure_count
        total_parsed_procedures += parsed_procedure_count
        total_dialogue_lines += dialogue_line_count
        members.append(
            {
                "index": index,
                "size": member_size,
                "sha1": sha1_bytes(member),
                **decoded_script,
            }
        )

    return {
        "containerType": "narc",
        "memberCount": len(members),
        "totalMemberBytes": total_member_bytes,
        "largestMemberSize": largest_member_size,
        "members": members,
        "decodedProcedureCount": total_procedures,
        "parsedProcedureCount": total_parsed_procedures,
        "decodedDialogueLineCount": total_dialogue_lines,
    }


def build_group_payload(
    narc_module,
    group_name: str,
    specs: Sequence[dict[str, str]],
    exported_by_path: dict[str, dict[str, object]],
    root: Path,
) -> dict[str, object]:
    containers = []
    for spec in specs:
        source_path = spec["sourcePath"]
        exported = exported_by_path.get(source_path)
        if exported is None:
            raise FileNotFoundError(f"Required raw source '{source_path}' is missing from raw/narc/required-files.json.")

        raw_path = resolve_output_path(root, str(exported["outputPath"]))
        raw_payload = raw_path.read_bytes()
        if group_name == "text":
            narc_summary = summarize_text_narc_bytes(narc_module, raw_payload)
        elif group_name == "maps" and spec["id"] == "map-containers":
            narc_summary = summarize_map_container_narc_bytes(narc_module, raw_payload)
        elif group_name == "maps" and spec["id"] == "zone-headers":
            narc_summary = summarize_zone_header_narc_bytes(narc_module, raw_payload)
        elif group_name == "maps" and spec["id"] == "map-lookup":
            narc_summary = summarize_map_lookup_narc_bytes(narc_module, raw_payload)
        elif group_name == "maps" and spec["id"] == "map-metadata-candidate":
            narc_summary = summarize_map_metadata_candidate_narc_bytes(narc_module, raw_payload)
        elif group_name == "maps" and spec["id"] == "map-side-lookup-candidate":
            narc_summary = summarize_map_side_lookup_narc_bytes(narc_module, raw_payload)
        elif group_name == "scripts":
            narc_summary = summarize_script_narc_bytes(narc_module, raw_payload, container_id=spec["id"])
        else:
            narc_summary = summarize_narc_bytes(narc_module, raw_payload)
        containers.append(
            {
                "id": spec["id"],
                "sourcePath": source_path,
                "rawOutputPath": exported["outputPath"],
                "fileId": exported["fileId"],
                "size": exported["size"],
                "sha1": exported["sha1"],
                **narc_summary,
            }
        )

    payload = {
        "group": group_name,
        "containerCount": len(containers),
        "containers": containers,
    }
    if group_name == "text":
        payload["totalDecodedMessages"] = sum(container["decodedMessageCount"] for container in containers)
    elif group_name == "maps":
        payload["totalScriptTextBindings"] = sum(container.get("scriptTextBindingCount", 0) for container in containers)
    elif group_name == "scripts":
        payload["totalDecodedProcedures"] = sum(container["decodedProcedureCount"] for container in containers)
        payload["totalParsedProcedures"] = sum(container["parsedProcedureCount"] for container in containers)
        payload["totalDecodedDialogueLines"] = sum(container["decodedDialogueLineCount"] for container in containers)

    return payload


def normalize_m0(root: Path) -> dict[str, str]:
    narc_module = load_ndspy_narc_module()
    resolved_root = root.resolve()

    raw_file_index_path = resolved_root / "raw" / "rom" / "file-index.json"
    required_files_path = resolved_root / "raw" / "narc" / "required-files.json"
    if not raw_file_index_path.is_file():
        raise FileNotFoundError(f"Missing raw file index at '{raw_file_index_path}'. Run extract_ndspy_m0 first.")
    if not required_files_path.is_file():
        raise FileNotFoundError(f"Missing required-files summary at '{required_files_path}'. Run extract_ndspy_m0 first.")

    raw_file_index = load_json(raw_file_index_path)
    required_files = load_json(required_files_path)
    rom_info = raw_file_index["rom"]
    entries = raw_file_index["entries"]
    exported_by_path = {entry["path"]: entry for entry in required_files["exported"]}

    normalized_root = resolved_root / "normalized"
    metadata_root = normalized_root / "metadata"

    file_index_payload = {
        "rom": rom_info,
        "fileCount": raw_file_index["fileCount"],
        "namedFileCount": sum(1 for entry in entries if entry["path"] is not None),
        "unnamedFileCount": sum(1 for entry in entries if entry["path"] is None),
        "entries": entries,
    }
    rom_info_payload = {
        "game": rom_info["game"],
        "filename": rom_info["filename"],
        "sha1": rom_info["sha1"],
        "size": rom_info["size"],
        "fileCount": raw_file_index["fileCount"],
        "namedFileCount": file_index_payload["namedFileCount"],
        "unnamedFileCount": file_index_payload["unnamedFileCount"],
    }

    source_catalog_entries = []
    output_paths: list[Path] = []

    file_index_output = metadata_root / "file-index.json"
    rom_info_output = metadata_root / "rom-info.json"
    source_catalog_output = metadata_root / "source-catalog.json"
    normalization_report_output = metadata_root / "normalization-report.json"

    write_json(file_index_output, file_index_payload)
    output_paths.extend([file_index_output, rom_info_output, source_catalog_output, normalization_report_output])
    write_json(rom_info_output, rom_info_payload)

    group_outputs: dict[str, str] = {}
    for group_name, specs in GROUP_SPECS.items():
        payload = build_group_payload(narc_module, group_name, specs, exported_by_path, resolved_root)
        for container in payload["containers"]:
            source_catalog_entries.append(
                {
                    "group": group_name,
                    "id": container["id"],
                    "sourcePath": container["sourcePath"],
                    "fileId": container["fileId"],
                    "size": container["size"],
                    "sha1": container["sha1"],
                    "memberCount": container["memberCount"],
                    "largestMemberSize": container["largestMemberSize"],
                }
            )

        group_output = normalized_root / group_name / "index.json"
        write_json(group_output, payload)
        output_paths.append(group_output)
        group_outputs[group_name] = relative_to_repo(group_output)

    source_catalog_payload = {
        "rom": rom_info,
        "sourceCount": len(source_catalog_entries),
        "sources": source_catalog_entries,
    }
    write_json(source_catalog_output, source_catalog_payload)

    placeholder_path = placeholder_contract_path(resolved_root)
    if not placeholder_path.is_file():
        raise FileNotFoundError(f"Missing placeholder contract at '{placeholder_path}'. Run prepare_export_root first.")

    report_payload = {
        "generatedAt": datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
        "rom": rom_info,
        "normalizedRoot": relative_to_repo(normalized_root),
        "outputCount": len(output_paths) + 1,
        "groupOutputs": group_outputs,
        "sourceCatalog": relative_to_repo(source_catalog_output),
        "placeholderContract": relative_to_repo(placeholder_path),
    }
    write_json(normalization_report_output, report_payload)

    normalized_outputs = []
    final_output_paths = [placeholder_path, *output_paths]
    for output_path in final_output_paths:
        normalized_outputs.append(
            {
                "path": relative_to_repo(output_path),
                "hash": sha1_bytes(output_path.read_bytes()),
            }
        )

    manifest = build_manifest(rom=rom_info, export_root=resolved_root, normalized_outputs=normalized_outputs)
    write_json(manifest_path(resolved_root), manifest)

    return {
        "fileIndex": relative_to_repo(file_index_output),
        "romInfo": relative_to_repo(rom_info_output),
        "sourceCatalog": relative_to_repo(source_catalog_output),
        "normalizationReport": relative_to_repo(normalization_report_output),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Normalize the first M0 raw extraction set into importer-facing JSON summaries.")
    parser.add_argument("--root", default="External/Exports/BlackWhite/M0", help="Export root directory.")
    args = parser.parse_args()

    result = normalize_m0(Path(args.root))
    print(json.dumps(result, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
