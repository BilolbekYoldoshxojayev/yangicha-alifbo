"""Turns uzbek_neural.bin (v3, half precision) into v4: int8 weights with one
float32 scale per output row, and resets the threshold for the quantized
weights so that no foreign word in validation or test is accepted.

    python quantize.py <uzbek_neural.bin v3> <data_dir> <out_dir>

Writes <out_dir>/uzbek_neural.bin (v4), vectors.tsv (C# parity) and report.txt.
Pure numpy: the forward pass here is the one NeuralFallback.cs runs.
"""
import os
import struct
import sys

import numpy as np


def read_v3(path):
    d = open(path, "rb").read()
    p = 0

    def take(fmt):
        nonlocal p
        v = struct.unpack_from(fmt, d, p)
        p += struct.calcsize(fmt)
        return v

    assert d[:4] == b"YANN"; p = 4
    ver, vocab, maxlen, emb, ch, nk, hid, nm = take("<8i")
    assert ver == 3
    kernels = take(f"<{nk}i")
    (thr,) = take("<d")
    e = d.index(b"\0", p); alphabet = d[p:e].decode(); p = e + 1
    (n,) = take("<i"); tri = d[p:p + n]; p += n

    def arr(*shape):
        nonlocal p
        cnt = int(np.prod(shape))
        a = np.frombuffer(d, dtype="<f2", count=cnt, offset=p).astype(np.float32).reshape(shape)
        p += 2 * cnt
        return a

    models = []
    for _ in range(nm):
        m = {"emb": arr(vocab, emb), "conv": []}
        for k in kernels:
            m["conv"].append((arr(ch, emb, k), arr(ch)))
        m["fc1w"], m["fc1b"], m["fc2w"], m["fc2b"] = arr(hid, ch * nk), arr(hid), arr(1, hid), arr(1)
        models.append(m)
    assert p == len(d)
    return dict(vocab=vocab, maxlen=maxlen, emb=emb, ch=ch, hid=hid, kernels=kernels, thr=thr,
                alphabet=alphabet, tri=tri, models=models)


def quant(w):
    """int8 per output row (first axis); returns (q, scale)."""
    flat = w.reshape(w.shape[0], -1)
    scale = np.abs(flat).max(axis=1) / 127.0
    scale[scale == 0] = 1.0
    q = np.clip(np.round(flat / scale[:, None]), -127, 127).astype(np.int8)
    return q.reshape(w.shape), scale.astype(np.float32)


def dequant(q, s):
    return (q.reshape(q.shape[0], -1).astype(np.float32) * s[:, None]).reshape(q.shape)


def encode(words, alphabet, maxlen):
    cid = {c: i + 3 for i, c in enumerate(alphabet)}
    x = np.zeros((len(words), maxlen), dtype=np.int64)
    for i, w in enumerate(words):
        ids = [1] + [cid[c] for c in w[:maxlen - 2]] + [2]
        x[i, :len(ids)] = ids
    return x


def forward(m, x, kernels):
    e = m["emb"][x]                                    # N, L, E
    pooled = []
    for (w, b), k in zip(m["conv"], kernels):
        L = e.shape[1] - k + 1
        win = np.stack([e[:, t:t + k, :] for t in range(L)], axis=1)   # N, L, k, E
        out = np.einsum("nlke,cek->nlc", win, w) + b
        pooled.append(np.maximum(out.max(axis=1), 0))
    h = np.concatenate(pooled, axis=1)
    h = np.maximum(h @ m["fc1w"].T + m["fc1b"], 0)
    z = (h @ m["fc2w"].T + m["fc2b"])[:, 0]
    return 1 / (1 + np.exp(-z.astype(np.float64)))


def main():
    src, data, out = sys.argv[1:4]
    os.makedirs(out, exist_ok=True)
    M = read_v3(src)
    tri = set(M["tri"].decode().split("\n"))
    qmodels = []
    for m in M["models"]:
        q = {"emb": quant(m["emb"]), "conv": [(quant(w), b) for w, b in m["conv"]],
             "fc1w": quant(m["fc1w"]), "fc1b": m["fc1b"], "fc2w": m["fc2w"], "fc2b": m["fc2b"]}
        qmodels.append(q)
    deq = [{"emb": dequant(*q["emb"]), "conv": [(dequant(*w), b) for w, b in q["conv"]],
            "fc1w": dequant(*q["fc1w"]), "fc1b": q["fc1b"], "fc2w": q["fc2w"], "fc2b": q["fc2b"]} for q in qmodels]

    def score(models, words):
        ok = np.array([all(("^" + w + "$")[i:i + 3] in tri for i in range(len(w))) for w in words])
        x = encode(words, M["alphabet"], M["maxlen"])
        s = np.ones(len(words))
        for m in models:
            for i in range(0, len(words), 4096):
                s[i:i + 4096] = np.minimum(s[i:i + 4096], forward(m, x[i:i + 4096], M["kernels"]))
        return np.where(ok, s, 0.0)

    def load(name):
        ws, ys = [], []
        for ln in open(os.path.join(data, name), encoding="utf-8"):
            w, y = ln.rstrip("\n").split("\t"); ws.append(w); ys.append(int(y))
        return ws, np.array(ys)

    lines = []
    for label, models in (("fp16", M["models"]), ("int8", deq)):
        res = {}
        for split in ("val", "test"):
            ws, ys = load(split + ".tsv")
            res[split] = (ws, ys, score(models, ws))
        neg = np.concatenate([res[s][2][res[s][1] == 0] for s in res])
        pos = np.concatenate([res[s][2][res[s][1] == 1] for s in res])
        thr = float(neg.max())
        lines.append(f"{label}: threshold {thr:.9f} (max foreign score of {len(neg)}), Uzbek recall {(pos > thr).mean():.2%} of {len(pos)}; "
                     f"at shipped threshold {M['thr']:.6f}: foreign above {(neg > M['thr']).sum()}, recall {(pos > M['thr']).mean():.2%}")
        if label == "int8":
            int8_thr, int8_res = thr, res
    print("\n".join(lines))
    open(os.path.join(out, "report.txt"), "w").write("\n".join(lines) + "\n")

    with open(os.path.join(out, "uzbek_neural.bin"), "wb") as f:
        f.write(b"YANN")
        f.write(struct.pack("<8i", 4, M["vocab"], M["maxlen"], M["emb"], M["ch"], len(M["kernels"]), M["hid"], len(qmodels)))
        f.write(struct.pack(f"<{len(M['kernels'])}i", *M["kernels"]))
        f.write(struct.pack("<d", int8_thr))
        f.write(M["alphabet"].encode() + b"\0")
        f.write(struct.pack("<i", len(M["tri"]))); f.write(M["tri"])

        def put(q):   # scales first, then int8 rows
            f.write(q[1].astype("<f4").tobytes()); f.write(q[0].astype(np.int8).tobytes())

        for q in qmodels:
            put(q["emb"])
            for w, b in q["conv"]:
                put(w); f.write(b.astype("<f4").tobytes())
            put(q["fc1w"]); f.write(q["fc1b"].astype("<f4").tobytes())
            f.write(q["fc2w"].astype("<f4").tobytes()); f.write(q["fc2b"].astype("<f4").tobytes())

    ws, _, s = int8_res["test"]
    rng = np.random.default_rng(0)
    with open(os.path.join(out, "vectors.tsv"), "w", encoding="utf-8") as f:
        for i in rng.choice(len(ws), 2000, replace=False):
            f.write(f"{ws[i]}\t{s[i]:.9f}\n")


if __name__ == "__main__":
    main()
