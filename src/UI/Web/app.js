/* Control center UI. Talks to ControlCenterForm through WebView2 messages:
   page → host: { type, ... }   host → page: { type: 'state' | 'converted', ... } */
(function () {
  'use strict';

  var host = window.chrome && window.chrome.webview;
  var send = function (msg) { if (host) host.postMessage(msg); };
  var $ = function (s) { return document.querySelector(s); };
  var $$ = function (s) { return Array.prototype.slice.call(document.querySelectorAll(s)); };
  var fmt = function (n) { return Number(n).toLocaleString('uz-UZ').replace(/,/g, ' '); };

  var SAMPLE = "O'zbekcha gaplashsak ham bo'laveradi. 5-chi sinf o'quvchilari choyxonada uchrashishdi, yaxwi boldi! Is'hoq akam show va push haqida gapirdi.";
  var state = {};

  // ── Navigation ───────────────────────────────────────────────
  function show(page) {
    $$('.nav__item').forEach(function (b) { b.classList.toggle('is-active', b.dataset.page === page); });
    $$('.page').forEach(function (p) { p.classList.toggle('is-active', p.id === page); });
  }
  $$('.nav__item').forEach(function (b) { b.addEventListener('click', function () { show(b.dataset.page); }); });

  // ── State from host ──────────────────────────────────────────
  function render(s) {
    state = s;
    $$('[data-bind="version"]').forEach(function (e) { e.textContent = 'v' + s.version; });
    $('[data-bind="words"]').textContent = fmt(s.words);
    $('[data-bind="lexicon"]').textContent = fmt(s.lexicon);
    $('[data-bind="reject"]').textContent = fmt(s.reject);
    $('[data-bind="memory"]').textContent = fmt(s.memoryMb);
    $('[data-bind="statusLabel"]').textContent = s.enabled ? 'Konvertatsiya yoqilgan' : 'Konvertatsiya to‘xtatilgan';
    $('[data-status]').classList.toggle('is-off', !s.enabled);
    $('.switch--big').setAttribute('aria-checked', String(s.enabled));
    $$('[data-bind-switch]').forEach(function (b) { b.setAttribute('aria-checked', String(!!s[b.dataset.bindSwitch])); });
    $$('[data-mode]').forEach(function (b) { b.setAttribute('aria-checked', String(b.dataset.mode === s.mode)); });
  }

  // ── Playground ───────────────────────────────────────────────
  var input = $('#input'), output = $('[data-output]');
  var seq = 0, timer = null;
  var NEW = { 'ş': ['s', 0], 'Ş': ['S', 0], 'ç': ['c', 0], 'Ç': ['C', 0], 'ö': ['o', 1], 'Ö': ['O', 1], 'ğ': ['g', 1], 'Ğ': ['G', 1] };

  // Each word gets a no-wrap span so lines never break inside a stacked letter.
  function paint(text) {
    output.textContent = '';
    text.split(/(\s+)/).forEach(function (part) {
      if (!part) return;
      if (/^\s+$/.test(part)) { output.appendChild(document.createTextNode(part)); return; }
      var w = document.createElement('span'); w.className = 'w';
      var buf = '';
      var flush = function () { if (buf) { w.appendChild(document.createTextNode(buf)); buf = ''; } };
      for (var i = 0; i < part.length; i++) {
        var m = NEW[part[i]];
        if (!m) { buf += part[i]; continue; }
        flush();
        var g = document.createElement('span'); g.className = 'g ' + (m[1] ? 'g--up' : 'g--down');
        var acc = document.createElement('span'); acc.className = 'g__acc'; acc.textContent = part[i];
        var base = document.createElement('span'); base.className = 'g__base'; base.textContent = m[0];
        g.appendChild(acc); g.appendChild(base); w.appendChild(g);
      }
      flush();
      output.appendChild(w);
    });
  }
  function words(t) { var m = t.trim().match(/\S+/g); return m ? m.length : 0; }
  function convert() {
    var t = input.value;
    $('[data-bind="inputMeta"]').textContent = t ? words(t) + ' so‘z' : '';
    if (!t) { paint(''); $('[data-bind="outputMeta"]').textContent = ''; return; }
    if (host) send({ type: 'convert', id: ++seq, text: t });
    else paint(t);
  }
  input.addEventListener('input', function () { clearTimeout(timer); timer = setTimeout(convert, 90); });

  // ── Commands ─────────────────────────────────────────────────
  var actions = {
    toggleEnabled: function () { send({ type: 'toggleEnabled' }); },
    toggleSlang: function () { send({ type: 'setSlang', value: !state.slang }); },
    toggleStartup: function () { send({ type: 'setStartup', value: !state.startup }); },
    open: function (el) { send({ type: 'open', target: el.dataset.target }); },
    reload: function (el) {
      send({ type: 'reload' });
      var old = el.textContent; el.textContent = 'Qayta yuklandi'; setTimeout(function () { el.textContent = old; }, 1600);
    },
    hide: function () { send({ type: 'hide' }); },
    exit: function () { send({ type: 'exit' }); },
    sample: function () { input.value = SAMPLE; convert(); input.focus(); },
    clear: function () { input.value = ''; convert(); input.focus(); },
    copy: function (el) {
      var text = output.dataset.text || '';
      if (!text) return;
      send({ type: 'copy', text: text });
      var label = el.querySelector('[data-copy-label]');
      el.classList.add('is-done'); label.textContent = 'Nusxalandi';
      setTimeout(function () { el.classList.remove('is-done'); label.textContent = 'Nusxalash'; }, 1600);
    }
  };
  document.addEventListener('click', function (e) {
    var el = e.target.closest('[data-cmd]');
    if (el && actions[el.dataset.cmd]) actions[el.dataset.cmd](el);
    var mode = e.target.closest('[data-mode]');
    if (mode) send({ type: 'setMode', value: mode.dataset.mode });
  });
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') send({ type: 'hide' });
  });

  if (host) {
    host.addEventListener('message', function (e) {
      var m = e.data;
      if (m.type === 'state') render(m);
      else if (m.type === 'converted' && m.id === seq) {
        output.dataset.text = m.text;
        paint(m.text);
        $('[data-bind="outputMeta"]').textContent = m.changed ? m.changed + ' ta so‘z o‘zgardi' : 'o‘zgarish yo‘q';
      }
    });
    send({ type: 'ready' });
  } else {
    render({ version: '1.5.0', words: 0, lexicon: 947000, reject: 10055, memoryMb: 0, enabled: true, slang: true, startup: true, mode: 'Strict' });
  }
  input.value = SAMPLE;
  convert();
})();
