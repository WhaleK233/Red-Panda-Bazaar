#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""对当前检出副本运行完整仓库校验，作为 CI 的冒烟测试。"""

from __future__ import annotations

import subprocess
import sys
import unittest
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[1] / "scripts" / "ValidateRepo.py"


class RepoSmokeTest(unittest.TestCase):
    def test_validate_repo_passes(self):
        result = subprocess.run(
            [sys.executable, str(SCRIPT)],
            capture_output=True,
            text=True,
            encoding="utf-8",
        )
        self.assertEqual(
            result.returncode,
            0,
            f"ValidateRepo.py failed:\n{result.stdout}\n{result.stderr}",
        )


if __name__ == "__main__":
    unittest.main()
