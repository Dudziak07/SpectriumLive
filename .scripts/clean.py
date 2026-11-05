#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
import shutil
import stat
from pathlib import Path

TARGET_DIRS = {"bin", "obj", "TestResults"}
IGNORE_DIRS = {".git", ".idea", ".vscode", ".venv", ".scripts"}


def _remove_readonly(func, path, _) -> None:
    try:
        os.chmod(path, stat.S_IWRITE)
        func(path)
    except Exception:
        pass


def get_dir_size(path: Path) -> int:
    total = 0
    try:
        for entry in path.rglob("*"):
            if entry.is_file():
                try:
                    total += entry.stat().st_size
                except OSError:
                    pass
    except OSError:
        pass
    return total


def format_size(size_bytes: int) -> str:
    for unit in ("B", "KB", "MB", "GB"):
        if size_bytes < 1024:
            return f"{size_bytes:.1f} {unit}"
        size_bytes /= 1024
    return f"{size_bytes:.1f} TB"


def main() -> int:
    parser = argparse.ArgumentParser(description="Clean .NET build artifacts.")
    parser.add_argument("--dry-run", action="store_true", help="List directories without deleting.")
    args = parser.parse_args()

    repo_root = Path(__file__).resolve().parent.parent
    targets: list[Path] = []

    for root, dirs, _ in os.walk(repo_root):
        dirs[:] = [d for d in dirs if d not in IGNORE_DIRS]
        for d in dirs:
            if d in TARGET_DIRS:
                targets.append(Path(root) / d)

    if not targets:
        print("No build artifacts found.")
        return 0

    total_bytes = 0
    for directory in sorted(targets):
        size = get_dir_size(directory)
        total_bytes += size
        rel_path = directory.relative_to(repo_root)

        if args.dry_run:
            print(f"[dry-run] Would remove: {rel_path} ({format_size(size)})")
        else:
            shutil.rmtree(directory, onerror=_remove_readonly)
            print(f"Removed: {rel_path} ({format_size(size)})")

    action = "Would free" if args.dry_run else "Freed"
    print(f"\n{action} {format_size(total_bytes)} across {len(targets)} directories.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
