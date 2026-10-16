#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Red Panda Bazaar 仓库一致性校验。

CI（.github/workflows/ci.yml）与本地共用同一入口：

    python "Red Panda Bazaar Code/scripts/ValidateRepo.py"

校验内容：
  1. JSON / JSONC 文件可解析、无重复键（SMAPI 允许注释与尾逗号）
  2. i18n 的 default.json 与 zh.json 的键集合、键顺序、占位符集合一致
  3. Constant/I18nKeys.cs 中的常量与 default.json 的键双向一一对应
  4. manifest.json 的必填字段、版本号格式、EntryDll、对 CP 包的 required 依赖
  5. csproj 的 TargetFramework 恒为 net6.0
  6. PR 不得改动只读的 [CP]Red Panda Bazaar/

环境变量（CI 注入；本地缺失时自动跳过依赖它们的检查）：
  BASE_SHA          PR 基线提交，用于第 6 项
  ALLOW_CP_CHANGE   "1" 时豁免第 6 项（CP 上游同步）
  GITHUB_ACTIONS    存在时按 GitHub Actions 注解格式输出
  GITHUB_STEP_SUMMARY  存在时追加 markdown 摘要

退出码：0 全部通过；1 存在失败项。
"""

from __future__ import annotations

import json
import os
import re
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
CODE_DIR = REPO_ROOT / "Red Panda Bazaar Code"
CP_DIR = REPO_ROOT / "[CP]Red Panda Bazaar"

I18N_DEFAULT = CODE_DIR / "i18n" / "default.json"
I18N_ZH = CODE_DIR / "i18n" / "zh.json"
I18N_KEYS_CS = CODE_DIR / "Constant" / "I18nKeys.cs"
MOD_MANIFEST = CODE_DIR / "manifest.json"
CSPROJ = CODE_DIR / "Red Panda Bazaar Code.csproj"
CP_MANIFEST = CP_DIR / "manifest.json"
CP_CONTENT = CP_DIR / "content.json"
CP_CONFIG = CP_DIR / "config.json"

JSON_FILES = (MOD_MANIFEST, I18N_DEFAULT, I18N_ZH, CP_MANIFEST, CP_CONTENT, CP_CONFIG)

# 只读内容包的路径限定符：路径中的方括号会被 git 当作通配符，须显式声明为字面量
CP_READONLY_PATHSPEC = ":(literal)[CP]Red Panda Bazaar"

PLACEHOLDER_RE = re.compile(r"\{\{([^{}]+)\}\}")
CONST_RE = re.compile(r'public\s+const\s+string\s+(\w+)\s*=\s*"((?:[^"\\]|\\.)*)"\s*;')

CI = bool(os.environ.get("GITHUB_ACTIONS"))
failures: list[tuple[str | None, int | None, str]] = []


def rel(path: Path | None) -> str | None:
    return None if path is None else path.relative_to(REPO_ROOT).as_posix()


def line_at(text: str, offset: int) -> int:
    return text.count("\n", 0, offset) + 1


def line_of_match(text: str, pattern: re.Pattern[str]) -> int | None:
    match = pattern.search(text)
    return None if match is None else line_at(text, match.start())


def fail(path: Path | None, message: str, line: int | None = None) -> None:
    """登记一条失败项；在 GitHub Actions 中同时发出 error 注解。"""
    location = rel(path)
    failures.append((location, line, message))
    if CI:
        where = ""
        if location:
            where += f" file={location}"
        if line is not None:
            where += f",line={line}"
        print(f"::error{where}::{message}")
    else:
        prefix = f"{location}:{line}: " if location and line else (f"{location}: " if location else "")
        print(f"[FAIL] {prefix}{message}")


def ok(message: str) -> None:
    print(f"[ OK ] {message}")


def skip(message: str) -> None:
    print(f"[SKIP] {message}")


# --------------------------------------------------------------------------- #
# JSON（允许注释与尾逗号）
# --------------------------------------------------------------------------- #

def blank_comments(text: str) -> str:
    """把 // 与 /* */ 注释替换为等长空白，保持行列号不变。"""
    chars = list(text)
    i, n = 0, len(text)
    in_string = False
    while i < n:
        ch = text[i]
        if in_string:
            if ch == "\\":
                i += 2
                continue
            if ch == '"':
                in_string = False
            i += 1
            continue
        if ch == '"':
            in_string = True
            i += 1
            continue
        if ch == "/" and i + 1 < n and text[i + 1] == "*":
            end = text.find("*/", i + 2)
            end = n if end < 0 else end + 2
            for k in range(i, end):
                if chars[k] != "\n":
                    chars[k] = " "
            i = end
            continue
        if ch == "/" and i + 1 < n and text[i + 1] == "/":
            end = text.find("\n", i)
            end = n if end < 0 else end
            for k in range(i, end):
                chars[k] = " "
            i = end
            continue
        i += 1
    return "".join(chars)


