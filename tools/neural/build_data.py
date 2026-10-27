"""Builds the training data for the neural fallback (tools/neural/README.md).

Positives: Uzbek word forms from src/YangiAlifbo.Core/Dictionaries/uzbek_words.txt, in the
old (1995) spelling people type. Negatives: English/Spanish/French words
expanded from LibreOffice's Hunspell dictionaries, english_reject.txt, the
English thesaurus, and identifiers found in source code on this machine.

Only words the model can be asked about are kept: they contain sh/ch/o'/g'
and pass the same hard rules as the C# engine (NeuralFallback.IsEligible).
Words found on both sides are dropped. Splits are by the first four letters,
so validation and test words come from stems the model never saw.

    python -I build_data.py <out_dir> [<source_root> ...]
"""
import hashlib
import os
import re
import sys
import unicodedata

HERE = os.path.dirname(os.path.abspath(__file__))
DICT = os.path.join(HERE, "..", "..", "src", "YangiAlifbo.Core", "Dictionaries")
LO = r"C:\Program Files\LibreOffice\share\extensions"

APOS = "'\u2018\u2019\u02bb\u02bc`\u00b4\u201b\u2032"
NEW_TO_OLD = {"ş": "sh", "ç": "ch", "ö": "o'", "ğ": "g'"}
ELIGIBLE = re.compile(r"^[a-z']+(-[a-z']+)?$")


def norm(w):
    w = w.strip().lower()
    for a in APOS:
        w = w.replace(a, "'")
    return w


def to_old(w):
    return "".join(NEW_TO_OLD.get(c, c) for c in w)


def has_pattern(w):
    w = w.replace("s'h", "").replace("c'h", "")
    return "sh" in w or "ch" in w or "o'" in w or "g'" in w


def eligible(w):
    """Mirror of NeuralFallback.IsEligible in C#."""
    if not (4 <= len(w) <= 32) or not ELIGIBLE.match(w):
        return False
    if "w" in w or re.search(r"c(?!h)", w):
        return False   # not Uzbek Latin letters
    if w.startswith("'") or w.endswith("-") or "''" in w:
        return False
    return has_pattern(w)


# ── Hunspell expansion (one suffix + one prefix level, enough for word lists) ──

def parse_flags(s, mode):
    if mode == "long":
        return [s[i:i + 2] for i in range(0, len(s) - 1, 2)]
    if mode == "num":
        return s.split(",")
    return list(s)


def load_aff(path):
    mode, rules = "char", {}
    with open(path, encoding="utf-8", errors="ignore") as f:
        lines = f.read().splitlines()
    for ln in lines:
        p = ln.split()
        if len(p) >= 2 and p[0] == "FLAG":
            mode = {"long": "long", "num": "num"}.get(p[1], "char")
    for ln in lines:
        p = ln.split()
        if len(p) >= 4 and p[0] in ("PFX", "SFX") and not p[3].isdigit():
            kind, flag, strip, add = p[0], p[1], p[2], p[3].split("/")[0]
            cond = p[4] if len(p) > 4 else "."
            strip = "" if strip == "0" else strip
            add = "" if add == "0" else add
            try:
                rx = re.compile(("^" + cond) if kind == "PFX" else (cond + "$"))
            except re.error:
                continue
            rules.setdefault(flag, []).append((kind, strip, add, rx))
    return mode, rules


def expand_dic(dic, aff):
    mode, rules = load_aff(aff)
    out = set()
    with open(dic, encoding="utf-8", errors="ignore") as f:
        next(f, None)
        for ln in f:
            ln = ln.strip()
            if not ln or ln.startswith("#"):
                continue
            ln = ln.split("\t")[0].split(" ")[0]
            word, _, flags = ln.partition("/")
            out.add(word)
            sfx = []
            for fl in parse_flags(flags, mode):
                for kind, strip, add, rx in rules.get(fl, ()):
                    if kind == "SFX" and not word.endswith(strip) or kind == "PFX" and not word.startswith(strip):
                        continue
                    if not rx.search(word):
                        continue
                    if kind == "SFX":
                        form = word[: len(word) - len(strip)] + add
                        out.add(form)
                        sfx.append(form)
                    else:
                        out.add(add + word[len(strip):])
            for fl in parse_flags(flags, mode):   # cross product prefix+suffix
                for kind, strip, add, rx in rules.get(fl, ()):
                    if kind == "PFX":
                        for form in sfx:
                            if form.startswith(strip):
                                out.add(add + form[len(strip):])
    return out


