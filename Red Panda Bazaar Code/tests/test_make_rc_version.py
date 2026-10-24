#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""MakeRcVersion.py 的 RC 版本号派生测试。"""

from __future__ import annotations

import importlib.util
import subprocess
import tempfile
import sys
import unittest
from contextlib import contextmanager
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[1] / "scripts" / "MakeRcVersion.py"
TEST_DIR = Path(__file__).resolve().parent


@contextmanager
def temporary_csproj(content: str):
    # NamedTemporaryFile(newline='') 绕过本机临时目录/新目录的 ACL 限制
    with tempfile.NamedTemporaryFile(
        dir=TEST_DIR, suffix=".csproj", mode="w", encoding="utf-8", newline="", delete=False
    ) as handle:
        handle.write(content)
        path = Path(handle.name)
    try:
        yield path
    finally:
        path.unlink()


class MakeRcVersionTest(unittest.TestCase):
    def run_script(self, *args: str) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [sys.executable, str(SCRIPT), *args],
            capture_output=True,
            text=True,
            encoding="utf-8",
        )

    def test_derives_rc_version_from_csproj(self):
        with temporary_csproj("<Project><Version>1.2.0-Beta</Version></Project>") as csproj:
            result = self.run_script("--csproj", str(csproj), "--date", "20261024", "--run", "7")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout, "1.2.0-rc.20261024.7\n")

    def test_invalid_version_fails(self):
        with temporary_csproj("<Project><Version>beta</Version></Project>") as csproj:
            result = self.run_script("--csproj", str(csproj))
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Error!", result.stderr)

    def test_read_base_version_accepts_suffix(self):
        spec = importlib.util.spec_from_file_location("make_rc_version", SCRIPT)
        module = importlib.util.module_from_spec(spec)
        assert spec.loader is not None
        spec.loader.exec_module(module)
        with temporary_csproj("<Version>2.0.1-RC.1</Version>") as csproj:
            self.assertEqual(module.read_base_version(csproj), "2.0.1")


if __name__ == "__main__":
    unittest.main()
