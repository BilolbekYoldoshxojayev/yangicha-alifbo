"""Key handling of the YangiAlifbo input method, without IBus (so it can be tested).

The word being typed is held as preedit. A space, punctuation, Enter or Tab ends
it: the word is committed converted to the new alphabet. Shortcuts, arrows and
other keys commit the word unchanged, as on Windows, where only a finished word
is converted.
"""

APOSTROPHES = "'‘’ʻʼ`´‛′"
END_KEYS = ("Return", "KP_Enter", "Tab", "ISO_Left_Tab")


def is_word_char(ch, word):
    return ch.isalpha() or ch.isdigit() or ch in APOSTROPHES or (ch == "-" and word != "")


class WordBuffer:
    """convert(word) returns the converted word, or None to keep it."""

    def __init__(self, convert):
        self.convert = convert
        self.word = ""

    def converted(self):
        if not self.word:
            return ""
        result = self.convert(self.word)
        return result if result else self.word

    def take(self, converted):
        text = self.converted() if converted else self.word
        self.word = ""
        return text

    def key(self, ch, name, shortcut):
        """Handles one key press.

        ch: the character the key types ("" for none), name: the key name
        (e.g. "BackSpace"), shortcut: Ctrl, Alt or Super is held.
        Returns (consumed, commit): commit is text to insert before the key is
        passed on (when not consumed) or instead of it (when consumed).
        """
        if shortcut:
            return False, self.take(False)
        if name == "BackSpace":
            if self.word:
                self.word = self.word[:-1]
                return True, ""
            return False, ""
        if name in END_KEYS:
            return False, self.take(True)
        if ch and is_word_char(ch, self.word):
            self.word += ch
            return True, ""
        if ch and ch.isprintable():
            return True, self.take(True) + ch
        return False, self.take(False)   # arrows, Escape, Home, F-keys …
