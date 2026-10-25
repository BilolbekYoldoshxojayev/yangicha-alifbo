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
    var nn = $('[data-bind="neuralNote"]');
    if (nn) nn.textContent = !s.neuralLoaded ? ' Model bu versiyada yo‘q.'
      : s.neuralAccepted ? ' Shu seansda ' + fmt(s.neuralAccepted) + ' ta so‘zni tanidi.' : '';
    if (s.update) renderUpdate(s.update, s.version);
  }

  // ── Updates ──────────────────────────────────────────────────
  function renderUpdate(u, current) {
    var st = $('[data-up-status]'), btn = $('[data-up-btn]'), bar = $('[data-up-bar]');
    var text;
    switch (u.status) {
      case 'Checking': text = 'Tekshirilmoqda…'; break;
      case 'UpToDate': text = 'Siz eng so‘nggi versiyadasiz: v' + current + '.' + (u.lastChecked ? ' Oxirgi tekshiruv ' + u.lastChecked + '.' : ''); break;
      case 'Available':
        text = 'v' + u.latest + ' chiqdi. ' + (u.canSelfUpdate ? '“Hozir yangilash” tugmasini bosing.' : 'Bu nusxa o‘zini almashtira olmaydi — reliz sahifasidan yuklab oling.'); break;
      case 'Downloading': text = 'v' + u.latest + ' yuklanmoqda — ' + u.progress + '%'; break;
      case 'Installing': text = 'O‘rnatilmoqda. Dastur bir necha soniyada qayta ishga tushadi.'; break;
      case 'Failed': text = u.error || 'Yangilashda xatolik.'; break;
      default: text = 'Joriy versiya: v' + current + '. Hali tekshirilmagan.';
    }
    st.textContent = text;
    st.classList.toggle('is-error', u.status === 'Failed');
    bar.hidden = u.status !== 'Downloading';
    bar.firstElementChild.style.width = u.progress + '%';
    var busy = u.status === 'Checking' || u.status === 'Downloading' || u.status === 'Installing';
    btn.disabled = busy;
    btn.textContent = u.status === 'Available' && u.canSelfUpdate ? 'Hozir yangilash' : 'Hozir tekshirish';
    $('[data-up-auto]').setAttribute('aria-checked', String(u.auto));
    $('[data-up-dot]').hidden = !(u.status === 'Available' || u.justUpdated);

    var banner = $('[data-up-banner]');
    banner.hidden = !u.justUpdated;
    if (u.justUpdated && u.shown) banner.textContent = 'YangiAlifbo v' + (u.shown.from || '?') + ' dan v' + u.shown.version + ' ga yangilandi. Quyida nima o‘zgargani.';

    var wrap = $('[data-notes-wrap]');
    wrap.hidden = !(u.shown && u.shown.notes);
    if (u.shown && u.shown.notes && wrap.dataset.v !== u.shown.version) {
      wrap.dataset.v = u.shown.version;
      $('[data-notes-title]').textContent = u.shown.name || ('v' + u.shown.version);
      renderNotes($('[data-notes]'), u.shown.notes);
    }
  }

  // Minimal Markdown → DOM (headings, lists, tables, **bold**, `code`, [links]). Text only, no HTML.
  function inline(el, text) {
    text.split(/(\*\*[^*]+\*\*|`[^`]+`|\[[^\]]+\]\([^)]+\))/).forEach(function (t) {
      if (!t) return;
      var n;
      if (/^\*\*.+\*\*$/.test(t)) { n = document.createElement('b'); n.textContent = t.slice(2, -2); }
      else if (/^`.+`$/.test(t)) { n = document.createElement('code'); n.textContent = t.slice(1, -1); }
      else if (/^\[.+\]\(.+\)$/.test(t)) { n = document.createTextNode(t.slice(1, t.indexOf(']('))); }
      else n = document.createTextNode(t);
      el.appendChild(n);
    });
  }
  function renderNotes(root, md) {
    root.textContent = '';
    var list = null;
    md.split(/\r?\n/).forEach(function (line) {
      var l = line.trim();
      if (!l) { list = null; return; }
      if (/^\|?\s*:?-{3,}/.test(l)) return;
      var h = l.match(/^#{1,6}\s+(.*)$/);
      if (h) { list = null; var e = document.createElement('h3'); inline(e, h[1]); root.appendChild(e); return; }
      var li = l.match(/^[-*]\s+(.*)$/) || l.match(/^\d+\.\s+(.*)$/);
      if (li) {
        if (!list) { list = document.createElement('ul'); root.appendChild(list); }
        var item = document.createElement('li'); inline(item, li[1]); list.appendChild(item); return;
      }
      list = null;
      var p = document.createElement('p');
      inline(p, l.charAt(0) === '|' ? l.split('|').map(function (c) { return c.trim(); }).filter(Boolean).join(' — ') : l);
      root.appendChild(p);
    });
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
    toggleNeural: function () { if (state.neuralLoaded) send({ type: 'setNeural', value: !state.neural }); },
    toggleStartup: function () { send({ type: 'setStartup', value: !state.startup }); },
    toggleAutoUpdate: function () { send({ type: 'setAutoUpdate', value: !(state.update && state.update.auto) }); },
    checkUpdate: function () { send({ type: 'checkUpdate' }); },
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
      else if (m.type === 'page') show(m.page);
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
