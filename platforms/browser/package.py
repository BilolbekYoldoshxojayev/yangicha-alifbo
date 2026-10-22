#!/usr/bin/env python3
"""
Packages the YangiAlifbo Browser Extension into a clean zip archive ready for loading or distribution.
"""
import os
import zipfile

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = os.path.join(SCRIPT_DIR, 'dist')
OUT_ZIP = os.path.join(OUT_DIR, 'yangi-alifbo-extension-v1.0.0.zip')

FILES_TO_INCLUDE = [
    'manifest.json',
    'background.js',
    'content.js',
    'popup.html',
    'popup.css',
    'popup.js',
    'icons/icon-16.png',
    'icons/icon-32.png',
    'icons/icon-48.png',
    'icons/icon-128.png',
    'wasm/yangi_wasm.js',
    'wasm/yangi_wasm_bg.wasm',
]

def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    if os.path.exists(OUT_ZIP):
        os.remove(OUT_ZIP)

    print(f"Packaging YangiAlifbo extension to {OUT_ZIP}...")
    with zipfile.ZipFile(OUT_ZIP, 'w', compression=zipfile.ZIP_DEFLATED) as zf:
        for rel_path in FILES_TO_INCLUDE:
            full_path = os.path.join(SCRIPT_DIR, rel_path)
            if not os.path.exists(full_path):
                raise FileNotFoundError(f"Required file missing: {full_path}")
            zf.write(full_path, arcname=rel_path)
            print(f"  + {rel_path} ({os.path.getsize(full_path):,} bytes)")

    size_kb = os.path.getsize(OUT_ZIP) / 1024
    print(f"\nDone! Extension package created successfully: {OUT_ZIP} ({size_kb:.1f} KB)")

if __name__ == '__main__':
    main()
