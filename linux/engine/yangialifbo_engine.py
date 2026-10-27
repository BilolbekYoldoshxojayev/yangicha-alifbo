#!/usr/bin/env python3
"""YangiAlifbo input method for IBus (Linux).

Linux, unlike Windows, does not let a program watch and retype keys in other
apps (Wayland forbids it), so YangiAlifbo works as an input method: the word
being typed is shown underlined and, when it ends, is committed in the new
alphabet. The conversion itself is libyangialifbo.so, the Windows engine.

    yangialifbo_engine.py --ibus     started by ibus-daemon (component file)
"""
import json
import os
import subprocess
import sys
import threading
import time
import urllib.request

import gi

gi.require_version("IBus", "1.0")
from gi.repository import GLib, GObject, IBus  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from wordbuffer import WordBuffer  # noqa: E402
from yangialifbo_core import AGGRESSIVE, STRICT, Core  # noqa: E402

CONFIG_DIR = os.path.join(GLib.get_user_config_dir(), "YangiAlifbo")
SETTINGS = os.path.join(CONFIG_DIR, "linux_settings.json")
RELEASES_API = "https://api.github.com/repos/BilolbekYoldoshxojayev/yangicha-alifbo/releases/latest"
INSTALL_CMD = "curl -fsSL https://github.com/BilolbekYoldoshxojayev/yangicha-alifbo/releases/latest/download/install.sh | sh"
SHORTCUT = (IBus.ModifierType.CONTROL_MASK | IBus.ModifierType.MOD1_MASK |
            IBus.ModifierType.SUPER_MASK | IBus.ModifierType.MOD4_MASK)
NO_CONVERT = (IBus.InputPurpose.PASSWORD, IBus.InputPurpose.PIN, IBus.InputPurpose.TERMINAL)

DEFAULTS = {"enabled": True, "mode": "strict", "slang": True, "neural": True, "last_update_check": 0, "latest": ""}


def load_settings():
    try:
        with open(SETTINGS, encoding="utf-8") as f:
            return {**DEFAULTS, **json.load(f)}
    except (OSError, ValueError):
        return dict(DEFAULTS)


def save_settings(s):
    try:
        os.makedirs(CONFIG_DIR, exist_ok=True)
        with open(SETTINGS, "w", encoding="utf-8") as f:
            json.dump(s, f, indent=1)
    except OSError:
        pass


def newer(latest, current):
    parse = lambda v: tuple(int(x) for x in v.lstrip("vV").split("-")[0].split(".")[:3] if x.isdigit())
    try:
        return parse(latest) > parse(current)
    except ValueError:
        return False


try:
    CORE = Core()
except Exception as e:  # the engine still loads and passes keys through
    CORE = None
    print("YangiAlifbo: " + str(e), file=sys.stderr)


