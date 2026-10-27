"""ctypes binding to libyangialifbo.so (src/YangiAlifbo.Native), the same engine
the Windows app uses: dictionary, morphology, foreign-word filter and neural model."""
import ctypes
import os

STRICT, AGGRESSIVE = 0, 1


class Core:
    def __init__(self, path=None):
        path = path or os.path.join(os.path.dirname(os.path.abspath(__file__)), "libyangialifbo.so")
        lib = ctypes.CDLL(path)
        lib.ya_init.restype = ctypes.c_int
        lib.ya_convert_word.argtypes = [ctypes.c_char_p, ctypes.c_int, ctypes.c_int, ctypes.c_int]
        lib.ya_convert_word.restype = ctypes.c_void_p
        lib.ya_free.argtypes = [ctypes.c_void_p]
        lib.ya_version.restype = ctypes.c_char_p
        if lib.ya_init() != 1:
            raise RuntimeError("libyangialifbo failed to load its dictionaries")
        self._lib = lib
        self.version = lib.ya_version().decode()

    def convert(self, word, mode=STRICT, slang=True, neural=True):
        """The word in the new alphabet, or None when it stays as typed."""
        p = self._lib.ya_convert_word(word.encode("utf-8"), mode, int(slang), int(neural))
        if not p:
            return None
        try:
            return ctypes.string_at(p).decode("utf-8")
        finally:
            self._lib.ya_free(p)