def strip_accents(w):
    return "".join(c for c in unicodedata.normalize("NFD", w) if unicodedata.category(c) != "Mn")


IDENT = re.compile(r"[A-Za-z][A-Za-z0-9_]{2,}")
CAMEL = re.compile(r"[A-Z]+(?=[A-Z][a-z])|[A-Z]?[a-z]+|[A-Z]+")
CODE_EXT = (".cs", ".py", ".js", ".ts", ".tsx", ".java", ".cpp", ".c", ".h", ".go", ".rs", ".kt", ".swift", ".rb", ".php")


def code_words(roots, skip):
    out = set()
    for root in roots:
        for dp, dn, fn in os.walk(root):
            dn[:] = [d for d in dn if not any(s in os.path.join(dp, d).lower() for s in skip)]
            for name in fn:
                if not name.endswith(CODE_EXT):
                    continue
                try:
                    with open(os.path.join(dp, name), encoding="utf-8", errors="ignore") as f:
                        text = f.read(2_000_000)
                except OSError:
                    continue
                for tok in IDENT.findall(text):
                    out.add(tok.lower())
                    for part in tok.split("_"):
                        out.update(m.lower() for m in CAMEL.findall(part))
    return out


def bucket(w):
    key = w.replace("'", "")[:4]
    return int(hashlib.md5(key.encode()).hexdigest(), 16) % 10   # 0 test, 1 val, rest train


def main():
    out_dir = sys.argv[1]
    roots = sys.argv[2:]
    os.makedirs(out_dir, exist_ok=True)

    pos = set()
    with open(os.path.join(DICT, "uzbek_words.txt"), encoding="utf-8") as f:
        for ln in f:
            w = to_old(norm(ln))
            if eligible(w):
                pos.add(w)

    neg_raw = set()
    with open(os.path.join(DICT, "english_reject.txt"), encoding="utf-8") as f:
        neg_raw.update(norm(ln) for ln in f)
    for name in ("dict-en/en_US", "dict-en/en_GB", "dict-es/es_ES", "dict-fr/fr"):
        base = os.path.join(LO, name)
        words = expand_dic(base + ".dic", base + ".aff")
        neg_raw.update(norm(w) for w in words)
        neg_raw.update(norm(strip_accents(w)) for w in words)
        print(f"{name}: {len(words)} forms", flush=True)
    with open(os.path.join(LO, "dict-en", "th_en_US_v2.dat"), encoding="utf-8", errors="ignore") as f:
        for ln in f:
            for w in re.split(r"[\s|()]+", ln):
                neg_raw.add(norm(w))
    code = code_words(roots, skip=("alphabet", "node_modules", "\\.git", "\\bin", "\\obj"))
    print(f"code identifiers: {len(code)}", flush=True)
    neg_raw.update(code)

    neg = {w for w in neg_raw if eligible(w)}
    both = pos & neg
    pos -= both
    neg -= both
    print(f"positives {len(pos)}, negatives {len(neg)}, dropped (both sides) {len(both)}")

    splits = {0: "test", 1: "val"}
    files = {s: open(os.path.join(out_dir, s + ".tsv"), "w", encoding="utf-8") for s in ("train", "val", "test")}
    counts = {}
    for label, words in ((1, pos), (0, neg)):
        for w in sorted(words):
            s = splits.get(bucket(w), "train")
            files[s].write(f"{w}\t{label}\n")
            counts[(s, label)] = counts.get((s, label), 0) + 1
    for f in files.values():
        f.close()
    with open(os.path.join(out_dir, "dropped_both.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(sorted(both)))
    for s in ("train", "val", "test"):
        print(f"{s}: {counts.get((s, 1), 0)} uzbek, {counts.get((s, 0), 0)} foreign")


if __name__ == "__main__":
    main()
