from __future__ import annotations

from dataclasses import dataclass


_HEADER_TERMINATOR = 0xFD13


@dataclass(frozen=True)
class ScriptCommandSpec:
    mnemonic: str
    operand_kinds: tuple[str, ...] = ()
    is_terminal: bool = False
    branch_operand_index: int | None = None
    branch_is_relative_to_instruction: bool = False
    dialogue_kind: str | None = None


@dataclass(frozen=True)
class DecodedScriptHeaderEntry:
    header_index: int
    header_offset: int
    stored_offset: int
    start_offset: int

    def to_dict(self) -> dict[str, int]:
        return {
            "headerIndex": self.header_index,
            "headerOffset": self.header_offset,
            "storedOffset": self.stored_offset,
            "startOffset": self.start_offset,
        }


@dataclass(frozen=True)
class DecodedScriptInstruction:
    offset: int
    opcode: int
    mnemonic: str
    byte_length: int
    operands: tuple[int, ...] = ()
    branch_target_offset: int | None = None

    def to_dict(self) -> dict[str, object]:
        payload: dict[str, object] = {
            "offset": self.offset,
            "opcode": self.opcode,
            "mnemonic": self.mnemonic,
            "byteLength": self.byte_length,
        }
        if self.operands:
            payload["operands"] = list(self.operands)
        if self.branch_target_offset is not None:
            payload["branchTargetOffset"] = self.branch_target_offset

        return payload


@dataclass(frozen=True)
class DecodedScriptDialogueLine:
    line_id: str
    procedure_id: str
    instruction_offset: int
    command: str
    message_id: int
    speaker_object_id: int | None = None
    view_type: int | None = None
    message_type: int | None = None
    variant_a: int | None = None
    variant_b: int | None = None

    def to_dict(self) -> dict[str, object]:
        payload: dict[str, object] = {
            "lineId": self.line_id,
            "procedureId": self.procedure_id,
            "instructionOffset": self.instruction_offset,
            "command": self.command,
            "messageId": self.message_id,
        }
        if self.speaker_object_id is not None:
            payload["speakerObjectId"] = self.speaker_object_id
        if self.view_type is not None:
            payload["viewType"] = self.view_type
        if self.message_type is not None:
            payload["messageType"] = self.message_type
        if self.variant_a is not None:
            payload["variantA"] = self.variant_a
        if self.variant_b is not None:
            payload["variantB"] = self.variant_b

        return payload


@dataclass(frozen=True)
class DecodedScriptProcedure:
    procedure_id: str
    entry_kind: str
    header_index: int | None
    start_offset: int
    end_offset: int
    parse_status: str
    instructions: tuple[DecodedScriptInstruction, ...]
    dialogue_lines: tuple[DecodedScriptDialogueLine, ...]

    def to_dict(self) -> dict[str, object]:
        payload: dict[str, object] = {
            "procedureId": self.procedure_id,
            "entryKind": self.entry_kind,
            "startOffset": self.start_offset,
            "endOffset": self.end_offset,
            "parseStatus": self.parse_status,
            "instructionCount": len(self.instructions),
            "instructions": [instruction.to_dict() for instruction in self.instructions],
            "dialogueLineCount": len(self.dialogue_lines),
            "dialogueLines": [line.to_dict() for line in self.dialogue_lines],
        }
        if self.header_index is not None:
            payload["headerIndex"] = self.header_index

        return payload


