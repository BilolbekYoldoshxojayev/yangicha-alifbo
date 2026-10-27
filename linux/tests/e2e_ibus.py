"""End to end through a real ibus-daemon: an input context selects the installed
YangiAlifbo engine, keys are sent, and the committed text is checked.

    dbus-run-session -- sh -c 'ibus-daemon -dr --panel disable && sleep 2 && python3 e2e_ibus.py'
"""
import sys
import time

import gi

gi.require_version("IBus", "1.0")
from gi.repository import GLib, IBus  # noqa: E402

IBus.init()
bus = IBus.Bus()
assert bus.is_connected(), "ibus-daemon not running"
names = [e.get_name() for e in bus.list_engines()]
assert "yangialifbo" in names, f"engine not registered: {names}"

failures = 0


def pump(seconds=0.3):
    ctx = GLib.MainContext.default()
    end = time.time() + seconds
    while time.time() < end:
        ctx.iteration(False)
        time.sleep(0.01)


def session(text, purpose=None):
    ic = bus.create_input_context("yangialifbo-e2e")
    ic.set_capabilities(IBus.Capabilite.PREEDIT_TEXT | IBus.Capabilite.FOCUS)
    commits = []
    ic.connect("commit-text", lambda _ic, t: commits.append(t.get_text()))
    ic.focus_in()
    ic.set_engine("yangialifbo")
    pump(1.5)   # the engine process starts on first use
    if purpose is not None:
        ic.set_content_type(purpose, 0)
    passed = []
    for ch in text:
        keyval = IBus.unicode_to_keyval(ord(ch))
        handled = ic.process_key_event(keyval, 0, 0)
        if not handled:
            passed.append(ch)   # the app would insert it itself
        ic.process_key_event(keyval, 0, IBus.ModifierType.RELEASE_MASK)
        pump(0.05)
    pump()
    ic.focus_out()
    return "".join(commits), "".join(passed)


def check(name, got, want):
    global failures
    ok = got == want
    failures += not ok
    print(("ok    " if ok else "FAIL  ") + f"{name}: {got!r}" + ("" if ok else f" (expected {want!r})"))


commits, _ = session("shahar choyxona o'rtoq g'alaba ")
check("converts Uzbek words", commits, "şahar çoyxona örtoq ğalaba ")
commits, _ = session("fresh shopping 5-chi ")
check("leaves English, converts ordinals", commits, "fresh shopping 5-çi ")
commits, passed = session("shahar ", IBus.InputPurpose.PASSWORD)
check("password field untouched", (commits, passed), ("", "shahar "))
commits, passed = session("shahar ", IBus.InputPurpose.TERMINAL)
check("terminal untouched", (commits, passed), ("", "shahar "))
sys.exit(1 if failures else 0)
