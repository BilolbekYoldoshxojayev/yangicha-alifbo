/*
 * Browser demo: converts the word before the caret when a delimiter is typed,
 * the same trigger the desktop KeyboardHook uses.
 */
(function () {
  'use strict';

  var E = window.YAEngine;
  var input = document.getElementById('demo-input');
  if (!E || !input) return;

  var logEl = document.querySelector('[data-demo-log]');
  var countEl = document.querySelector('[data-demo-count]');
  var skipEl = document.querySelector('[data-demo-skip]');
  var sampleBtn = document.querySelector('[data-demo-sample]');
  var resetBtn = document.querySelector('[data-demo-reset]');
  var reduce = window.matchMedia('(prefers-reduced-motion: reduce)');

  var SAMPLE = "O'zbekcha gaplashsak ham bo'laveradi. 5-chi sinfda choy ichib, show va chat haqida gapirdik. Is'hoq akam g'alaba qozondi!";
  var converted = 0, kept = 0, typingTimer = null;

  // ── Reject filter ────────────────────────────────────────────────
  var loading = null;
  function loadReject() {
    if (loading) return loading;
    loading = fetch('assets/data/english_reject.txt')
      .then(function (r) { if (!r.ok) throw new Error(r.status); return r.text(); })
      .then(function (t) { E.setRejectList(t); })
      .catch(function () { loading = null; });
    return loading;
  }
  (window.requestIdleCallback || function (f) { setTimeout(f, 800); })(loadReject);
  input.addEventListener('focus', loadReject, { once: true });

  // ── Log ──────────────────────────────────────────────────────────
  function addLog(from, to, isKept) {
    var li = document.createElement('li');
    if (isKept) {
      li.className = 'is-kept';
      li.textContent = from + ' — inglizcha, tegilmadi';
    } else {
      li.appendChild(document.createTextNode(from + ' → '));
      var b = document.createElement('b');
      b.textContent = to;
      li.appendChild(b);
    }
    logEl.insertBefore(li, logEl.firstChild);
    while (logEl.children.length > 8) logEl.removeChild(logEl.lastChild);
  }
  function renderCount() {
    countEl.textContent = String(converted);
    skipEl.textContent = kept ? ', ' + kept + ' ta inglizcha so‘z joyida qoldi' : '';
  }

  // ── Conversion at the caret ──────────────────────────────────────
  // A hyphen right after a letter or digit joins the word (5-chi, kel-chi),
  // as in KeyboardHook; anywhere else it ends the word.
  var WORDCHAR = /[\p{L}\p{N}]/u;
  function isBoundary(text, i) {
    if (text[i] === '-' && i > 0 && WORDCHAR.test(text[i - 1])) return false;
    return E.isDelimiter(text[i]);
  }

  function wordBefore(text, end) {
    var start = end;
    while (start > 0 && !isBoundary(text, start - 1)) start--;
    return { start: start, word: text.slice(start, end) };
  }

  function processCaret() {
    var caret = input.selectionStart;
    if (caret !== input.selectionEnd || caret < 2) return;
    var text = input.value;
    if (!isBoundary(text, caret - 1)) return;
    var wb = wordBefore(text, caret - 1);
    if (!wb.word) return;
    var d = E.decide(wb.word);
    if (d.reason === 'rejected') { kept++; addLog(wb.word, wb.word, true); renderCount(); return; }
    if (!d.convert) return;
    var next = E.convert(wb.word);
    if (next === wb.word) return;
    input.setRangeText(next, wb.start, caret - 1, 'preserve');
    converted++;
    addLog(wb.word, next, false);
    renderCount();
    input.classList.remove('flash');
    void input.offsetWidth;
    input.classList.add('flash');
  }

  var composing = false;
  input.addEventListener('compositionstart', function () { composing = true; });
  input.addEventListener('compositionend', function () { composing = false; processCaret(); });
  input.addEventListener('input', function (e) {
    if (composing || e.isComposing) return;
    var t = e.inputType || 'insertText';
    if (t.indexOf('insert') !== 0 || t === 'insertFromPaste' || t === 'insertFromDrop') return;
    processCaret();
  });

  // ── Sample typing ────────────────────────────────────────────────
  function stopTyping() { if (typingTimer) { clearTimeout(typingTimer); typingTimer = null; } }

  function typeSample() {
    stopTyping();
    reset(false);
    loadReject().then(function () {
      if (reduce.matches) {
        // Type the whole sample in one go, converting word by word.
        for (var i = 0; i < SAMPLE.length; i++) {
          input.setRangeText(SAMPLE[i], input.value.length, input.value.length, 'end');
          processCaret();
        }
        return;
      }
      var i = 0;
      (function step() {
        if (i >= SAMPLE.length) { typingTimer = null; return; }
        var c = SAMPLE[i++];
        input.setRangeText(c, input.value.length, input.value.length, 'end');
        input.dispatchEvent(new InputEvent('input', { inputType: 'insertText', data: c, bubbles: true }));
        input.scrollTop = input.scrollHeight;
        typingTimer = setTimeout(step, isBoundary(input.value, input.value.length - 1) ? 140 : 38 + Math.random() * 40);
      })();
    });
  }

  function reset(focus) {
    stopTyping();
    input.value = '';
    converted = 0; kept = 0;
    logEl.textContent = '';
    renderCount();
    if (focus) input.focus();
  }

  input.addEventListener('keydown', stopTyping);
  input.addEventListener('pointerdown', stopTyping);
  sampleBtn.addEventListener('click', typeSample);
  resetBtn.addEventListener('click', function () { reset(true); });
})();