@dataclass(frozen=True)
class DecodedScriptFile:
    header_marker_offset: int | None
    header_entries: tuple[DecodedScriptHeaderEntry, ...]
    procedures: tuple[DecodedScriptProcedure, ...]
    dialogue_lines: tuple[DecodedScriptDialogueLine, ...]
    warnings: tuple[str, ...] = ()

    @property
    def header_entry_count(self) -> int:
        return len(self.header_entries)

    @property
    def procedure_count(self) -> int:
        return len(self.procedures)

    @property
    def parsed_procedure_count(self) -> int:
        return sum(1 for procedure in self.procedures if procedure.parse_status == "complete")

    @property
    def dialogue_line_count(self) -> int:
        return len(self.dialogue_lines)

    def to_dict(self) -> dict[str, object]:
        payload: dict[str, object] = {
            "headerEntryCount": self.header_entry_count,
            "procedureCount": self.procedure_count,
            "parsedProcedureCount": self.parsed_procedure_count,
            "dialogueLineCount": self.dialogue_line_count,
            "headerEntries": [entry.to_dict() for entry in self.header_entries],
            "procedures": [procedure.to_dict() for procedure in self.procedures],
            "dialogueLines": [line.to_dict() for line in self.dialogue_lines],
            "parseWarningCount": len(self.warnings),
            "parseWarnings": list(self.warnings),
        }
        if self.header_marker_offset is not None:
            payload["headerMarkerOffset"] = self.header_marker_offset

        return payload


COMMAND_SPECS: dict[int, ScriptCommandSpec] = {
    0x0001: ScriptCommandSpec("Nop"),
    0x0002: ScriptCommandSpec("End", is_terminal=True),
    0x0003: ScriptCommandSpec("Wait", ("u16",)),
    0x0004: ScriptCommandSpec("CallRoutine", ("s32",)),
    0x0005: ScriptCommandSpec("EndRoutine", is_terminal=True),
    0x0006: ScriptCommandSpec("CheckFlag", ("u16", "u16")),
    0x0007: ScriptCommandSpec("CheckFlagUnset", ("u16", "u16")),
    0x0008: ScriptCommandSpec("StoreFlag", ("u16",)),
    0x0009: ScriptCommandSpec("StoreVar", ("u16",)),
    0x000A: ScriptCommandSpec("ClearVar", ("u16",)),
    0x0010: ScriptCommandSpec("StoreFlag", ("u16",)),
    0x0011: ScriptCommandSpec("StoreCondition", ("u16",)),
    0x0014: ScriptCommandSpec("MovementApply", ("u16",)),
    0x0016: ScriptCommandSpec("Return", is_terminal=True),
    0x0017: ScriptCommandSpec("MovementWait", ("u16",)),
    0x0019: ScriptCommandSpec("CompareVars", ("u16", "u16")),
    0x001C: ScriptCommandSpec("CallStd", ("u16",)),
    0x001D: ScriptCommandSpec("EndStdReturn", ("u16",), is_terminal=True),
    0x001E: ScriptCommandSpec("GoTo", ("s32",), is_terminal=True, branch_operand_index=0, branch_is_relative_to_instruction=True),
    0x001F: ScriptCommandSpec(
        "IfThenGoTo",
        ("u8", "s32"),
        branch_operand_index=1,
        branch_is_relative_to_instruction=True,
    ),
    0x0023: ScriptCommandSpec("SetFlag", ("u16",)),
    0x0024: ScriptCommandSpec("ClearFlag", ("u16",)),
    0x0028: ScriptCommandSpec("BorderMessage", ("u16", "u16")),
    0x002A: ScriptCommandSpec("CopyVar", ("u16", "u16")),
    0x002E: ScriptCommandSpec("LockAll"),
    0x0030: ScriptCommandSpec("ReleaseAll"),
    0x0032: ScriptCommandSpec("WaitForMessage"),
    0x0034: ScriptCommandSpec("ApplyMovementSet", ("u16",)),
    0x0036: ScriptCommandSpec("WaitMovementSet", ("u16",)),
    0x0039: ScriptCommandSpec("CloseBubbleMessage"),
    0x003C: ScriptCommandSpec("Message1", ("u8", "u8", "u16", "u16", "u16", "u16"), dialogue_kind="message1"),
    0x003D: ScriptCommandSpec("Message2", ("u8", "u8", "u16", "u16", "u16"), dialogue_kind="message2"),
    0x003E: ScriptCommandSpec("CloseMessage"),
    0x003F: ScriptCommandSpec("CloseMessageWindow"),
    0x0043: ScriptCommandSpec("BorderMessageEx", ("u16", "u16")),
    0x0044: ScriptCommandSpec("CloseBorderMessage"),
    0x0047: ScriptCommandSpec("PopYesNoVar", ("u16",)),
    0x004B: ScriptCommandSpec("CloseAngryMessage"),
    0x004C: ScriptCommandSpec("MessageStyle", ("u8",)),
    0x0064: ScriptCommandSpec("ApplyMovement", ("u16", "s32")),
    0x0065: ScriptCommandSpec("WaitMovement"),
    0x0068: ScriptCommandSpec("StoreHeroPosition", ("u16", "u16")),
    0x006B: ScriptCommandSpec("RunScript", ("u16",)),
    0x006C: ScriptCommandSpec("RunScriptAsync", ("u16",)),
    0x006D: ScriptCommandSpec("WaitScript", ("u16", "u16", "u16", "u16")),
    0x0074: ScriptCommandSpec("FacePlayer"),
    0x008D: ScriptCommandSpec("SetVarBattleResult", ("u16",)),
    0x008E: ScriptCommandSpec("DisableTrainer"),
    0x0098: ScriptCommandSpec("TrainerBattle", ("u16",)),
    0x009B: ScriptCommandSpec("RegisterTrainer", ("u16",)),
    0x009C: ScriptCommandSpec("UnregisterTrainer", ("u16",)),
    0x009F: ScriptCommandSpec("StoreTrainerId", ("u16",)),
    0x00A6: ScriptCommandSpec("PlaySound", ("u16",)),
    0x00A7: ScriptCommandSpec("WaitSound", ("u16",)),
    0x00A9: ScriptCommandSpec("FadeSound", ("u16",)),
    0x00AB: ScriptCommandSpec("StoreSpritePosition", ("u32",)),
    0x00AC: ScriptCommandSpec("ApplyVisualEffect", ("u16",)),
    0x00CB: ScriptCommandSpec("Warp", ("u16", "u16")),
    0x00CC: ScriptCommandSpec("StoreMapId", ("u16",)),
    0x00FE: ScriptCommandSpec("SetRivalNameSource", ("u16",)),
    0x01AD: ScriptCommandSpec("ApplyItemEffect", ("u16", "u16")),
    0x01C2: ScriptCommandSpec("PrepareSoundCue", ("u16", "u16")),
    0x01C4: ScriptCommandSpec("StoreTextVariable", ("u16", "u16")),
}


