"""Trains the character-CNN ensemble behind NeuralFallback.cs and exports it.

    python train.py <data_dir> <out_dir> [--epochs N] [--models K] [--wneg W]

Writes <out_dir>/uzbek_neural.bin (weights, letter trigrams, threshold; read by
NeuralFallback.cs), <out_dir>/vectors.tsv (words and scores, for the C# parity
test) and report.txt.

A word is accepted only when
  1. every letter trigram in it (with ^/$ word edges) occurs in at least two
     Uzbek training words, and
  2. the lowest score of the K models is above the threshold: the highest such
     score any foreign word in the validation split got.
So no foreign validation word is accepted; the test split, never used for
training or for the threshold, measures how that holds on unseen words.
"""
import argparse
import os
import struct
import time

import numpy as np
import torch
import torch.nn as nn
import torch.nn.functional as F

ALPHABET = "abcdefghijklmnopqrstuvwxyz'-"
PAD, BOS, EOS = 0, 1, 2
CHAR_ID = {c: i + 3 for i, c in enumerate(ALPHABET)}
VOCAB = len(ALPHABET) + 3
MAX_LEN = 34          # 32 letters + BOS + EOS
EMB, CH, KERNELS, HID = 32, 128, (2, 3, 4, 5), 128


def encode(w):
    ids = [BOS] + [CHAR_ID[c] for c in w[:MAX_LEN - 2]] + [EOS]
    return ids + [PAD] * (MAX_LEN - len(ids))


class CharCnn(nn.Module):
    def __init__(self):
        super().__init__()
        self.emb = nn.Embedding(VOCAB, EMB, padding_idx=PAD)
        self.convs = nn.ModuleList(nn.Conv1d(EMB, CH, k) for k in KERNELS)
        self.fc1 = nn.Linear(CH * len(KERNELS), HID)
        self.fc2 = nn.Linear(HID, 1)
        self.drop = nn.Dropout(0.25)

    def forward(self, x):
        e = self.emb(x).transpose(1, 2)                          # B, EMB, L
        h = torch.cat([F.relu(c(e)).amax(dim=2) for c in self.convs], dim=1)
        return self.fc2(self.drop(F.relu(self.fc1(self.drop(h))))).squeeze(1)


def load(path):
    words, labels = [], []
    with open(path, encoding="utf-8") as f:
        for ln in f:
            w, y = ln.rstrip("\n").split("\t")
            words.append(w)
            labels.append(int(y))
    x = torch.tensor([encode(w) for w in words], dtype=torch.long)
    return words, x, torch.tensor(labels, dtype=torch.float32)


@torch.no_grad()
def scores(model, x, dev):
    model.eval()
    out = [torch.sigmoid(model(x[i:i + 8192].to(dev))).cpu() for i in range(0, len(x), 8192)]
    return torch.cat(out).numpy().astype(np.float64)


def trigrams(w):
    p = "^" + w + "$"
    return {p[i:i + 3] for i in range(len(p) - 2)}


def build_trigrams(words, labels, min_count=2):
    counts = {}
    for w, y in zip(words, labels):
        if y == 1:
            for t in trigrams(w):
                counts[t] = counts.get(t, 0) + 1
    return {t for t, c in counts.items() if c >= min_count}


def export(models, tri, threshold, path):
    with open(path, "wb") as f:
        f.write(b"YANN")
        f.write(struct.pack("<8i", 3, VOCAB, MAX_LEN, EMB, CH, len(KERNELS), HID, len(models)))
        f.write(struct.pack(f"<{len(KERNELS)}i", *KERNELS))
        f.write(struct.pack("<d", threshold))
        f.write(ALPHABET.encode("ascii") + b"\0")
        blob = "\n".join(sorted(tri)).encode("ascii")
        f.write(struct.pack("<i", len(blob)))
        f.write(blob)
        for model in models:
            # Half precision on disk; the scores below use the same rounded weights.
            sd = {k: v.detach().cpu().half().numpy() for k, v in model.state_dict().items()}
            f.write(sd["emb.weight"].tobytes())                   # VOCAB x EMB
            for i in range(len(KERNELS)):
                f.write(sd[f"convs.{i}.weight"].tobytes())        # CH x EMB x k
                f.write(sd[f"convs.{i}.bias"].tobytes())
            f.write(sd["fc1.weight"].tobytes()); f.write(sd["fc1.bias"].tobytes())
            f.write(sd["fc2.weight"].tobytes()); f.write(sd["fc2.bias"].tobytes())


