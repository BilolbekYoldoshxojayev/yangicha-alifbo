"""libyangialifbo.so must convert exactly like the Windows app.

    python3 parity.py <libyangialifbo.so> <words.txt> <out.tsv> <expected.sha256>

words.txt comes from tools/parity (same list on every OS); out.tsv uses its format.
"""
import hashlib
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "engine"))
from yangialifbo_core import AGGRESSIVE, STRICT, Core  # noqa: E402

lib, words, out, expected = sys.argv[1:5]
core = Core(lib)
rows = []
for w in open(words, encoding="utf-8").read().split("\n"):
    if not w:
        continue
    r = [core.convert(w, STRICT, True), core.convert(w, STRICT, False), core.convert(w, AGGRESSIVE, True)]
    rows.append(w + "\t" + "\t".join(x or "" for x in r) + "\n")
data = "".join(rows).encode("utf-8")
open(out, "wb").write(data)
got = hashlib.sha256(data).hexdigest()
want = open(expected).read().split()[0]
print(f"{len(rows)} words, sha256 {got} (Windows {want})")
sys.exit(0 if got == want else 1)
