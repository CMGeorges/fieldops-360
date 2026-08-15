#!/usr/bin/env python3
"""Render and enforce Cobertura line and branch coverage gates."""

from __future__ import annotations

import argparse
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def percentage(value: str | None) -> float:
    return round(float(value or "0") * 100, 2)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("report", type=Path)
    parser.add_argument("--min-line", type=float, default=90.0)
    parser.add_argument("--min-branch", type=float, default=85.0)
    args = parser.parse_args()

    if not args.report.is_file():
        print(f"Coverage report not found: {args.report}", file=sys.stderr)
        return 2

    root = ET.parse(args.report).getroot()
    line = percentage(root.get("line-rate"))
    branch = percentage(root.get("branch-rate"))
    line_pass = line >= args.min_line
    branch_pass = branch >= args.min_branch

    print("## Domain coverage")
    print()
    print("| Metric | Result | Required | Status |")
    print("|---|---:|---:|:---:|")
    print(f"| Lines | {line:.2f}% | {args.min_line:.2f}% | {'PASS' if line_pass else 'FAIL'} |")
    print(f"| Branches | {branch:.2f}% | {args.min_branch:.2f}% | {'PASS' if branch_pass else 'FAIL'} |")

    return 0 if line_pass and branch_pass else 1


if __name__ == "__main__":
    raise SystemExit(main())

