from __future__ import annotations

from dataclasses import dataclass
from typing import Iterable


_TEXT_TERMINATOR = 0xFFFF
_TEXT_NEWLINE = 0xFFFE
_TEXT_COMPRESSED_MARKER = 0xF100
_TEXT_CONTROL_MARKER = 0xF000
_TEXT_PAGE_BREAK = 0xBE00
_TEXT_CARRIAGE_RETURN = 0xBE01


@dataclass(frozen=True)
class DecodedTextToken:
    kind: str
    text: str = ""
    code_point: int | None = None
    control_code: int | None = None
    arguments: tuple[int, ...] = ()

    def to_dict(self) -> dict[str, object]:
        payload: dict[str, object] = {
            "kind": self.kind,
        }
        if self.text:
            payload["text"] = self.text
        if self.code_point is not None:
            payload["codePoint"] = self.code_point
        if self.control_code is not None:
            payload["controlCode"] = self.control_code
        if self.arguments:
            payload["arguments"] = list(self.arguments)

        return payload


@dataclass(frozen=True)
class DecodedTextMessage:
    block_index: int
    entry_index: int
    char_count: int
    flags: int
    is_compressed: bool
    text: str
    tokens: tuple[DecodedTextToken, ...]

    def to_dict(self) -> dict[str, object]:
        return {
            "blockIndex": self.block_index,
            "charCount": self.char_count,
            "entryIndex": self.entry_index,
            "flags": self.flags,
            "isCompressed": self.is_compressed,
            "text": self.text,
            "tokens": [token.to_dict() for token in self.tokens],
        }


@dataclass(frozen=True)
class DecodedTextBank:
    block_count: int
    messages_per_block: int
    messages: tuple[DecodedTextMessage, ...]

    @property
    def message_count(self) -> int:
        return len(self.messages)

    def to_dict(self) -> dict[str, object]:
        return {
            "blockCount": self.block_count,
            "messageCount": self.message_count,
            "messages": [message.to_dict() for message in self.messages],
            "messagesPerBlock": self.messages_per_block,
        }


def decode_text_bank(payload: bytes) -> DecodedTextBank:
    if len(payload) < 16:
        raise ValueError("Text bank payload is too small to contain a valid header.")

    block_count = read_u16(payload, 0)
    messages_per_block = read_u16(payload, 2)
    if block_count <= 0:
        raise ValueError("Text bank payload must contain at least one block.")

    header_size = 12 + (block_count * 4)
    if len(payload) < header_size:
        raise ValueError("Text bank payload is truncated before the block offset table.")

    block_offsets = [read_u32(payload, 12 + (index * 4)) for index in range(block_count)]
    messages: list[DecodedTextMessage] = []

    for block_index, block_offset in enumerate(block_offsets):
        if block_offset < header_size or block_offset >= len(payload):
            raise ValueError(f"Text block '{block_index}' offset '{block_offset}' falls outside the payload.")

        entries_table_offset = block_offset + 4
        required_table_size = entries_table_offset + (messages_per_block * 8)
        if required_table_size > len(payload):
            raise ValueError(f"Text block '{block_index}' entry table exceeds the payload length.")

        for entry_index in range(messages_per_block):
            table_offset = entries_table_offset + (entry_index * 8)
            entry_offset = read_u32(payload, table_offset)
            char_count = read_u16(payload, table_offset + 4)
            flags = read_u16(payload, table_offset + 6)
            encrypted_words = read_words(payload, block_offset + entry_offset, char_count)
            decrypted_words = decrypt_words(encrypted_words)
            is_compressed = bool(decrypted_words) and decrypted_words[0] == _TEXT_COMPRESSED_MARKER
            if is_compressed:
                decrypted_words = decompress_9bit(decrypted_words[1:])

            tokens = parse_text_tokens(decrypted_words)
            messages.append(
                DecodedTextMessage(
                    block_index=block_index,
                    entry_index=entry_index,
                    char_count=char_count,
                    flags=flags,
                    is_compressed=is_compressed,
                    text=render_tokens(tokens),
                    tokens=tokens,
                )
            )

    return DecodedTextBank(
        block_count=block_count,
        messages_per_block=messages_per_block,
        messages=tuple(messages),
    )


def encode_text_bank(messages: Iterable[str], *, flags: int = 0) -> bytes:
    word_sequences = []
    for message in messages:
        word_sequences.append([ord(character) for character in message])

    return encode_text_bank_from_words(word_sequences, flags=flags)


def encode_text_bank_from_words(messages: Iterable[Iterable[int]], *, flags: int = 0) -> bytes:
    message_list = [list(message) for message in messages]
    encoded_messages: list[bytes] = []
    table_entries: list[tuple[int, int, int]] = []
    current_offset = 4 + (len(message_list) * 8)

    for words in message_list:
        words = list(words)
        if not words or words[-1] != _TEXT_TERMINATOR:
            words.append(_TEXT_TERMINATOR)
        encrypted = encrypt_words(words)
        encoded = b"".join(word.to_bytes(2, "little") for word in encrypted)
        encoded_messages.append(encoded)
        table_entries.append((current_offset, len(encrypted), flags))
        current_offset += len(encoded)

    block_size = current_offset
    payload = bytearray()
    payload.extend((1).to_bytes(2, "little"))
    payload.extend((len(message_list)).to_bytes(2, "little"))
    payload.extend((12 + 4 + block_size).to_bytes(4, "little"))
    payload.extend((0).to_bytes(4, "little"))
    payload.extend((16).to_bytes(4, "little"))
    payload.extend(block_size.to_bytes(4, "little"))

    for entry_offset, char_count, entry_flags in table_entries:
        payload.extend(entry_offset.to_bytes(4, "little"))
        payload.extend(char_count.to_bytes(2, "little"))
        payload.extend(entry_flags.to_bytes(2, "little"))

    for encoded_message in encoded_messages:
        payload.extend(encoded_message)

    return bytes(payload)


