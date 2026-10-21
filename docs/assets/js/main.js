/* Navigation, letter table, pipeline diagram, checksum copy and hotkey. */
(function () {
  'use strict';

  var $ = function (s, r) { return (r || document).querySelector(s); };
  var $$ = function (s, r) { return Array.prototype.slice.call((r || document).querySelectorAll(s)); };
  var reduce = window.matchMedia('(prefers-reduced-motion: reduce)');
  var G = window.YAGlyphs;

  // ── Navigation ───────────────────────────────────────────────────
  var nav = $('[data-nav]');
  var toggle = $('[data-menu-toggle]');
  function setMenu(open) {
    nav.classList.toggle('is-open', open);
    toggle.setAttribute('aria-expanded', String(open));
    toggle.querySelector('.sr-only').textContent = open ? 'Menyuni yopish' : 'Menyuni ochish';
  }
  toggle.addEventListener('click', function () { setMenu(!nav.classList.contains('is-open')); });
  $$('#nav-links a').forEach(function (a) { a.addEventListener('click', function () { setMenu(false); }); });
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && nav.classList.contains('is-open')) { setMenu(false); toggle.focus(); }
  });

  var lastY = window.scrollY;
  function onScroll() {
    var y = window.scrollY;
    nav.classList.toggle('is-solid', y > 40);
    var hide = y > lastY && y > window.innerHeight * 0.9 && !nav.classList.contains('is-open') && !nav.contains(document.activeElement);
    nav.classList.toggle('is-hidden', hide);
    lastY = y;
  }
  window.addEventListener('scroll', onScroll, { passive: true });
  onScroll();

  // Highlight the section in view.
  var links = $$('#nav-links a[href^="#"]');
  if ('IntersectionObserver' in window) {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (en) {
        if (!en.isIntersecting) return;
        links.forEach(function (a) { a.setAttribute('aria-current', String(a.getAttribute('href') === '#' + en.target.id)); });
      });
    }, { rootMargin: '-45% 0px -50% 0px' });
    links.forEach(function (a) { var t = $(a.getAttribute('href')); if (t) io.observe(t); });
  }

  // ── Letter table: rows convert their example ─────────────────────
  $$('[data-row]').forEach(function (row) {
    var holder = $('[data-ex]', row);
    var old = row.getAttribute('data-old');
    var state = null, tl = null;
    function render() {
      holder.textContent = '';
      state = G.build(old);
      holder.appendChild(state.node);
      G.setPre(state);
    }
    function play() {
      if (state && state.converted) return;
      if (reduce.matches || !window.gsap) { G.setDone(state); return; }
      tl = G.fuseTimeline(state);
    }
    function back() {
      if (tl) { tl.kill(); tl = null; }
      render();
    }
    render();
    row.setAttribute('aria-label', old + ' yangi alifboda ' + row.getAttribute('data-new'));
    row.addEventListener('mouseenter', play);
    row.addEventListener('focus', play);
    row.addEventListener('click', function () { state.converted ? back() : play(); });
    row.addEventListener('keydown', function (e) {
      if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); state.converted ? back() : play(); }
    });
    row.addEventListener('mouseleave', back);
    row.addEventListener('blur', back);
  });

  // ── Pipeline diagram ─────────────────────────────────────────────
  // Verdicts below are the desktop engine's real Strict-mode answers.
  var VERDICTS = {
    'shahar': { stop: 2, ok: true, why: 'Filtrda yo‘q, lug‘atda bor. O‘zgartiriladi.' },
    'show': { stop: 1, ok: false, why: 'Inglizcha so‘zlar filtrida bor. Tegilmaydi.' },
    'gaplashishdi': { stop: 3, ok: true, why: 'Aynan shu shakl lug‘atda yo‘q, lekin qo‘shimchalar ajratilgach o‘zagi topildi. O‘zgartiriladi.' },
    "Is'hoq": { stop: 0, ok: false, why: 'Tutuq belgisi s va h ni ajratib turibdi, bu ş emas. Tegilmaydi.' },
    'Toshkent': { stop: 3, ok: false, why: 'Filtrda ham, lug‘atda ham yo‘q, morfologiya ham tasdiqlamadi. Qat’iy rejimda noma’lum so‘zga tegilmaydi.' }
  };
  var pipe = $('[data-pipe]');
  if (pipe) {
    var stations = $$('.pipe__st', pipe);
    var token = $('[data-pipe-token]', pipe);
    var inEl = $('[data-pipe-in]', pipe), outEl = $('[data-pipe-out]', pipe), whyEl = $('[data-pipe-why]', pipe);
    var radios = $$('[data-word]', pipe);

    var choose = function (btn, focus) {
      var word = btn.getAttribute('data-word');
      var v = VERDICTS[word];
      radios.forEach(function (b) { var on = b === btn; b.setAttribute('aria-checked', String(on)); b.tabIndex = on ? 0 : -1; });
      if (focus) btn.focus();

      stations.forEach(function (st, i) {
        st.classList.remove('is-pass', 'is-stop', 'is-accept', 'is-skip');
        if (i < v.stop) st.classList.add('is-pass');
        else if (i === v.stop) { st.classList.add('is-stop'); if (v.ok) st.classList.add('is-accept'); }
        else st.classList.add('is-skip');
      });
      var display = word.replace("'", '’');
      token.textContent = display;
      var target = stations[v.stop];
      token.style.transform = 'translate(' + target.offsetLeft + 'px, -110%)';

      inEl.textContent = display;
      outEl.textContent = '';
      if (v.ok) {
        var st = G.build(word);
        outEl.appendChild(st.node);
        if (reduce.matches || !window.gsap) G.setDone(st);
        else { G.setPre(st); G.fuseTimeline(st, { stagger: 0.12 }).delay(0.35); }
      } else {
        outEl.textContent = display;
      }
      whyEl.textContent = v.why;
    };
    radios.forEach(function (b, i) {
      b.tabIndex = i === 0 ? 0 : -1;
      b.addEventListener('click', function () { choose(b); });
      b.addEventListener('keydown', function (e) {
        var d = (e.key === 'ArrowRight' || e.key === 'ArrowDown') ? 1 : (e.key === 'ArrowLeft' || e.key === 'ArrowUp') ? -1 : 0;
        if (!d) return;
        e.preventDefault();
        choose(radios[(i + d + radios.length) % radios.length], true);
      });
    });
    var initial = function () { choose(radios[0]); };
    if (document.fonts && document.fonts.ready) document.fonts.ready.then(initial); else initial();
    window.addEventListener('resize', function () {
      var on = radios.filter(function (b) { return b.getAttribute('aria-checked') === 'true'; })[0];
      var v = VERDICTS[on.getAttribute('data-word')];
      token.style.transform = 'translate(' + stations[v.stop].offsetLeft + 'px, -110%)';
    });
  }

  // ── Checksum copy ────────────────────────────────────────────────
  var copyBtn = $('[data-copy]');
  if (copyBtn) {
    copyBtn.addEventListener('click', function () {
      var value = $('[data-hash]').textContent.trim();
      var label = $('[data-copy-label]', copyBtn);
      var done = function (ok) {
        label.textContent = ok ? 'Nusxalandi' : 'Qo‘lda belgilang';
        copyBtn.classList.toggle('is-done', ok);
        setTimeout(function () { label.textContent = 'Nusxalash'; copyBtn.classList.remove('is-done'); }, 2000);
      };
      if (navigator.clipboard && window.isSecureContext) navigator.clipboard.writeText(value).then(function () { done(true); }, function () { done(false); });
      else {
        var r = document.createRange(); r.selectNodeContents($('[data-hash]'));
        var s = window.getSelection(); s.removeAllRanges(); s.addRange(r);
        done(false);
      }
    });
  }

  // ── Hotkey: Ctrl + Shift + U toggles the mock tray state ─────────
  var hk = $('[data-hotkey]');
  if (hk) {
    var keys = { Control: $('[data-k="Control"]', hk), Shift: $('[data-k="Shift"]', hk), U: $('[data-k="U"]', hk) };
    var text = $('[data-hotkey-text]', hk);
    var on = true;
    var flip = function () {
      on = !on;
      hk.classList.toggle('is-off', !on);
      text.textContent = on ? 'Yoqilgan' : 'O‘chirilgan';
    };
    var keyName = function (e) { return e.key === 'Control' || e.key === 'Shift' ? e.key : (e.code === 'KeyU' ? 'U' : null); };
    document.addEventListener('keydown', function (e) {
      var k = keyName(e);
      if (k) keys[k].classList.add('is-down');
      if (e.ctrlKey && e.shiftKey && e.code === 'KeyU' && !e.repeat) { e.preventDefault(); flip(); }
    });
    document.addEventListener('keyup', function (e) { var k = keyName(e); if (k) keys[k].classList.remove('is-down'); });
    window.addEventListener('blur', function () { Object.keys(keys).forEach(function (k) { keys[k].classList.remove('is-down'); }); });
    $('[data-hotkey-btn]', hk).addEventListener('click', function () {
      var seq = [keys.Control, keys.Shift, keys.U];
      seq.forEach(function (k, i) { setTimeout(function () { k.classList.add('is-down'); }, i * 90); });
      setTimeout(function () { flip(); seq.forEach(function (k) { k.classList.remove('is-down'); }); }, 380);
    });
  }
})();
