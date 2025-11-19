#!/usr/bin/env python3
import re
import argparse
import sys
from pathlib import Path


def update_section(readme_path: Path, section: str, new_content: str):
    text = readme_path.read_text(encoding="utf-8")

    pattern = rf"(<!-- START_SECTION:{section} -->)(.*?)(<!-- END_SECTION:{section} -->)"
    match = re.search(pattern, text, flags=re.DOTALL)
    if not match:
        raise ValueError(f"Section '{section}' not found in {readme_path}")

    start_tag, _, end_tag = match.groups()
    updated_block = f"{start_tag}\n\n{new_content}\n{end_tag}"

    new_text = text[:match.start()] + updated_block + text[match.end():]
    readme_path.write_text(new_text, encoding="utf-8")


def print_usage():
    print("Usage: update_section.py --section <name> --input <file> [--file README.md]")


def main():
    parser = argparse.ArgumentParser(add_help=False)
    parser.add_argument("--file", default="README.md")
    parser.add_argument("--section")
    parser.add_argument("--input")

    if len(sys.argv) == 1:
        print_usage()
        sys.exit(0)

    args = parser.parse_args()

    if not args.section or not args.input:
        print_usage()
        sys.exit(1)

    readme = Path(args.file)
    input_path = Path(args.input)

    if not readme.exists():
        print(f"Error: File not found: {readme}")
        sys.exit(1)

    if not input_path.exists():
        print(f"Error: Input file not found: {input_path}")
        sys.exit(1)

    new_content = input_path.read_text(encoding="utf-8")

    try:
        update_section(readme, args.section, new_content)
        print(f"Updated '{args.section}' in {readme}")
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