def blank_trailing_commas(text: str) -> str:
    """把对象 / 数组末尾的多余逗号替换为空格，保持行列号不变。"""
    chars = list(text)
    i, n = 0, len(text)
    in_string = False
    while i < n:
        ch = text[i]
        if in_string:
            if ch == "\\":
                i += 2
                continue
            if ch == '"':
                in_string = False
            i += 1
            continue
        if ch == '"':
            in_string = True
            i += 1
            continue
        if ch == ",":
            j = i + 1
            while j < n and text[j] in " \t\r\n":
                j += 1
            if j < n and text[j] in "}]":
                chars[i] = " "
        i += 1
    return "".join(chars)


def unique_object(pairs: list[tuple[str, object]]) -> dict:
    result: dict = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"重复的键 {key!r}")
        result[key] = value
    return result


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def load_json(path: Path):
    """解析 JSON；失败时登记错误并返回 None。"""
    if not path.exists():
        fail(path, "文件不存在")
        return None
    try:
        return json.loads(
            blank_trailing_commas(blank_comments(read_text(path))),
            object_pairs_hook=unique_object,
        )
    except ValueError as exc:
        line = getattr(exc, "lineno", None)
        fail(path, f"JSON 解析失败：{exc}", line)
        return None


def check_json_files() -> dict[Path, object]:
    print("-- JSON 文件可解析性 --")
    parsed: dict[Path, object] = {}
    for path in JSON_FILES:
        data = load_json(path)
        if data is not None:
            parsed[path] = data
    if len(parsed) == len(JSON_FILES):
        ok(f"{len(JSON_FILES)} 个 JSON 文件解析通过，且无重复键")
    return parsed


# --------------------------------------------------------------------------- #
# i18n
# --------------------------------------------------------------------------- #

def check_i18n(parsed: dict[Path, object]) -> None:
    print("-- i18n 双文件一致性（键集合 / 键顺序 / 占位符） --")
    default, zh = parsed.get(I18N_DEFAULT), parsed.get(I18N_ZH)
    if not isinstance(default, dict) or not isinstance(zh, dict):
        skip("default.json 或 zh.json 无法解析，跳过")
        return

    default_keys, zh_keys = list(default), list(zh)
    same_set = set(default_keys) == set(zh_keys)
    if not same_set:
        for key in sorted(set(default_keys) - set(zh_keys)):
            fail(I18N_ZH, f"缺少键 {key!r}（default.json 中存在）")
        for key in sorted(set(zh_keys) - set(default_keys)):
            fail(I18N_DEFAULT, f"缺少键 {key!r}（zh.json 中存在）")
    elif default_keys != zh_keys:
        zh_text = read_text(I18N_ZH)
        for index, (expected, actual) in enumerate(zip(default_keys, zh_keys)):
            if expected != actual:
                fail(
                    I18N_ZH,
                    f"键顺序与 default.json 不一致：第 {index + 1} 个键应为 {expected!r}，实际为 {actual!r}",
                    line_of_match(zh_text, re.compile(r'^\s*"' + re.escape(actual) + r'"\s*:', re.MULTILINE)),
                )
                break

    zh_text = read_text(I18N_ZH)
    mismatch = 0
    keys = default_keys if default_keys == zh_keys else sorted(set(default_keys) & set(zh_keys))
    for key in keys:
        default_tokens = set(PLACEHOLDER_RE.findall(str(default[key])))
        zh_tokens = set(PLACEHOLDER_RE.findall(str(zh[key])))
        if default_tokens != zh_tokens:
            mismatch += 1
            fail(
                I18N_ZH,
                f"{key} 的占位符与 default.json 不一致："
                f"default.json {sorted(default_tokens)} / zh.json {sorted(zh_tokens)}",
                line_of_match(zh_text, re.compile(r'^\s*"' + re.escape(key) + r'"\s*:', re.MULTILINE)),
            )

    if same_set and default_keys == zh_keys and mismatch == 0:
        ok(f"两份文件键集合与顺序一致，共 {len(default_keys)} 个键，占位符全部匹配")


