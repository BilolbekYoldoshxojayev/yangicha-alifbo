#!/usr/bin/env python3
"""
YangiAlifbo Evaluation Harness (tools/eval/evaluate.py)
Evaluates any YangiAlifbo engine against real-world Uzbek text and English/code text.
Enforces Hard Rule 4: Converting an English word is worse than missing an Uzbek word.
Never accept a change that raises English false positives on the eval set.
"""

import argparse
import json
import os
import subprocess
import sys

REFORMED_CHARS = set("şçöğŞÇÖĞ")

def find_repo_root():
    curr = os.path.abspath(os.path.dirname(__file__))
    while curr:
        if os.path.exists(os.path.join(curr, "YangiAlifbo.sln")) or os.path.exists(os.path.join(curr, ".agent")):
            return curr
        parent = os.path.dirname(curr)
        if parent == curr:
            break
        curr = parent
    return os.getcwd()

def extract_reformed_tokens(text):
    tokens = []
    for word in text.split():
        clean = "".join(c for c in word if c.isalnum() or c in "'-_şçöğŞÇÖĞ")
        if any(c in REFORMED_CHARS for c in clean):
            tokens.append(clean)
    return tokens

def run_engine(cmd, text):
    proc = subprocess.run(
        cmd,
        input=text.encode("utf-8"),
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        shell=isinstance(cmd, str)
    )
    if proc.returncode != 0:
        raise RuntimeError(f"Engine command failed with code {proc.returncode}: {proc.stderr.decode('utf-8', errors='replace')}")
    return proc.stdout.decode("utf-8")

def main():
    parser = argparse.ArgumentParser(description="Evaluate YangiAlifbo engine against eval dataset.")
    parser.add_argument("--dataset", default=None, help="Path to eval dataset JSON file.")
    parser.add_argument("--cmd", default=None, help="Command to run the engine (accepts stdin, writes stdout).")
    args = parser.parse_args()

    repo_root = find_repo_root()
    dataset_path = args.dataset or os.path.join(repo_root, "tools", "eval", "eval_dataset.json")

    if not os.path.exists(dataset_path):
        print(f"Error: Dataset file not found: {dataset_path}", file=sys.stderr)
        return 1

    with open(dataset_path, "r", encoding="utf-8") as f:
        dataset = json.load(f)

    cmd = args.cmd
    if not cmd:
        runner_exe = os.path.join(repo_root, "tools", "eval", "bin", "Release", "net10.0-windows", "EvalRunner.exe")
        if not os.path.exists(runner_exe):
            dotnet_exe = os.path.expandvars(r"%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe")
            if not os.path.exists(dotnet_exe):
                dotnet_exe = "dotnet"
            eval_proj = os.path.join(repo_root, "tools", "eval", "EvalRunner.csproj")
            print("Building EvalRunner.exe once...")
            subprocess.run([dotnet_exe, "build", eval_proj, "-c", "Release", "--nologo", "-v", "q"], check=True)
        cmd = f'"{runner_exe}"'

    print(f"=== YangiAlifbo Engine Evaluation ===")
    print(f"Dataset: {dataset_path} ({len(dataset)} cases)")
    print(f"Engine command: {cmd}\n")

    uzbek_total = 0
    uzbek_passed = 0
    uzbek_misses = []

    english_total = 0
    english_passed = 0
    english_false_positives = []

    for item in dataset:
        item_id = item["id"]
        item_type = item["type"]
        input_text = item["input"].replace("\r\n", "\n").strip()
        expected_text = item["expected"].replace("\r\n", "\n").strip()

        raw_output = run_engine(cmd, item["input"])
        actual_output = raw_output.replace("\r\n", "\n").strip()

        if item_type == "uzbek":
            uzbek_total += 1
            if actual_output == expected_text:
                uzbek_passed += 1
                print(f"  [PASS] {item_id}")
            else:
                print(f"  [FAIL] {item_id}")
                print(f"         Expected: {expected_text}")
                print(f"         Actual:   {actual_output}")
                # Analyze missing words
                exp_words = expected_text.split()
                act_words = actual_output.split()
                for e_w, a_w in zip(exp_words, act_words):
                    if any(c in REFORMED_CHARS for c in e_w) and not any(c in REFORMED_CHARS for c in a_w):
                        uzbek_misses.append((item_id, e_w, a_w))

        elif item_type == "english_code":
            english_total += 1
            reformed_in_actual = extract_reformed_tokens(actual_output)
            if actual_output == input_text and not reformed_in_actual:
                english_passed += 1
                print(f"  [PASS] {item_id} (Zero English false positives)")
            else:
                print(f"  [CRITICAL FAIL] {item_id} - ENGLISH WORD CONVERTED!")
                print(f"         Input:  {input_text}")
                print(f"         Actual: {actual_output}")
                for tok in reformed_in_actual:
                    english_false_positives.append((item_id, tok))

    print("\n" + "=" * 50)
    print("EVALUATION SUMMARY")
    print("=" * 50)
    print(f"Uzbek Sentences: {uzbek_passed}/{uzbek_total} passed ({uzbek_passed/uzbek_total*100:.1f}%)")
    if uzbek_misses:
        print(f"  Uzbek misses ({len(uzbek_misses)}):")
        for m in uzbek_misses:
            print(f"    - [{m[0]}] expected '{m[1]}', got '{m[2]}'")

    print(f"English/Code:    {english_passed}/{english_total} passed ({english_passed/english_total*100:.1f}%)")
    print(f"English False Positives: {len(english_false_positives)}")
    if english_false_positives:
        print(f"  CRITICAL VIOLATION OF HARD RULE 4:")
        for fp in english_false_positives:
            print(f"    - [{fp[0]}] falsely converted token: '{fp[1]}'")
        return 2

    if uzbek_passed == uzbek_total and english_passed == english_total:
        print("\nALL EVALUATION CRITERIA SATISFIED (100% PARITY, ZERO FALSE POSITIVES)!")
        return 0
    else:
        print(f"\nEvaluation did not achieve 100% (Uzbek: {uzbek_passed}/{uzbek_total})")
        return 1

if __name__ == "__main__":
    sys.exit(main())