def decode_script_file(payload: bytes, *, program_id: str = "script-program") -> DecodedScriptFile:
    if not payload:
        return DecodedScriptFile(
            header_marker_offset=None,
            header_entries=(),
            procedures=(),
            dialogue_lines=(),
        )

    if is_zero_filled_payload(payload):
        return DecodedScriptFile(
            header_marker_offset=None,
            header_entries=(),
            procedures=(),
            dialogue_lines=(),
        )

    header_entries, marker_offset = parse_header_entries(payload)
    if not header_entries:
        raise ValueError("Script payload did not contain any header entries before the header terminator.")

    procedures: list[DecodedScriptProcedure] = []
    dialogue_lines: list[DecodedScriptDialogueLine] = []
    warnings: list[str] = []
    visited_offsets: set[int] = set()
    queued_targets: list[tuple[int, str, int | None]] = [
        (entry.start_offset, "script", entry.header_index) for entry in header_entries
    ]
    discovered_function_count = 0

    while queued_targets:
        start_offset, entry_kind, header_index = queued_targets.pop(0)
        if start_offset in visited_offsets:
            continue

        visited_offsets.add(start_offset)
        if entry_kind == "script":
            procedure_id = f"{program_id}:script:{header_index}"
        else:
            discovered_function_count += 1
            procedure_id = f"{program_id}:function:{discovered_function_count}"

        procedure, branch_targets, procedure_warnings = parse_procedure(
            payload,
            start_offset=start_offset,
            entry_kind=entry_kind,
            header_index=header_index,
            procedure_id=procedure_id,
        )
        procedures.append(procedure)
        dialogue_lines.extend(procedure.dialogue_lines)
        warnings.extend(procedure_warnings)

        for branch_target in branch_targets:
            if branch_target not in visited_offsets:
                queued_targets.append((branch_target, "function", None))

    procedures.sort(key=lambda procedure: (procedure.start_offset, procedure.procedure_id))
    dialogue_lines.sort(key=lambda line: (line.instruction_offset, line.line_id))
    return DecodedScriptFile(
        header_marker_offset=marker_offset,
        header_entries=tuple(header_entries),
        procedures=tuple(procedures),
        dialogue_lines=tuple(dialogue_lines),
        warnings=tuple(warnings),
    )


