#!/bin/sh
# YangiAlifbo for Linux: installs the input method for your user.
#
#   curl -fsSL https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/releases/latest/download/install.sh | sh
#   … | sh -s -- --uninstall
#
# Files go to ~/.local/share/yangialifbo. IBus only reads engine descriptions
# from /usr/share/ibus/component, so that one small file is written with sudo.
# Running the command again updates to the latest version.
set -eu

REPO="BilolbekYoldoshxojayev/yangicha-alifbo"
DEST="${XDG_DATA_HOME:-$HOME/.local/share}/yangialifbo"
COMPONENT_DIR="${YANGIALIFBO_COMPONENT_DIR:-/usr/share/ibus/component}"
COMPONENT="$COMPONENT_DIR/yangialifbo.xml"

say() { printf '%s\n' "$*"; }
die() { printf 'Xato / Error: %s\n' "$*" >&2; exit 1; }

as_root() {
    if [ -w "$COMPONENT_DIR" ] || [ "$(id -u)" = 0 ]; then "$@"
    elif command -v sudo >/dev/null 2>&1; then sudo "$@"
    else die "sudo topilmadi; quyidagini root sifatida bajaring: $*"
    fi
}

restart_ibus() {
    if command -v ibus >/dev/null 2>&1; then
        ibus write-cache >/dev/null 2>&1 || true
        if pgrep -u "$(id -u)" -x ibus-daemon >/dev/null 2>&1; then ibus restart >/dev/null 2>&1 || true; fi
    fi
}

if [ "${1:-}" = "--uninstall" ]; then
    rm -rf "$DEST"
    [ -e "$COMPONENT" ] && as_root rm -f "$COMPONENT"
    restart_ibus
    say "YangiAlifbo o‘chirildi. / YangiAlifbo removed."
    exit 0
fi

case "$(uname -m)" in
    x86_64|amd64) ARCH=x64 ;;
    aarch64|arm64) ARCH=arm64 ;;
    *) die "bu protsessor qo‘llab-quvvatlanmaydi: $(uname -m) (x86_64 yoki arm64 kerak)" ;;
esac

# ── Requirements: IBus and its Python bindings ──────────────────────
if ! command -v ibus >/dev/null 2>&1 || ! /usr/bin/env python3 -c \
        "import gi; gi.require_version('IBus', '1.0'); from gi.repository import IBus" >/dev/null 2>&1; then
    id_like=$(. /etc/os-release 2>/dev/null; echo "${ID:-} ${ID_LIKE:-}")
    case "$id_like" in
        *debian*|*ubuntu*) cmd="sudo apt install ibus python3-gi gir1.2-ibus-1.0" ;;
        *fedora*|*rhel*)   cmd="sudo dnf install ibus python3-gobject ibus-libs" ;;
        *arch*)            cmd="sudo pacman -S ibus python-gobject" ;;
        *suse*)            cmd="sudo zypper install ibus python3-gobject typelib-1_0-IBus-1_0" ;;
        *)                 cmd="(paket menejeringiz bilan) ibus va Python GObject (gi) bog‘lamalarini o‘rnating" ;;
    esac
    die "IBus yoki uning Python bog‘lamalari yo‘q. Avval o‘rnating:
    $cmd
so‘ng bu buyruqni qayta ishga tushiring."
fi

# ── Download and verify ──────────────────────────────────────────────
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
NAME="yangialifbo-linux-$ARCH.tar.gz"
if [ -n "${YANGIALIFBO_TARBALL:-}" ]; then        # local build (CI, testing)
    cp "$YANGIALIFBO_TARBALL" "$tmp/$NAME"
else
    URL="https://github.com/$REPO/releases/latest/download"
    say "Yuklab olinmoqda / Downloading $NAME …"
    curl -fsSL "$URL/$NAME" -o "$tmp/$NAME" || die "yuklab bo‘lmadi: $URL/$NAME"
    curl -fsSL "$URL/$NAME.sha256" -o "$tmp/$NAME.sha256" || die "nazorat summasi topilmadi"
    want=$(cut -d' ' -f1 "$tmp/$NAME.sha256")
    got=$(sha256sum "$tmp/$NAME" | cut -d' ' -f1)
    [ "$want" = "$got" ] || die "SHA-256 mos kelmadi (kutilgan $want, olingan $got)"
fi
mkdir -p "$tmp/x"
tar -xzf "$tmp/$NAME" -C "$tmp/x"

# ── Install ──────────────────────────────────────────────────────────
rm -rf "$DEST"
mkdir -p "$DEST"
cp -R "$tmp/x/yangialifbo/." "$DEST/"
chmod +x "$DEST/yangialifbo-engine"
VERSION=$(cat "$DEST/VERSION")

cat > "$tmp/yangialifbo.xml" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<component>
  <name>org.freedesktop.IBus.YangiAlifbo</name>
  <description>YangiAlifbo — yangi o‘zbek alifbosi</description>
  <exec>$DEST/yangialifbo-engine --ibus</exec>
  <version>$VERSION</version>
  <author>YangiAlifbo</author>
  <license>MIT</license>
  <homepage>https://bilolbekyoldoshxojayev.github.io/yangicha-alifbo/</homepage>
  <textdomain>yangialifbo</textdomain>
  <engines>
    <engine>
      <name>yangialifbo</name>
      <language>uz</language>
      <license>MIT</license>
      <author>YangiAlifbo</author>
      <layout>default</layout>
      <longname>YangiAlifbo</longname>
      <description>Odatdagidek yozing: so‘zlar yangi alifboga o‘giriladi (sh → ş, ch → ç, o‘ → ö, g‘ → ğ).</description>
      <symbol>Ş</symbol>
      <rank>0</rank>
    </engine>
  </engines>
</component>
EOF
if ! cmp -s "$tmp/yangialifbo.xml" "$COMPONENT" 2>/dev/null; then
    say "IBus dvigatel tavsifini $COMPONENT ga yozish uchun sudo paroli so‘ralishi mumkin."
    as_root mkdir -p "$COMPONENT_DIR"
    as_root install -m 644 "$tmp/yangialifbo.xml" "$COMPONENT"
fi
restart_ibus

say ""
say "YangiAlifbo v$VERSION o‘rnatildi. / installed."
say ""
say "Yoqish / Turn it on:"
say "  GNOME (Ubuntu, Fedora): Sozlamalar → Klaviatura → Kiritish manbalari → + → O‘zbek → YangiAlifbo"
say "                          Settings → Keyboard → Input Sources → + → Uzbek → YangiAlifbo"
say "  KDE va boshqalar / KDE and others: ibus-setup → Input Method → Add → Uzbek → YangiAlifbo"
say "  Almashtirish / Switch: Super+Space"
say ""
say "Agar ro‘yxatda ko‘rinmasa, tizimdan chiqib qayta kiring. / If it is not listed yet, log out and back in."
