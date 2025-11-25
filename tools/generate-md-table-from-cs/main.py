#!/usr/bin/env python3
import sys
import os

if len(sys.argv) < 2:
    print("Usage: python generate_docs.py <input_dir> <output_file>")
    sys.exit(1)

input_dir = sys.argv[1]
output_file = sys.argv[2]

if not os.path.isdir(input_dir):
    print(f"Input file not found: {input_dir}")
    sys.exit(1)

if os.path.isdir(output_file):
    print(f"Output file is a directory: {output_file}")
    sys.exit(1)

base_name = os.path.splitext(os.path.basename(input_dir))[0]

def parse_file(path, lines):
    classes = []
    current_class = None
    class_summary = []
    current_properties = []
    prop_summary = []
    prop_value = []
    recording_summary = False
    recording_value = False
    attr_generate = False

    print(f"Parse file {path}")

    def flush_class():
        nonlocal current_class, class_summary, current_properties
        if current_class:
            classes.append({
                "name": current_class,
                "desc": " ".join(class_summary).strip(),
                "props": current_properties.copy()
            })
            print(f"Added class {current_class}")
            current_class = None
            class_summary = []
            current_properties = []

    for raw in lines:
        line = raw.strip()

        if line.startswith("[GenerateMarkdownTable]"):
            attr_generate = True
            continue

        if line.startswith("/// <summary>"):
            recording_summary = True
            prop_summary = []
            continue
        elif line.startswith("/// </summary>"):
            recording_summary = False
            continue
        elif recording_summary and line.startswith("///"):
            prop_summary.append(line[3:].strip())
            continue

        if line.startswith("/// <default>") or line.startswith("/// <value>"):
            recording_value = True
            prop_value = []
            continue
        elif line.startswith("/// </default>") or line.startswith("/// </value>"):
            recording_value = False
            continue
        elif recording_value and line.startswith("///"):
            prop_value.append(line[3:].strip())
            continue

        if attr_generate and line.startswith("public class "):
            parts = line.split()
            if len(parts) >= 3:
                current_class = parts[2].strip("{")
                class_summary = prop_summary.copy()
                current_properties = []
                attr_generate = False
                prop_summary = []
                print(f"  Class {current_class} --- {class_summary}")
            continue

        if current_class and line.startswith("public ") and " get; " in line:
            tokens = line.split()
            if len(tokens) >= 3:
                # handle optional "required"
                if tokens[1] == "required":
                    type_ = tokens[2]
                    name = tokens[3].split("{")[0].split("=")[0].strip()
                else:
                    type_ = tokens[1]
                    name = tokens[2].split("{")[0].split("=")[0].strip()

                # default from assignment
                default = ""
                if "=" in line:
                    default = line.split("=")[1].split(";")[0].strip()

                # extract <type> and <default> tags from summary
                type_override = ""
                default_override = None
                desc_lines = []
                for l in prop_summary:
                    if "<type>" in l and "</type>" in l:
                        type_override = l.split("<type>")[1].split("</type>")[0].strip()
                    elif "<default>" in l and "</default>" in l:
                        default_override = l.split("<default>")[1].split("</default>")[0].strip()
                    else:
                        desc_lines.append(l)

                desc = "\n".join(desc_lines).strip()
                final_type = type_override or type_
                final_default = default_override if default_override is not None else default

                print(f"    {name} --- default_override={default_override} final_default={default}")

                current_properties.append((name, final_type, final_default, desc))
                prop_summary = []
                prop_value = []
            continue

        if line == "}":
            flush_class()

    return classes

def generate_markdown(classes):
    lines = []
    for cls in classes:
        lines.append(f"### {cls['name']}")
        lines.append("")
        lines.append(cls["desc"])
        lines.append("")
        lines.append("| Property | Type | Default | Description |")
        lines.append("|-----------|------|----------|--------------|")
        for name, type_, default, desc in cls["props"]:
            default_str = f"`{default}`" if default else ""
            safe_type = type_.replace("|", "\\|")
            safe_desc = desc.replace("\n", "<br>").replace("\r", "").replace("", "")
            camel_name = name[0].lower() + name[1:] if name else name
            lines.append(f"| `{camel_name}` | `{safe_type}` | {default_str} | {safe_desc} |")
        lines.append("")
    return "\n".join(lines)

def collect_classes(input_dir):
    all_classes = []

    for root, _, files in os.walk(input_dir):
        for name in files:
            if not name.lower().endswith(".cs"):
                continue

            path = os.path.join(root, name)
            with open(path, encoding="utf-8") as f:
                lines = f.readlines()

            classes = parse_file(path, lines)
            if classes:
                all_classes.extend(classes)

    return all_classes

classes = collect_classes(input_dir)

if not classes:
    print("No classes found with [GenerateMarkdownTable].")
else:
    markdown = generate_markdown(classes)
    with open(output_file, "w", encoding="utf-8") as f:
        f.write(markdown)
    print(f"Documentation written to {output_file}")
