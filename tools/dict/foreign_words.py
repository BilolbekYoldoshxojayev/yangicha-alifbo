"""Writes every foreign word form we can build offline, one per line, lower case:
English (en_US, en_GB), Spanish and French Hunspell dictionaries with their
affixes expanded, the English thesaurus, and identifiers from Python sources.

tools/compact then keeps the words the engine could act on (sh/ch/o'/g', or a
chat-slang rewrite) and that are not Uzbek, and stores them as foreign_words.br.

    python -I foreign_words.py <out.txt> [<source_root> ...]
"""
import os
import re
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "neural"))
from build_data import LO, code_words, expand_dic, norm, strip_accents  # noqa: E402

WORD = re.compile(r"^[a-z][a-z'-]*[a-z]$")


def main():
    out, roots = sys.argv[1], sys.argv[2:]
    words = set()
    for name in ("dict-en/en_US", "dict-en/en_GB", "dict-es/es_ES", "dict-fr/fr"):
        base = os.path.join(LO, name)
        for w in expand_dic(base + ".dic", base + ".aff"):
            words.add(norm(w))
            words.add(norm(strip_accents(w)))
    with open(os.path.join(LO, "dict-en", "th_en_US_v2.dat"), encoding="utf-8", errors="ignore") as f:
        for ln in f:
            words.update(norm(w) for w in re.split(r"[\s|()]+", ln))
    words.update(code_words(roots, skip=("alphabet", "node_modules", "\\.git", "\\bin", "\\obj")))
    words = sorted(w for w in words if WORD.match(w))
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(words) + "\n")
    print(f"{len(words)} foreign word forms")


if __name__ == "__main__":
    main()