def parse_header_entries(payload: bytes) -> tuple[tuple[DecodedScriptHeaderEntry, ...], int]:
    cursor = 0
    header_index = 1
    entries: list[DecodedScriptHeaderEntry] = []

    while True:
        if cursor + 2 > len(payload):
            raise ValueError("Script payload ended before the header terminator was found.")

        if read_u16(payload, cursor) == _HEADER_TERMINATOR:
            marker_offset = cursor
            break

        if cursor + 4 > len(payload):
            raise ValueError("Script payload is truncated in the middle of a header entry.")

        stored_offset = read_u32(payload, cursor)
        start_offset = resolve_header_entry_start_offset(payload, cursor, stored_offset)
        if start_offset < 0 or start_offset >= len(payload):
            raise ValueError(
                f"Script header entry '{header_index}' resolved to start offset '{start_offset}', which is outside the payload."
            )

        entries.append(
            DecodedScriptHeaderEntry(
                header_index=header_index,
                header_offset=cursor,
                stored_offset=stored_offset,
                start_offset=start_offset,
            )
        )
        header_index += 1
        cursor += 4

    return tuple(entries), marker_offset


def resolve_header_entry_start_offset(payload: bytes, header_offset: int, stored_offset: int) -> int:
    raw_start_offset = header_offset + 4 + stored_offset
    if raw_start_offset < 0 or raw_start_offset + 2 > len(payload):
        return raw_start_offset

    opcode = read_u16(payload, raw_start_offset)
    if opcode in COMMAND_SPECS and not COMMAND_SPECS[opcode].is_terminal:
        return raw_start_offset

    local_jump = read_u16(payload, raw_start_offset)
    candidate_start = raw_start_offset + local_jump
    if local_jump <= 0 or candidate_start + 2 > len(payload):
        return raw_start_offset

    candidate_opcode = read_u16(payload, candidate_start)
    if candidate_opcode in COMMAND_SPECS:
        return candidate_start

    return raw_start_offset


def parse_procedure(
    payload: bytes,
    *,
    start_offset: int,
    entry_kind: str,
    header_index: int | None,
    procedure_id: str,
) -> tuple[DecodedScriptProcedure, tuple[int, ...], tuple[str, ...]]:
    if start_offset < 0 or start_offset >= len(payload):
        raise ValueError(f"Procedure '{procedure_id}' start offset '{start_offset}' is outside the payload.")

    cursor = start_offset
    instructions: list[DecodedScriptInstruction] = []
    branch_targets: list[int] = []
    dialogue_lines: list[DecodedScriptDialogueLine] = []
    warnings: list[str] = []
    parse_status = "complete"

    while cursor + 2 <= len(payload):
        opcode = read_u16(payload, cursor)
        spec = COMMAND_SPECS.get(opcode)
        if spec is None:
            instructions.append(
                DecodedScriptInstruction(
                    offset=cursor,
                    opcode=opcode,
                    mnemonic=f"Unknown_{opcode:04X}",
                    byte_length=2,
                )
            )
            warnings.append(
                f"Procedure '{procedure_id}' stopped at unknown opcode 0x{opcode:04X} at offset {cursor}."
            )
            parse_status = "unknownOpcode"
            cursor += 2
            break

        operands: list[int] = []
        operand_cursor = cursor + 2
        try:
            for operand_kind in spec.operand_kinds:
                operand_value, operand_cursor = read_operand(payload, operand_cursor, operand_kind)
                operands.append(operand_value)
        except ValueError as exc:
            warnings.append(
                f"Procedure '{procedure_id}' has a truncated operand payload for opcode 0x{opcode:04X} at offset {cursor}: {exc}"
            )
            parse_status = "truncatedOperand"
            break

        branch_target = None
        if spec.branch_operand_index is not None:
            branch_delta = operands[spec.branch_operand_index]
            if spec.branch_is_relative_to_instruction:
                branch_target = cursor + branch_delta
            else:
                branch_target = branch_delta

            if 0 <= branch_target < len(payload):
                branch_targets.append(branch_target)
            else:
                warnings.append(
                    f"Procedure '{procedure_id}' computed out-of-range branch target '{branch_target}' for opcode 0x{opcode:04X} at offset {cursor}."
                )
                parse_status = "invalidBranchTarget"
                branch_target = None

        instruction = DecodedScriptInstruction(
            offset=cursor,
            opcode=opcode,
            mnemonic=spec.mnemonic,
            byte_length=operand_cursor - cursor,
            operands=tuple(operands),
            branch_target_offset=branch_target,
        )
        instructions.append(instruction)

        if spec.dialogue_kind is not None:
            dialogue_lines.append(
                build_dialogue_line(
                    procedure_id=procedure_id,
                    instruction=instruction,
                    dialogue_kind=spec.dialogue_kind,
                )
            )

        cursor = operand_cursor
        if spec.is_terminal:
            break
    else:
        if cursor != len(payload):
            parse_status = "truncatedOperand"

    if not instructions and parse_status == "complete":
        parse_status = "empty"

    return (
        DecodedScriptProcedure(
            procedure_id=procedure_id,
            entry_kind=entry_kind,
            header_index=header_index,
            start_offset=start_offset,
            end_offset=cursor,
            parse_status=parse_status,
            instructions=tuple(instructions),
            dialogue_lines=tuple(dialogue_lines),
        ),
        tuple(sorted(set(branch_targets))),
        tuple(warnings),
    )


