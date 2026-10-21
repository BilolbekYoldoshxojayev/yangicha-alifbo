/*
 * YangiAlifbo — browser port of the orthography rules.
 * Mirrors src/Core/UzbekPhonetics.cs (Convert, HasConvertiblePatterns,
 * NormalizeApostrophes) and the delimiter set in src/Win32/KeyboardHook.cs.
 *
 * Scope: rules + the 10,055-word English/code reject filter. The desktop
 * app's 938,526-entry lexicon and morphology are NOT shipped to the browser,
 * so this behaves like the app's Aggressive mode with the reject filter.
 */
(function (root) {
  'use strict';

  // The 9 apostrophe / tutuq variants recognised by the engine.
  var APOSTROPHES = "'‘’ʻʼ`´‛′";

  // Characters that end a word (KeyboardHook.Delimiters).
  var DELIMITERS = " \t\r\n.,!?:;-\"()[]{}/\<>=+*&|^~@#%";

  function isApostrophe(c) { return APOSTROPHES.indexOf(c) !== -1; }
  function isDelimiter(c) { return DELIMITERS.indexOf(c) !== -1; }

  function normalizeApostrophes(text) {
    if (!text) return text;
    var out = '';
    for (var i = 0; i < text.length; i++) out += isApostrophe(text[i]) ? "'" : text[i];
    return out;
  }

  function lower(c) { return c.toLowerCase(); }
  function isUpper(c) { return c !== c.toLowerCase() && c === c.toUpperCase(); }

  // Positions inside s'h / c'h sequences (Is'hoq, mus'haf, as'hob).
  function protectedSet(s) {
    var p = {};
    for (var i = 0; i < s.length - 2; i++) {
      var lo = lower(s[i]);
      if ((lo === 's' || lo === 'c') && s[i + 1] === "'" && lower(s[i + 2]) === 'h') {
        p[i] = p[i + 1] = p[i + 2] = true;
      }
    }
    return p;
  }

  function hasConvertiblePatterns(word) {
    if (!word || word.length < 2) return false;
    var s = normalizeApostrophes(word);
    var p = protectedSet(s);
    for (var i = 0; i < s.length - 1; i++) {
      if (p[i] || p[i + 1]) continue;
      var lo = lower(s[i]), next = s[i + 1], loNext = lower(next);
      if ((lo === 's' || lo === 'c') && loNext === 'h') return true;
      if ((lo === 'o' || lo === 'g') && next === "'") return true;
    }
    return false;
  }

  var MAP = { s: ['ş', 'Ş'], c: ['ç', 'Ç'], o: ['ö', 'Ö'], g: ['ğ', 'Ğ'] };

  /*
   * Returns { text, parts } where parts is a list of
   * { from: 'sh', to: 'ş' } for converted groups and { from: 'a', to: 'a' } otherwise.
   * The parts drive the letter-fusion animations.
   */
  function convertDetailed(word) {
    var parts = [];
    if (!word) return { text: word, parts: parts };
    var s = normalizeApostrophes(word);
    var p = protectedSet(s);
    var i = 0, out = '';
    while (i < s.length) {
      if (!p[i] && i + 1 < s.length && !p[i + 1]) {
        var lo = lower(s[i]), next = s[i + 1], loNext = lower(next);
        var hit = ((lo === 's' || lo === 'c') && loNext === 'h') ||
                  ((lo === 'o' || lo === 'g') && next === "'");
        if (hit) {
          var ch = MAP[lo][isUpper(s[i]) ? 1 : 0];
          parts.push({ from: s[i] + next, to: ch });
          out += ch;
          i += 2;
          continue;
        }
      }
      parts.push({ from: s[i], to: s[i] });
      out += s[i];
      i++;
    }
    return { text: out, parts: parts };
  }

  function convert(word) { return convertDetailed(word).text; }

  // Reject filter, loaded on demand from assets/data/english_reject.txt.
  var reject = null;
  function setRejectList(text) {
    reject = new Set();
    text.split(/\r?\n/).forEach(function (w) { w = w.trim(); if (w) reject.add(normalizeApostrophes(w).toLowerCase()); });
    return reject.size;
  }
  function isRejected(word) {
    return !!reject && reject.has(normalizeApostrophes(word).toLowerCase());
  }

  /* Decision used by the demo: same order as LanguageDetector.ShouldConvert,
     minus the lexicon/morphology tiers that only exist in the desktop app. */
  function decide(word) {
    if (!word || word.trim().length < 2) return { convert: false, reason: 'short' };
    if (!hasConvertiblePatterns(word)) return { convert: false, reason: 'nopattern' };
    if (isRejected(word)) return { convert: false, reason: 'rejected' };
    return { convert: true, reason: 'rules' };
  }

  var api = {
    APOSTROPHES: APOSTROPHES,
    isDelimiter: isDelimiter,
    isApostrophe: isApostrophe,
    normalizeApostrophes: normalizeApostrophes,
    hasConvertiblePatterns: hasConvertiblePatterns,
    convert: convert,
    convertDetailed: convertDetailed,
    setRejectList: setRejectList,
    isRejected: isRejected,
    rejectReady: function () { return !!reject; },
    decide: decide
  };

  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  else root.YAEngine = api;
})(this);