def check_i18n_keys(default: object) -> None:
    print("-- I18nKeys.cs 与 default.json 双向对应 --")
    if not isinstance(default, dict):
        skip("default.json 无法解析，跳过")
        return
    if not I18N_KEYS_CS.exists():
        fail(I18N_KEYS_CS, "文件不存在")
        return

    text = read_text(I18N_KEYS_CS)
    constants = CONST_RE.findall(text)
    if not constants:
        fail(I18N_KEYS_CS, "未解析到任何 public const string 常量")
        return

    names = [name for name, _ in constants]
    for name in sorted({n for n in names if names.count(n) > 1}):
        fail(I18N_KEYS_CS, f"常量 {name} 重复定义")

    constant_values = {value for _, value in constants}
    file_keys = set(default)
    for value in sorted(constant_values - file_keys):
        fail(I18N_KEYS_CS, f"常量值 {value!r} 在 default.json 中不存在")
    for key in sorted(file_keys - constant_values):
        fail(I18N_DEFAULT, f"i18n 键 {key!r} 在 I18nKeys.cs 中没有对应常量")

    if constant_values == file_keys:
        ok(f"{len(constants)} 个常量与 {len(file_keys)} 个 i18n 键双向对应")


# --------------------------------------------------------------------------- #
# manifest.json / csproj
# --------------------------------------------------------------------------- #

def check_manifest(parsed: dict[Path, object]) -> None:
    print("-- manifest.json --")
    manifest = parsed.get(MOD_MANIFEST)
    if not isinstance(manifest, dict):
        skip("manifest.json 无法解析，跳过")
        return
    before = len(failures)

    for field in ("Name", "Author", "Version", "Description", "UniqueID", "EntryDll", "MinimumApiVersion"):
        if not str(manifest.get(field, "")).strip():
            fail(MOD_MANIFEST, f"字段 {field} 缺失或为空")

    version = str(manifest.get("Version", ""))
    if not re.fullmatch(r"\d+\.\d+\.\d+(?:[.\-][0-9A-Za-z.\-]+)?", version):
        fail(MOD_MANIFEST, f"版本号 {version!r} 不符合 x.y.z / x.y.z-beta / x.y.z-beta.Build.N 格式")
    else:
        build = re.search(r"\.Build\.(.+)$", version)
        if build and not build.group(1).isdigit():
            fail(MOD_MANIFEST, f"版本号 {version!r} 的 Build 段必须是整数")

    if not re.fullmatch(r"[A-Za-z0-9_.]+", str(manifest.get("UniqueID", ""))):
        fail(MOD_MANIFEST, f"UniqueID {manifest.get('UniqueID')!r} 含非法字符")

    if not str(manifest.get("MinimumApiVersion", "")).startswith("4."):
        fail(MOD_MANIFEST, f"MinimumApiVersion 应为 SMAPI 4.x，实际 {manifest.get('MinimumApiVersion')!r}")

    expected_dll = f"{CSPROJ.stem}.dll"
    if manifest.get("EntryDll") != expected_dll:
        fail(MOD_MANIFEST, f"EntryDll 应为 {expected_dll!r}（csproj 项目名），实际 {manifest.get('EntryDll')!r}")

    dependencies = manifest.get("Dependencies")
    if not isinstance(dependencies, list) or not dependencies:
        fail(MOD_MANIFEST, "Dependencies 缺失或为空")
        return

    cp_manifest = parsed.get(CP_MANIFEST)
    cp_id = cp_manifest.get("UniqueID") if isinstance(cp_manifest, dict) else "Lilaoliu.RedPandaBazaar"
    required_ids = {
        dep.get("UniqueID")
        for dep in dependencies
        if isinstance(dep, dict) and dep.get("IsRequired") is True
    }
    if cp_id not in required_ids:
        fail(MOD_MANIFEST, f"必须以 IsRequired: true 声明对 CP 包 {cp_id!r} 的依赖（缺失会导致游戏启动硬报错）")
    if len(failures) == before:
        ok(f"清单字段、版本号 {version} 与对 CP 包 {cp_id!r} 的必需依赖均正常")