def build_dialogue_line(
    *,
    procedure_id: str,
    instruction: DecodedScriptInstruction,
    dialogue_kind: str,
) -> DecodedScriptDialogueLine:
    if instruction.opcode == 0x003C:
        message_id = instruction.operands[2]
        speaker_object_id = instruction.operands[3]
        view_type = instruction.operands[4]
        message_type = instruction.operands[5]
        variant_a = instruction.operands[0]
        variant_b = instruction.operands[1]
    elif instruction.opcode == 0x003D:
        message_id = instruction.operands[2]
        speaker_object_id = None
        view_type = instruction.operands[3]
        message_type = instruction.operands[4]
        variant_a = instruction.operands[0]
        variant_b = instruction.operands[1]
    else:
        raise ValueError(f"Opcode 0x{instruction.opcode:04X} does not map to a dialogue line.")

    return DecodedScriptDialogueLine(
        line_id=f"{procedure_id}:{instruction.offset}",
        procedure_id=procedure_id,
        instruction_offset=instruction.offset,
        command=dialogue_kind,
        message_id=message_id,
        speaker_object_id=speaker_object_id,
        view_type=view_type,
        message_type=message_type,
        variant_a=variant_a,
        variant_b=variant_b,
    )


def read_operand(payload: bytes, offset: int, operand_kind: str) -> tuple[int, int]:
    if operand_kind == "u8":
        if offset + 1 > len(payload):
            raise ValueError("expected u8 operand")
        return payload[offset], offset + 1

    if operand_kind == "u16":
        if offset + 2 > len(payload):
            raise ValueError("expected u16 operand")
        return read_u16(payload, offset), offset + 2

    if operand_kind == "u32":
        if offset + 4 > len(payload):
            raise ValueError("expected u32 operand")
        return read_u32(payload, offset), offset + 4

    if operand_kind == "s32":
        if offset + 4 > len(payload):
            raise ValueError("expected s32 operand")
        return int.from_bytes(payload[offset : offset + 4], "little", signed=True), offset + 4

    raise ValueError(f"Unsupported operand kind '{operand_kind}'.")


def read_u16(payload: bytes, offset: int) -> int:
    return int.from_bytes(payload[offset : offset + 2], "little")


def read_u32(payload: bytes, offset: int) -> int:
    return int.from_bytes(payload[offset : offset + 4], "little")


def is_zero_filled_payload(payload: bytes) -> bool:
    return not payload or all(value == 0 for value in payload)