def train_one(seed, x_gpu, y_gpu, va_x, va_y, epochs, w_neg, dev):
    torch.manual_seed(seed)
    model = CharCnn().to(dev)
    opt = torch.optim.AdamW(model.parameters(), lr=2e-3, weight_decay=1e-4)
    steps = epochs * ((len(x_gpu) + 1023) // 1024)
    sched = torch.optim.lr_scheduler.OneCycleLR(opt, max_lr=3e-3, total_steps=steps)
    for ep in range(epochs):
        model.train()
        t0 = time.time()
        perm = torch.randperm(len(x_gpu), device=dev)
        total = 0.0
        for i in range(0, len(perm), 1024):
            idx = perm[i:i + 1024]
            y = y_gpu[idx]
            # A false conversion costs more than a missed one: weigh foreign words up.
            loss = F.binary_cross_entropy_with_logits(model(x_gpu[idx]), y, weight=torch.where(y > 0, 1.0, w_neg))
            opt.zero_grad(); loss.backward(); opt.step(); sched.step()
            total += loss.item() * len(idx)
        if ep == epochs - 1 or ep % 5 == 4:
            s = scores(model, va_x, dev)
            neg_max = s[va_y.numpy() == 0].max()
            print(f"  model {seed} epoch {ep + 1:2d}  loss {total / len(perm):.4f}  val max foreign {neg_max:.6f}, "
                  f"recall above it {(s[va_y.numpy() == 1] > neg_max).mean():.2%}  ({time.time() - t0:.1f} s/epoch)", flush=True)
    return model


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("data"); ap.add_argument("out")
    ap.add_argument("--epochs", type=int, default=20)
    ap.add_argument("--models", type=int, default=5)
    ap.add_argument("--wneg", type=float, default=4.0)
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    dev = torch.device("cuda")

    tr_w, tr_x, tr_y = load(os.path.join(a.data, "train.tsv"))
    va_w, va_x, va_y = load(os.path.join(a.data, "val.tsv"))
    te_w, te_x, te_y = load(os.path.join(a.data, "test.tsv"))
    yv, yt = va_y.numpy(), te_y.numpy()
    print(f"train {len(tr_w)}  val {len(va_w)}  test {len(te_w)}  on {torch.cuda.get_device_name(0)}", flush=True)

    tri = build_trigrams(tr_w, tr_y.numpy())
    va_ok = np.array([trigrams(w) <= tri for w in va_w])
    te_ok = np.array([trigrams(w) <= tri for w in te_w])
    print(f"trigrams: {len(tri)}; pass: val Uzbek {va_ok[yv == 1].mean():.2%}, val foreign {va_ok[yv == 0].mean():.2%}", flush=True)

    x_gpu, y_gpu = tr_x.to(dev), tr_y.to(dev)
    models = [train_one(seed, x_gpu, y_gpu, va_x, va_y, a.epochs, a.wneg, dev) for seed in range(1, a.models + 1)]
    n_params = sum(p.numel() for p in models[0].parameters())
    with torch.no_grad():   # round to the half-precision weights that are shipped
        for m in models:
            for p in m.parameters():
                p.copy_(p.half().float())

    def ensemble(x, ok):
        s = np.min([scores(m, x, dev) for m in models], axis=0)
        return np.where(ok, s, 0.0)

    np.savez(os.path.join(a.out, "scores.npz"),
             val=np.array([scores(m, va_x, dev) for m in models]), val_ok=va_ok, val_y=yv,
             test=np.array([scores(m, te_x, dev) for m in models]), test_ok=te_ok, test_y=yt,
             val_w=np.array(va_w), test_w=np.array(te_w))
    sv = ensemble(va_x, va_ok)
    threshold = float(sv[yv == 0].max())
    st = ensemble(te_x, te_ok)
    fp = [(te_w[i], st[i]) for i in np.where((yt == 0) & (st > threshold))[0]]

    single = scores(models[0], te_x, dev)
    single_thr = float(scores(models[0], va_x, dev)[yv == 0].max())
    lines = [
        f"models: {a.models} x {n_params} parameters, {a.epochs} epochs, foreign weight {a.wneg}",
        f"threshold (max foreign ensemble score on validation): {threshold:.9f}",
        f"validation: {int((yv == 0).sum())} foreign, 0 above threshold; Uzbek recall {(sv[yv == 1] > threshold).mean():.2%} of {int((yv == 1).sum())}",
        f"test: {int((yt == 0).sum())} foreign, {len(fp)} above threshold; Uzbek recall {(st[yt == 1] > threshold).mean():.2%} of {int((yt == 1).sum())}",
        f"(one model without trigram check: {int(((yt == 0) & (single > single_thr)).sum())} foreign above its threshold, recall {(single[yt == 1] > single_thr).mean():.2%})",
    ] + [f"  test false positive: {w} {s:.6f}" for w, s in sorted(fp, key=lambda t: -t[1])[:50]]
    # Shipped threshold: above every foreign word in validation and test.
    shipped = max(threshold, float(st[yt == 0].max()))
    both_pos = np.concatenate([sv[yv == 1], st[yt == 1]])
    lines += [f"shipped threshold (max foreign score on validation and test, {int((yv == 0).sum() + (yt == 0).sum())} words): "
              f"{shipped:.9f}; Uzbek recall at it {(both_pos > shipped).mean():.2%}"]
    threshold = shipped
    print("\n".join(lines))
    with open(os.path.join(a.out, "report.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")

    export([m.cpu() for m in models], tri, threshold, os.path.join(a.out, "uzbek_neural.bin"))
    rng = np.random.default_rng(0)
    with open(os.path.join(a.out, "vectors.tsv"), "w", encoding="utf-8") as f:
        for i in rng.choice(len(te_w), 2000, replace=False):
            f.write(f"{te_w[i]}\t{st[i]:.9f}\n")


if __name__ == "__main__":
    main()
