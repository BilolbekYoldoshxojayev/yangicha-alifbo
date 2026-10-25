"""Compares ensemble rules on the scores train.py saved (scores.npz).

    python analyze.py <out_dir> [<out_dir> ...]

For each rule the threshold is set on one split (max foreign score) and
checked on the other, both ways round, so every number is out-of-sample.
"""
import sys

import numpy as np


def logit(p):
    p = np.clip(p, 1e-12, 1 - 1e-12)
    return np.log(p / (1 - p))


RULES = {
    "min": lambda s: s.min(axis=0),
    "mean-logit": lambda s: 1 / (1 + np.exp(-logit(s).mean(axis=0))),
    "2nd-lowest": lambda s: np.sort(s, axis=0)[1],
}


def main():
    for d in sys.argv[1:]:
        z = np.load(f"{d}/scores.npz")
        print(f"== {d}")
        for name, rule in RULES.items():
            row = []
            for fit, chk in (("val", "test"), ("test", "val")):
                sf = np.where(z[f"{fit}_ok"], rule(z[fit]), 0)
                sc = np.where(z[f"{chk}_ok"], rule(z[chk]), 0)
                yf, yc = z[f"{fit}_y"], z[f"{chk}_y"]
                thr = sf[yf == 0].max()
                fp = np.where((yc == 0) & (sc > thr))[0]
                row.append(f"fit {fit}: thr {thr:.6f}, {chk} FP {len(fp)}/{(yc == 0).sum()} "
                           f"{[str(z[chk + '_w'][i]) for i in fp][:4]}, recall {(sc[yc == 1] > thr).mean():.2%}")
            # final threshold over both splits
            both_s = np.concatenate([np.where(z[f"{k}_ok"], rule(z[k]), 0) for k in ("val", "test")])
            both_y = np.concatenate([z["val_y"], z["test_y"]])
            thr = both_s[both_y == 0].max()
            print(f"  {name:11s} " + " | ".join(row) + f" | both: thr {thr:.6f}, recall {(both_s[both_y == 1] > thr).mean():.2%}")


if __name__ == "__main__":
    main()