class YangiAlifboEngine(IBus.Engine):
    __gtype_name__ = "YangiAlifboEngine"

    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        self.settings = load_settings()
        self.buffer = WordBuffer(self._convert)
        self.passthrough = False
        self._build_props()
        threading.Thread(target=self._check_update, daemon=True).start()

    # ── conversion ───────────────────────────────────────────────
    def _convert(self, word):
        if CORE is None:
            return None
        s = self.settings
        return CORE.convert(word, AGGRESSIVE if s["mode"] == "aggressive" else STRICT, s["slang"], s["neural"])

    def _active(self):
        return self.settings["enabled"] and not self.passthrough and CORE is not None

    # ── keys ─────────────────────────────────────────────────────
    def do_process_key_event(self, keyval, keycode, state):
        if state & IBus.ModifierType.RELEASE_MASK:
            return False
        if not self._active():
            self._flush(False)
            return False
        ch = chr(IBus.keyval_to_unicode(keyval)) if IBus.keyval_to_unicode(keyval) else ""
        consumed, commit = self.buffer.key(ch, IBus.keyval_name(keyval) or "", bool(state & SHORTCUT))
        if commit:
            self.commit_text(IBus.Text.new_from_string(commit))
        self._show_preedit()
        return consumed

    def _show_preedit(self):
        word = self.buffer.word
        if not word:
            self.hide_preedit_text()
            return
        text = IBus.Text.new_from_string(word)
        text.append_attribute(IBus.AttrType.UNDERLINE, IBus.AttrUnderline.SINGLE, 0, len(word))
        # Focus changes and clicks commit the word as typed.
        self.update_preedit_text_with_mode(text, len(word), True, IBus.PreeditFocusMode.COMMIT)

    def _flush(self, converted):
        text = self.buffer.take(converted)
        if text:
            self.commit_text(IBus.Text.new_from_string(text))
        self.hide_preedit_text()

    def do_focus_in(self):
        self.register_properties(self.props)

    def do_focus_out(self):
        self._flush(False)

    def do_reset(self):
        self._flush(False)

    def do_disable(self):
        self._flush(False)

    def do_set_content_type(self, purpose, hints):
        # Password, PIN and terminal fields stay untouched, as on Windows.
        self.passthrough = purpose in NO_CONVERT
        if self.passthrough:
            self._flush(False)

    # ── menu ─────────────────────────────────────────────────────
    def _build_props(self):
        s = self.settings
        on = IBus.PropState.CHECKED
        off = IBus.PropState.UNCHECKED
        mk = lambda key, kind, label, state=off, sub=None, tip="": IBus.Property(
            key=key, prop_type=kind, label=IBus.Text.new_from_string(label), symbol=IBus.Text.new_from_string("Ş"),
            tooltip=IBus.Text.new_from_string(tip), sensitive=True, visible=True, state=state, sub_props=sub)
        modes = IBus.PropList()
        modes.append(mk("mode.strict", IBus.PropType.RADIO, "Qat’iy (lug‘at tasdiqlagan so‘zlar)", on if s["mode"] == "strict" else off))
        modes.append(mk("mode.aggressive", IBus.PropType.RADIO, "Agressiv (barcha so‘zlar)", on if s["mode"] == "aggressive" else off))
        self.props = IBus.PropList()
        self.props.append(mk("enabled", IBus.PropType.TOGGLE, "Konvertatsiya yoqilgan", on if s["enabled"] else off))
        self.props.append(mk("mode", IBus.PropType.MENU, "Rejim", sub=modes))
        self.props.append(mk("slang", IBus.PropType.TOGGLE, "Chat qisqartmalari", on if s["slang"] else off))
        self.props.append(mk("neural", IBus.PropType.TOGGLE, "Neyron model (lug‘atda yo‘q so‘zlar)", on if s["neural"] else off))
        self.props.append(mk("whitelist", IBus.PropType.NORMAL, "Oq ro‘yxatni tahrirlash…"))
        self.props.append(mk("blacklist", IBus.PropType.NORMAL, "Qora ro‘yxatni tahrirlash…"))
        version = CORE.version if CORE else "?"
        if s.get("latest") and newer(s["latest"], version):
            self.props.append(mk("update", IBus.PropType.NORMAL, f"⬆ {s['latest']} chiqdi — yangilash buyrug‘ini nusxalash",
                                 tip="Terminalda ishga tushiring: " + INSTALL_CMD))
        self.props.append(mk("about", IBus.PropType.NORMAL, f"YangiAlifbo v{version}"))

    def do_property_activate(self, name, state):
        s = self.settings
        if name in ("enabled", "slang", "neural"):
            s[name] = state == IBus.PropState.CHECKED
        elif name.startswith("mode.") and state == IBus.PropState.CHECKED:
            s["mode"] = name[5:]
        elif name in ("whitelist", "blacklist"):
            path = os.path.join(CONFIG_DIR, f"user_{name}.txt")
            subprocess.Popen(["xdg-open", path], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            return
        elif name == "update":
            self._copy(INSTALL_CMD)
            return
        else:
            return
        save_settings(s)
        self._flush(False)
        self._build_props()
        self.register_properties(self.props)

    @staticmethod
    def _copy(text):
        for cmd in (["wl-copy"], ["xclip", "-selection", "clipboard"], ["xsel", "-b", "-i"]):
            try:
                subprocess.run(cmd, input=text.encode(), timeout=3, check=True)
                return
            except (OSError, subprocess.SubprocessError):
                continue

    # ── updates: tell, never replace files silently ─────────────
    def _check_update(self):
        s = self.settings
        if time.time() - s.get("last_update_check", 0) < 86400:
            return
        try:
            req = urllib.request.Request(RELEASES_API, headers={"User-Agent": "YangiAlifbo-Linux"})
            with urllib.request.urlopen(req, timeout=10) as r:
                tag = json.load(r).get("tag_name", "")
        except Exception:
            return
        s["last_update_check"] = time.time()
        s["latest"] = tag
        save_settings(s)
        GLib.idle_add(self._refresh_props)

    def _refresh_props(self):
        self._build_props()
        self.register_properties(self.props)
        return False


def main():
    IBus.init()
    loop = GLib.MainLoop()
    bus = IBus.Bus()
    bus.connect("disconnected", lambda *_: loop.quit())
    factory = IBus.Factory.new(bus.get_connection())
    factory.add_engine("yangialifbo", GObject.type_from_name("YangiAlifboEngine"))
    if "--ibus" in sys.argv:
        bus.request_name("org.freedesktop.IBus.YangiAlifbo", 0)
    else:  # run by hand for testing: register the component ourselves
        component = IBus.Component(name="org.freedesktop.IBus.YangiAlifbo", description="YangiAlifbo",
                                   version=CORE.version if CORE else "", license="MIT", author="YangiAlifbo")
        component.add_engine(IBus.EngineDesc(name="yangialifbo", longname="YangiAlifbo", description="Yangi o‘zbek alifbosi",
                                             language="uz", license="MIT", author="YangiAlifbo", layout="default", symbol="Ş"))
        bus.register_component(component)
    loop.run()


if __name__ == "__main__":
    main()
