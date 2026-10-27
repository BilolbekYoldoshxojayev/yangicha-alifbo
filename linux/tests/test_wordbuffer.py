"""Key handling of the input method (no IBus needed):  python3 -m unittest discover linux/tests"""
import os
import sys
import unittest

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "engine"))
from wordbuffer import WordBuffer  # noqa: E402

TABLE = {"shahar": "şahar", "o'rtoq": "örtoq", "5-chi": "5-çi", "choyxona": "çoyxona"}


def typed(keys, shortcut=False):
    """Feeds (char, keyname) pairs; returns (text the app ends up with, word still in preedit)."""
    b = WordBuffer(lambda w: TABLE.get(w))
    out = []
    for ch, name in keys:
        consumed, commit = b.key(ch, name, shortcut)
        out.append(commit)
        if not consumed:
            if name == "BackSpace" and out:
                text = "".join(out)[:-1]
                out = [text]
            elif ch and not shortcut:
                out.append(ch)
            elif name in ("Return", "Tab"):
                out.append({"Return": "\n", "Tab": "\t"}[name])
    return "".join(out), b.word


def chars(s):
    return [(c, "space" if c == " " else c) for c in s]


class WordBufferTests(unittest.TestCase):
    def test_word_converts_on_space(self):
        self.assertEqual(typed(chars("shahar ")), ("şahar ", ""))

    def test_punctuation_ends_word(self):
        self.assertEqual(typed(chars("o'rtoq, choyxona."))[0], "örtoq, çoyxona.")

    def test_unknown_word_stays(self):
        self.assertEqual(typed(chars("fresh shop ")), ("fresh shop ", ""))

    def test_ordinal_with_hyphen(self):
        self.assertEqual(typed(chars("5-chi "))[0], "5-çi ")

    def test_enter_commits_converted_then_passes(self):
        self.assertEqual(typed(chars("shahar") + [("", "Return")])[0], "şahar\n")

    def test_backspace_edits_word(self):
        self.assertEqual(typed(chars("shaharr") + [("", "BackSpace")] + chars(" "))[0], "şahar ")

    def test_backspace_with_empty_word_passes_to_app(self):
        b = WordBuffer(lambda w: None)
        self.assertEqual(b.key("", "BackSpace", False), (False, ""))

    def test_arrow_commits_word_unchanged(self):
        b = WordBuffer(lambda w: TABLE.get(w))
        for c in "shahar":
            b.key(c, c, False)
        self.assertEqual(b.key("", "Left", False), (False, "shahar"))
        self.assertEqual(b.word, "")

    def test_shortcut_commits_word_unchanged_and_passes(self):
        b = WordBuffer(lambda w: TABLE.get(w))
        for c in "shahar":
            b.key(c, c, False)
        self.assertEqual(b.key("s", "s", True), (False, "shahar"))

    def test_apostrophe_variants_are_part_of_word(self):
        TABLE["o’rtoq"] = "örtoq"
        self.assertEqual(typed(chars("o’rtoq "))[0], "örtoq ")

    def test_leading_hyphen_is_not_a_word(self):
        self.assertEqual(typed(chars("-shahar "))[0], "-şahar ")


if __name__ == "__main__":
    unittest.main()
