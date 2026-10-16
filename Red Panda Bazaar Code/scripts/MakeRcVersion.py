#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成持续发布（CD）构建的 RC 版本号。

把 manifest.json 的版本号改写为：x.y.z-rc.Build.c
  x.y.z  原版本号的语义化部分（忽略 -beta.Build.N 等预发布后缀）
  c      当前提交在分支上的序号（提交次数）

新版本号只写到 stdout 的单独一行，便于在 GitHub Actions 中用 $(...) 捕获；
默认同时写回 manifest.json，`--dry-run` 则只输出不写回。

注意：改写只发生在 CI/CD 的检出副本上，绝不提交回仓库。
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

CODE_DIR = Path(__file__).resolve().parents[1]
MANIFEST = CODE_DIR / "manifest.json"
BASE_RE = re.compile(r"^(\d+\.\d+\.\d+)")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="生成 x.y.z-rc.Build.c 形式的 RC 版本号")
    parser.add_argument("--manifest", type=Path, default=MANIFEST, help="manifest.json 路径")
    parser.add_argument("--commit-count", type=int, help="提交次数；默认取 git rev-list --count HEAD")
    parser.add_argument("--dry-run", action="store_true", help="只输出新版本号，不写回 manifest")
    return parser.parse_args()


def read_manifest(path: Path) -> dict:
    with path.open(encoding="utf-8-sig") as handle:
        return json.load(handle)


def write_version(path: Path, manifest: dict, version: str) -> None:
    """写回 manifest，保留原文件的换行风格（仓库内为 CRLF）。"""
    raw = path.read_bytes()
    newline = "\r\n" if b"\r\n" in raw else "\n"
    manifest["Version"] = version
    text = json.dumps(manifest, ensure_ascii=False, indent=2)
    path.write_text(text.replace("\n", newline), encoding="utf-8")


def count_commits() -> int:
    """取当前分支的提交总数；要求完整历史（浅克隆会得到错误结果）。"""
    result = subprocess.run(
        ["git", "rev-list", "--count", "HEAD"],
        capture_output=True,
        text=True,
        check=True,
    )
    return int(result.stdout.strip())


def main() -> int:
    args = parse_args()
    if not args.manifest.exists():
        print(f"Error! {args.manifest} not found!", file=sys.stderr)
        return 1

    manifest = read_manifest(args.manifest)
    version = str(manifest.get("Version", ""))
    match = BASE_RE.match(version)
    if match is None:
        print(f"Error! 版本号 {version!r} 不以 x.y.z 开头，无法生成 RC 版本号", file=sys.stderr)
        return 1

    count = args.commit_count if args.commit_count is not None else count_commits()
    new_version = f"{match.group(1)}-rc.Build.{count}"

    if not args.dry_run:
        write_version(args.manifest, manifest, new_version)

    print(new_version)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
