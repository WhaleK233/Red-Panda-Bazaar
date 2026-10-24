#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""ValidateRepo.py 纯函数部分的回归测试。"""

from __future__ import annotations

import importlib.util
import json
import unittest
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[1] / "scripts" / "ValidateRepo.py"


def load_module():
    spec = importlib.util.spec_from_file_location("validate_repo", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


class JsoncTest(unittest.TestCase):
    def setUp(self):
        self.module = load_module()

    def parse(self, text: str):
        return json.loads(
            self.module.blank_trailing_commas(self.module.blank_comments(text)),
            object_pairs_hook=self.module.unique_object,
        )

    def test_comments_and_trailing_commas_are_blanked(self):
        source = """{
            // 行注释
            "a": 1, /* 块
            注释 */
            "b": [2, 3,],
        }"""
        self.assertEqual(self.parse(source), {"a": 1, "b": [2, 3]})

    def test_comment_markers_inside_strings_are_preserved(self):
        source = '{"a": "// 不是注释", "b": "/* 也不是注释",}'
        self.assertEqual(self.parse(source), {"a": "// 不是注释", "b": "/* 也不是注释"})

    def test_duplicate_keys_are_rejected(self):
        with self.assertRaises(ValueError):
            self.parse('{"a": 1, "a": 2}')

    def test_comment_blanking_keeps_line_numbers(self):
        source = 'line1\n// comment\nline3'
        blanked = self.module.blank_comments(source)
        self.assertEqual(blanked.splitlines(), ["line1", " " * len("// comment"), "line3"])


if __name__ == "__main__":
    unittest.main()
