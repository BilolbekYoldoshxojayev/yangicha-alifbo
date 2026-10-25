"""Copies a trained model into the app: Brotli-compressed weights into
src/Core/Dictionaries and the PyTorch scores into tests/data for the parity test.

    python pack.py <out_dir_of_train.py>
"""
import os
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))


def brotli(src, dst):
    try:
        import brotli as b
        with open(src, "rb") as f, open(dst, "wb") as o:
            o.write(b.compress(f.read(), quality=11))
    except ImportError:   # .NET is always there; compress with it
        ps = (f"$i=[IO.File]::OpenRead('{src}'); $o=[IO.File]::Create('{dst}'); "
              "$b=New-Object IO.Compression.BrotliStream($o,[IO.Compression.CompressionLevel]::SmallestSize); "
              "$i.CopyTo($b); $b.Dispose(); $o.Dispose(); $i.Dispose()")
        subprocess.run(["pwsh", "-NoProfile", "-Command", ps], check=True)


def main():
    out = sys.argv[1]
    dst = os.path.join(ROOT, "src", "Core", "Dictionaries", "uzbek_neural.bin.br")
    brotli(os.path.join(out, "uzbek_neural.bin"), dst)
    os.makedirs(os.path.join(ROOT, "tests", "data"), exist_ok=True)
    shutil.copy(os.path.join(out, "vectors.tsv"), os.path.join(ROOT, "tests", "data", "neural_vectors.tsv"))
    shutil.copy(os.path.join(out, "report.txt"), os.path.join(HERE, "report.txt"))
    print(f"{dst}: {os.path.getsize(dst)} bytes")


if __name__ == "__main__":
    main()
