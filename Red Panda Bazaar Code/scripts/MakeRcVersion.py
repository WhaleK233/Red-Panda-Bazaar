#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""计算持续发布（CD）构建的 RC 版本号。

版本号的唯一来源是 csproj 的 <Version>（本地开发格式 x.y.z-beta）。
本脚本取出其中的语义化部分，拼成 CD 专用版本号：

    x.y.z-rc.<日期>.<运行号>
      x.y.z     csproj <Version> 的语义化部分（忽略 -beta 等预发布后缀）
      <日期>    UTC 日期 YYYYMMDD，人读友好
      <运行号>  GitHub Actions 的 run number，仓库内单调递增，保证同日多次构建唯一

只输出到 stdout 的单独一行，便于在 GitHub Actions 中用 $(...) 捕获，再通过
`-p:Version=...` 注入构建；**不改写任何文件**，因此 CI 检出副本保持干净。
"""

from __future__ import annotations

import argparse
import datetime
import os
import re
import sys
from pathlib import Path

CODE_DIR = Path(__file__).resolve().parents[1]
CSPROJ = CODE_DIR / "Red Panda Bazaar Code.csproj"
VERSION_RE = re.compile(r"<Version>\s*([^<\s]+)\s*</Version>")
BASE_RE = re.compile(r"^(\d+\.\d+\.\d+)")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="计算 x.y.z-rc.<日期>.<运行号> 形式的 RC 版本号")
    parser.add_argument("--csproj", type=Path, default=CSPROJ, help="csproj 路径")
    parser.add_argument(
        "--run",
        default=os.environ.get("GITHUB_RUN_NUMBER", "0"),
        help="运行号；默认取环境变量 GITHUB_RUN_NUMBER",
    )
    parser.add_argument(
        "--date",
        default=datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%d"),
        help="日期 YYYYMMDD；默认取 UTC 今天",
    )
    return parser.parse_args()


def read_base_version(path: Path) -> str:
    """从 csproj 读 <Version> 并取出 x.y.z 部分。"""
    text = path.read_text(encoding="utf-8-sig")
    match = VERSION_RE.search(text)
    if match is None:
        raise ValueError(f"{path.name} 中没有找到 <Version> 属性")
    base = BASE_RE.match(match.group(1))
    if base is None:
        raise ValueError(f"<Version> {match.group(1)!r} 不以 x.y.z 开头，无法生成 RC 版本号")
    return base.group(1)


def main() -> int:
    # 版本号要被 $(...) 捕获后原样注入 MSBuild；Windows 上文本模式会把 \n 转成 \r\n，
    # 残留的 \r 会混进 manifest.json 破坏 JSON，因此强制 stdout 用 \n
    try:
        sys.stdout.reconfigure(newline="\n")
    except (AttributeError, ValueError):
        pass

    args = parse_args()
    try:
        base = read_base_version(args.csproj)
    except (OSError, ValueError) as error:
        print(f"Error! {error}", file=sys.stderr)
        return 1

    print(f"{base}-rc.{args.date}.{args.run}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
