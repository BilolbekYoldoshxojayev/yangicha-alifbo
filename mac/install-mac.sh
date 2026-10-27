#!/bin/sh
# YangiAlifbo for macOS (12+, Apple Silicon and Intel).
#
#   curl -fsSL https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/releases/latest/download/install-mac.sh | sh
#   … | sh -s -- --uninstall
#
# Installs ~/Applications/YangiAlifbo.app. Files fetched by curl carry no quarantine
# flag, so macOS opens the app without the "unidentified developer" block.
# The app then asks for Accessibility permission and updates itself.
set -eu

REPO="BilolbekYoldoshxojayev/yangicha-alifbo"
APP="$HOME/Applications/YangiAlifbo.app"
NAME="YangiAlifbo-mac.zip"

say() { printf '%s\n' "$*"; }
die() { printf 'Xato / Error: %s\n' "$*" >&2; exit 1; }

[ "$(uname -s)" = Darwin ] || die "bu skript macOS uchun (Linux: install.sh)"

if [ "${1:-}" = "--uninstall" ]; then
    pkill -x YangiAlifbo 2>/dev/null || true
    rm -rf "$APP" "$HOME/Library/Logs/YangiAlifbo.log"
    defaults delete uz.yangialifbo.app >/dev/null 2>&1 || true
    tccutil reset Accessibility uz.yangialifbo.app >/dev/null 2>&1 || true
    say "YangiAlifbo o‘chirildi. / YangiAlifbo removed."
    exit 0
fi

tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
if [ -n "${YANGIALIFBO_ZIP:-}" ]; then          # local build (CI, testing)
    cp "$YANGIALIFBO_ZIP" "$tmp/$NAME"
else
    URL="https://github.com/$REPO/releases/latest/download"
    say "Yuklab olinmoqda / Downloading $NAME …"
    curl -fsSL "$URL/$NAME" -o "$tmp/$NAME" || die "yuklab bo‘lmadi: $URL/$NAME"
    curl -fsSL "$URL/$NAME.sha256" -o "$tmp/$NAME.sha256" || die "nazorat summasi topilmadi"
    want=$(cut -d' ' -f1 "$tmp/$NAME.sha256")
    got=$(shasum -a 256 "$tmp/$NAME" | cut -d' ' -f1)
    [ "$want" = "$got" ] || die "SHA-256 mos kelmadi (kutilgan $want, olingan $got)"
fi

pkill -x YangiAlifbo 2>/dev/null || true
mkdir -p "$HOME/Applications"
ditto -x -k "$tmp/$NAME" "$tmp/x"
rm -rf "$APP"
mv "$tmp/x/YangiAlifbo.app" "$APP"
xattr -dr com.apple.quarantine "$APP" 2>/dev/null || true
VERSION=$(defaults read "$APP/Contents/Info" CFBundleShortVersionString)

say "YangiAlifbo v$VERSION o‘rnatildi: $APP"
say ""
say "Birinchi ishga tushganda “Maxsus imkoniyatlar” (Accessibility) ruxsatini bering:"
say "  Tizim sozlamalari → Maxfiylik va xavfsizlik → Maxsus imkoniyatlar → YangiAlifbo"
say "First run: allow it in System Settings → Privacy & Security → Accessibility."
say "Menyu satridagi “Ş” belgisi — sozlamalar. / The “Ş” in the menu bar has the settings."
[ -n "${YANGIALIFBO_NO_OPEN:-}" ] || open "$APP"
