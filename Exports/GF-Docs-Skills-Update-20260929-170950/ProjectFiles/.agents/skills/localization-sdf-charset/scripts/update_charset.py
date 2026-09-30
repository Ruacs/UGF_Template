#!/usr/bin/env python3
"""Report or append missing characters for Unity SDF charset files."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

DEFAULT_CHARSET_DIR = "Assets/AAA_DevAssets/Fonts/Charset"
LEGACY_CHARSET_DIRS = ("Assets/AAA_Test/Fonts/Charset", "Assets/AAA_Test")

CHARSET_FILES = {
    "default": "unity_sdf_charset.txt",
    "CNS": "unity_sdf_charset_CNS.txt",
    "CNT": "unity_sdf_charset_CNT.txt",
    "JP": "unity_sdf_charset_JP.txt",
    "KR": "unity_sdf_charset_KR.txt",
    "Hindi": "unity_sdf_charset_Hindi.txt",
    "Arabic": "unity_sdf_charset_Arabic.txt",
    "Thai": "unity_sdf_charset_Thai.txt",
}


def unique_chars(strings: list[str]) -> list[str]:
    seen: dict[str, None] = {}
    for text in strings:
        for char in text:
            if not char.isspace():
                seen.setdefault(char, None)
    return list(seen.keys())


def resolve_dir(project: Path, raw_dir: str) -> Path:
    path = Path(raw_dir)
    if not path.is_absolute():
        path = project / path
    return path


def resolve_charset(project: Path, charset_dir: Path, key: str) -> Path:
    filename = CHARSET_FILES.get(key)
    if filename is None:
        path = Path(key)
        if not path.is_absolute():
            path = project / path
        return path

    preferred = charset_dir / filename
    if preferred.exists():
        return preferred

    for legacy_dir in LEGACY_CHARSET_DIRS:
        legacy = resolve_dir(project, legacy_dir) / filename
        if legacy.exists():
            return legacy

    return preferred


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def append_text(path: Path, text: str) -> None:
    with path.open("a", encoding="utf-8", newline="") as handle:
        handle.write(text)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", default=".", help="Unity project root. Defaults to current directory.")
    parser.add_argument("--phrases", required=True, help="JSON file mapping charset keys/paths to string arrays.")
    parser.add_argument("--charset-dir", default=DEFAULT_CHARSET_DIR, help="Auxiliary directory that contains unity_sdf_charset*.txt files.")
    parser.add_argument("--apply", action="store_true", help="Append missing characters. Omit for dry-run stats.")
    args = parser.parse_args()

    project = Path(args.project).resolve()
    charset_dir = resolve_dir(project, args.charset_dir)
    phrases_path = Path(args.phrases).resolve()
    data = json.loads(phrases_path.read_text(encoding="utf-8"))

    rows = []
    had_remaining = False
    for key, strings in data.items():
        if not isinstance(strings, list) or not all(isinstance(item, str) for item in strings):
            raise SystemExit(f"{key}: expected an array of strings")

        charset_path = resolve_charset(project, charset_dir, key)
        before = read_text(charset_path)
        chars = unique_chars(strings)
        missing = [char for char in chars if char not in before]

        appended = ""
        if args.apply and missing:
            appended = "".join(missing)
            append_text(charset_path, appended)

        after = read_text(charset_path)
        remaining = [char for char in chars if char not in after]
        had_remaining = had_remaining or bool(remaining)
        rows.append(
            {
                "key": key,
                "file": str(charset_path),
                "candidate_unique_count": len(chars),
                "missing_count": len(missing),
                "missing_chars": "".join(missing),
                "appended_count": len(appended),
                "appended_chars": appended,
                "remaining_missing_count": len(remaining),
                "remaining_missing_chars": "".join(remaining),
            }
        )

    print(json.dumps(rows, ensure_ascii=False, indent=2))
    return 1 if had_remaining else 0


if __name__ == "__main__":
    raise SystemExit(main())