def read_words(payload: bytes, start_offset: int, count: int) -> list[int]:
    end_offset = start_offset + (count * 2)
    if start_offset < 0 or end_offset > len(payload):
        raise ValueError(
            f"Text message offset '{start_offset}' with char count '{count}' exceeds the payload length '{len(payload)}'."
        )

    return [read_u16(payload, start_offset + (index * 2)) for index in range(count)]


def decrypt_words(encrypted_words: list[int]) -> list[int]:
    if not encrypted_words:
        return []

    key = encrypted_words[-1] ^ _TEXT_TERMINATOR
    decrypted_words: list[int] = []
    for encrypted_word in reversed(encrypted_words):
        decrypted_words.insert(0, encrypted_word ^ key)
        key = rotate_right_16(key, 3)

    return decrypted_words


def encrypt_words(words: list[int], *, initial_key: int = 0x7C89) -> list[int]:
    if not words:
        return []

    key = initial_key & 0xFFFF
    encrypted_words: list[int] = []
    for word in words:
        encrypted_words.append((word ^ key) & 0xFFFF)
        key = rotate_left_16(key, 3)

    return encrypted_words


def decompress_9bit(words: list[int]) -> list[int]:
    unpacked_words: list[int] = []
    container = 0
    bit_count = 0

    for word in words:
        container |= word << bit_count
        bit_count += 16

        while bit_count >= 9:
            bit_count -= 9
            value = container & 0x1FF
            unpacked_words.append(_TEXT_TERMINATOR if value == 0x1FF else value)
            container >>= 9

    return unpacked_words


def render_text(words: list[int]) -> str:
    return render_tokens(parse_text_tokens(words))


def parse_text_tokens(words: list[int]) -> tuple[DecodedTextToken, ...]:
    tokens: list[DecodedTextToken] = []
    pending_text_parts: list[str] = []
    queue = list(words)

    while queue:
        word = queue.pop(0)
        if word == _TEXT_TERMINATOR:
            break

        if word == _TEXT_NEWLINE:
            flush_text_token(tokens, pending_text_parts)
            tokens.append(DecodedTextToken(kind="lineBreak"))
            continue

        if word == _TEXT_CONTROL_MARKER:
            flush_text_token(tokens, pending_text_parts)
            tokens.append(parse_control_token(queue))
            continue

        if is_renderable_unicode_word(word):
            pending_text_parts.append(chr(word))
            continue

        flush_text_token(tokens, pending_text_parts)
        tokens.append(DecodedTextToken(kind="rawCodePoint", code_point=word))

    flush_text_token(tokens, pending_text_parts)
    return tuple(tokens)


def parse_control_token(queue: list[int]) -> DecodedTextToken:
    if len(queue) < 2:
        raise ValueError("Encountered a truncated control sequence in decoded text data.")

    control_kind = queue.pop(0)
    argument_count = queue.pop(0)
    if control_kind == _TEXT_PAGE_BREAK and argument_count == 0:
        return DecodedTextToken(kind="pageBreak")

    if control_kind == _TEXT_CARRIAGE_RETURN and argument_count == 0:
        return DecodedTextToken(kind="carriageReturn")

    arguments = [control_kind]
    for _ in range(argument_count):
        if not queue:
            raise ValueError("Control sequence argument count exceeded the remaining decoded text payload.")
        arguments.append(queue.pop(0))

    return DecodedTextToken(
        kind="variable",
        control_code=control_kind,
        arguments=tuple(arguments[1:]),
    )


def render_tokens(tokens: tuple[DecodedTextToken, ...] | list[DecodedTextToken]) -> str:
    rendered_parts: list[str] = []
    for token in tokens:
        if token.kind == "text":
            rendered_parts.append(token.text)
        elif token.kind == "lineBreak":
            rendered_parts.append("\\n")
        elif token.kind == "pageBreak":
            rendered_parts.append("\\f")
        elif token.kind == "carriageReturn":
            rendered_parts.append("\\r")
        elif token.kind == "variable":
            arguments = [str(token.control_code)] if token.control_code is not None else []
            arguments.extend(str(argument) for argument in token.arguments)
            rendered_parts.append(f"VAR({', '.join(arguments)})")
        elif token.kind == "rawCodePoint":
            if token.code_point is None:
                raise ValueError("rawCodePoint token requires a code_point value.")
            rendered_parts.append(f"\\x{token.code_point:04X}")
        else:
            raise ValueError(f"Unsupported token kind '{token.kind}'.")

    return "".join(rendered_parts)


def flush_text_token(tokens: list[DecodedTextToken], pending_text_parts: list[str]) -> None:
    if not pending_text_parts:
        return

    tokens.append(DecodedTextToken(kind="text", text="".join(pending_text_parts)))
    pending_text_parts.clear()


def is_renderable_unicode_word(word: int) -> bool:
    if word < 0x20 or word == _TEXT_CONTROL_MARKER or word == _TEXT_NEWLINE or word == _TEXT_TERMINATOR:
        return False

    return not (0xD800 <= word <= 0xDFFF)


def read_u16(payload: bytes, offset: int) -> int:
    return int.from_bytes(payload[offset : offset + 2], "little")


def read_u32(payload: bytes, offset: int) -> int:
    return int.from_bytes(payload[offset : offset + 4], "little")


def rotate_left_16(value: int, amount: int) -> int:
    amount &= 15
    return ((value << amount) | (value >> (16 - amount))) & 0xFFFF


def rotate_right_16(value: int, amount: int) -> int:
    amount &= 15
    return ((value >> amount) | (value << (16 - amount))) & 0xFFFF
