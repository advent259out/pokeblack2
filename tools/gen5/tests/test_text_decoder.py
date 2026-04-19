from __future__ import annotations

import unittest
from pathlib import Path

import ndspy.narc

from tools.gen5.contract import repo_root
from tools.gen5.text_decoder import (
    decode_text_bank,
    encode_text_bank,
    encode_text_bank_from_words,
)


class TextDecoderTests(unittest.TestCase):
    def test_decode_text_bank_round_trips_synthetic_messages(self) -> None:
        payload = encode_text_bank(["Hello", "World"], flags=74)

        decoded = decode_text_bank(payload)

        self.assertEqual(decoded.block_count, 1)
        self.assertEqual(decoded.messages_per_block, 2)
        self.assertEqual(decoded.message_count, 2)
        self.assertEqual(decoded.messages[0].text, "Hello")
        self.assertEqual(decoded.messages[1].text, "World")
        self.assertEqual(decoded.messages[0].flags, 74)
        self.assertFalse(decoded.messages[0].is_compressed)
        self.assertEqual(decoded.messages[0].tokens[0].kind, "text")
        self.assertEqual(decoded.messages[0].tokens[0].text, "Hello")

    def test_decode_text_bank_emits_structured_control_tokens(self) -> None:
        payload = encode_text_bank_from_words(
            [
                [
                    ord("A"),
                    0xFFFE,
                    0xF000,
                    0x1234,
                    2,
                    7,
                    9,
                    0xF000,
                    0xBE00,
                    0,
                    0xF000,
                    0xBE01,
                    0,
                    0xFF2E,
                ]
            ],
            flags=216,
        )

        decoded = decode_text_bank(payload)
        message = decoded.messages[0]

        self.assertEqual(message.text, "A\\nVAR(4660, 7, 9)\\f\\rＮ")
        self.assertEqual(
            [token.kind for token in message.tokens],
            ["text", "lineBreak", "variable", "pageBreak", "carriageReturn", "text"],
        )
        self.assertEqual(message.tokens[2].control_code, 0x1234)
        self.assertEqual(message.tokens[2].arguments, (7, 9))
        self.assertEqual(message.tokens[5].text, "Ｎ")

    def test_decode_text_bank_reads_known_canonical_europe_strings(self) -> None:
        raw_path = repo_root() / "External" / "Exports" / "BlackWhite" / "M0" / "raw" / "narc" / "a" / "0" / "0" / "2"
        if not raw_path.is_file():
            self.skipTest(f"Canonical text archive is missing at '{raw_path}'.")

        narc = ndspy.narc.NARC(raw_path.read_bytes())
        system_text_bank = decode_text_bank(narc.files[0])
        compressed_name_bank = decode_text_bank(narc.files[176])

        self.assertEqual(system_text_bank.messages[0].text, "No Answer")
        self.assertEqual(system_text_bank.messages[1].text, "Black")
        self.assertEqual(compressed_name_bank.messages[1].text, "Cheren")
        self.assertTrue(compressed_name_bank.messages[1].is_compressed)
        self.assertEqual(compressed_name_bank.messages[1].tokens[0].kind, "text")


if __name__ == "__main__":
    unittest.main()