def check_csproj() -> None:
    print("-- csproj --")
    if not CSPROJ.exists():
        fail(CSPROJ, "文件不存在")
        return
    text = read_text(CSPROJ)
    match = re.search(r"<TargetFramework>\s*([^<\s]+)\s*</TargetFramework>", text)
    if match is None:
        fail(CSPROJ, "未找到 TargetFramework")
    elif match.group(1) != "net6.0":
        fail(CSPROJ, f"TargetFramework 必须为 net6.0（SMAPI 4.x），实际 {match.group(1)!r}", line_at(text, match.start()))
    else:
        ok("TargetFramework = net6.0")


# --------------------------------------------------------------------------- #
# CP 内容包只读
# --------------------------------------------------------------------------- #

def check_cp_readonly() -> None:
    print("-- [CP]Red Panda Bazaar/ 只读约束 --")
    if os.environ.get("ALLOW_CP_CHANGE") == "1":
        skip("已通过 ALLOW_CP_CHANGE=1 豁免（CP 上游同步）")
        return
    base = os.environ.get("BASE_SHA", "").strip()
    if not base:
        skip("未提供 BASE_SHA（本地运行或非 PR 事件）")
        return

    proc = subprocess.run(
        ["git", "-C", str(REPO_ROOT), "diff", "--name-only", base, "HEAD", "--", CP_READONLY_PATHSPEC],
        capture_output=True,
        text=True,
    )
    if proc.returncode != 0:
        fail(None, f"无法与基线 {base[:7]} 比对（需完整 git 历史）：{proc.stderr.strip()}")
        return

    changed = [name for name in proc.stdout.splitlines() if name.strip()]
    if changed:
        preview = "、".join(changed[:5])
        suffix = f" 等 {len(changed)} 个文件" if len(changed) > 5 else ""
        fail(
            None,
            f"[CP]Red Panda Bazaar/ 为只读内容包，本次改动了 {preview}{suffix}；"
            "若是同步上游请给 PR 打上 cp-change-approved 标签",
        )
    else:
        ok("[CP]Red Panda Bazaar/ 未被改动")


# --------------------------------------------------------------------------- #
# 汇总
# --------------------------------------------------------------------------- #

def write_summary() -> None:
    path = os.environ.get("GITHUB_STEP_SUMMARY")
    if not path:
        return
    lines = ["## Red Panda Bazaar 一致性校验", ""]
    if failures:
        lines += [f"**{len(failures)} 项未通过**", "", "| 位置 | 问题 |", "|---|---|"]
        for location, line, message in failures:
            where = location or "-"
            if line is not None:
                where += f":{line}"
            lines.append(f"| `{where}` | {message.replace('|', chr(92) + '|')} |")
    else:
        lines.append("全部检查通过。")
    with open(path, "a", encoding="utf-8") as handle:
        handle.write("\n".join(lines) + "\n")


def main() -> int:
    print(f"== Red Panda Bazaar 一致性校验（{'GitHub Actions' if CI else '本地'}）==")
    parsed = check_json_files()
    check_i18n(parsed)
    check_i18n_keys(parsed.get(I18N_DEFAULT))
    check_manifest(parsed)
    check_csproj()
    check_cp_readonly()

    print()
    if failures:
        print(f"校验失败：{len(failures)} 项未通过。")
        write_summary()
        return 1
    print("校验通过：全部检查项均满足。")
    write_summary()
    return 0


if __name__ == "__main__":
    sys.exit(main())
