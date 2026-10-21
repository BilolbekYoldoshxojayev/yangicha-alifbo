/*
 * Choreography:
 *   1. Opening: wordmark letters rise in, then promise, lede, actions.
 *   2. Hero stage: a word is typed, "probel" is pressed, the pair fuses.
 *   3. Story (scrubbed + pinned on wide screens): the caret walks a sentence,
 *      Uzbek words fuse, English words get a filter mark and stay.
 *   4. Apps wall: four windows type their own line once in view.
 *   5. Finale: the four new diacritics drop onto their base letters.
 * With prefers-reduced-motion every piece renders its final state.
 */
(function () {
  'use strict';

  var root = document.documentElement;
  var E = window.YAEngine, G = window.YAGlyphs;
  var reduceMQ = window.matchMedia('(prefers-reduced-motion: reduce)');
  var gsap = window.gsap;
  var motion = !!gsap && !reduceMQ.matches;
  var $ = function (s, r) { return (r || document).querySelector(s); };
  var $$ = function (s, r) { return Array.prototype.slice.call((r || document).querySelectorAll(s)); };

  if (gsap && window.ScrollTrigger) gsap.registerPlugin(window.ScrollTrigger);

  // ── 1. Opening sequence ──────────────────────────────────────────
  var mark = $('[data-split]');
  var text = mark.textContent;
  mark.textContent = '';
  var sr = document.createElement('span'); sr.className = 'sr-only'; sr.textContent = text;
  var vis = document.createElement('span'); vis.setAttribute('aria-hidden', 'true');
  text.split('').forEach(function (c) { var s = document.createElement('span'); s.className = 'ch'; s.textContent = c; vis.appendChild(s); });
  mark.appendChild(sr); mark.appendChild(vis);

  var intro;
  if (motion) {
    intro = gsap.timeline({ defaults: { ease: 'expo.out' } });
    intro
      .fromTo('.hero__wordmark .ch', { opacity: 0, yPercent: 60, rotate: 6 }, { opacity: 1, yPercent: 0, rotate: 0, duration: 1.1, stagger: 0.035 })
      .fromTo('.hero__promise', { opacity: 0, y: 18 }, { opacity: 1, y: 0, duration: 0.9 }, 0.35)
      .fromTo('.hero__lede', { opacity: 0, y: 14 }, { opacity: 1, y: 0, duration: 0.9 }, 0.5)
      .fromTo('.hero__actions', { opacity: 0, y: 12 }, { opacity: 1, y: 0, duration: 0.9 }, 0.62)
      .fromTo('.stage', { opacity: 0 }, { opacity: 1, duration: 1.2, ease: 'power2.out' }, 0.4)
      .fromTo('.hero__girih', { opacity: 0 }, { opacity: 0.055, duration: 2.4, ease: 'power1.out' }, 0);
  }
  root.classList.remove('js-motion');
  root.classList.add('motion-ready');

  // ── 2. Hero stage ────────────────────────────────────────────────
  var STAGE = ["o'zbekcha", 'shahar', "g'alaba", 'choyxona', "bog'cha", "o'rtoqlar"];
  var stageWord = $('[data-stage-word]');
  var stageCaret = $('[data-stage-caret]');
  var stageKey = $('[data-stage-key]');
  var stageRule = $('[data-stage-rule]');
  var display = function (w) { return w.replace(/'/g, '‘'); };

  function rulesFor(word) {
    var seen = {};
    return E.convertDetailed(word).parts.filter(function (p) {
      if (p.from === p.to || seen[p.from]) return false;
      seen[p.from] = 1; return true;
    }).map(function (p) { return display(p.from) + ' → ' + p.to; }).join('   ');
  }

  if (!motion) {
    var st0 = G.build(display(STAGE[0]));
    stageWord.appendChild(st0.node);
    G.setDone(st0);
    stageRule.textContent = rulesFor(STAGE[0]);
    stageCaret.style.display = 'none';
  } else {
    var idx = 0, timer = null, running = false, visible = true;
    var wait = function (ms, fn) { timer = setTimeout(fn, ms); };

    var cycle = function () {
      if (!visible || document.hidden) { running = false; return; }
      running = true;
      var word = display(STAGE[idx % STAGE.length]);
      idx++;
      stageWord.textContent = '';
      stageRule.textContent = '';
      gsap.set(stageWord, { opacity: 1, y: 0 });
      var n = 0;
      (function type() {
        if (n < word.length) {
          stageWord.textContent = word.slice(0, ++n);
          wait(70 + Math.random() * 70, type);
          return;
        }
        wait(420, function () {
          stageKey.classList.add('is-down');
          var st = G.build(word);
          stageWord.textContent = '';
          stageWord.appendChild(st.node);
          G.setPre(st);
          G.fuseTimeline(st, { duration: 0.7, stagger: 0.12 });
          gsap.fromTo(stageRule, { opacity: 0, x: -8 }, { opacity: 1, x: 0, duration: 0.6, delay: 0.25, ease: 'power3.out' });
          stageRule.textContent = rulesFor(word);
          wait(160, function () { stageKey.classList.remove('is-down'); });
          setTimeout(function () {
            gsap.to(stageWord, { opacity: 0, y: -14, duration: 0.45, ease: 'power2.in', onComplete: function () { wait(260, cycle); } });
          }, 2300);
        });
      })();
    };

    var start = function () { if (!running) { clearTimeout(timer); cycle(); } };
    setTimeout(start, 1100);
    if ('IntersectionObserver' in window) {
      new IntersectionObserver(function (es) { visible = es[0].isIntersecting; if (visible) start(); }).observe($('.stage'));
    }
    document.addEventListener('visibilitychange', function () { if (!document.hidden) start(); });
  }

  // ── 3. Story sentence ────────────────────────────────────────────
  var SENTENCE = "Bog'chada o'g'il-qizlar choy ichib, push va chat haqida gaplashishdi.";
  var KEPT = { push: 1, chat: 1 };
  var storyText = $('[data-story-text]');
  var steps = $$('[data-story-steps] li');
  var tokens = [];

  // Split into words and delimiters, keeping both.
  (function () {
    var buf = '';
    var flush = function () { if (buf) { tokens.push({ w: buf }); buf = ''; } };
    for (var i = 0; i < SENTENCE.length; i++) {
      var c = SENTENCE[i];
      if (E.isDelimiter(c)) { flush(); tokens.push({ d: c }); } else buf += c;
    }
    flush();
  })();

  var words = [];
  tokens.forEach(function (t) {
    if (t.d) { storyText.appendChild(document.createTextNode(t.d)); return; }
    var span = document.createElement('span');
    span.className = 'w';
    var shown = display(t.w);
    var kept = !!KEPT[t.w];
    var st = G.build(shown, { convert: !kept });
    span.appendChild(st.node);
    var caret = document.createElement('span');
    caret.className = 'caret';
    storyText.appendChild(span);
    storyText.appendChild(caret);
    if (kept) span.classList.add('is-kept');
    words.push({ el: span, st: st, kept: kept, caret: caret });
  });

  function setStep(n) { steps.forEach(function (li, i) { li.classList.toggle('is-active', i === n); }); }

  if (!motion) {
    words.forEach(function (w) { G.setDone(w.st); w.caret.style.display = 'none'; });
    steps.forEach(function (li) { li.classList.add('is-active'); });
  } else {
    var firstKept = words.findIndex(function (w) { return w.kept; });
    var buildStory = function () {
      var tl = gsap.timeline({ paused: true });
      words.forEach(function (w) {
        G.setPre(w.st);
        if (w.kept) gsap.set(w.el, { textDecorationColor: 'rgba(15,111,103,0)' });
      });
      gsap.set(words.map(function (w) { return w.caret; }), { autoAlpha: 0 });
      words.forEach(function (w, i) {
        var at = i;
        tl.set(w.caret, { autoAlpha: 1 }, at);
        if (i > 0) tl.set(words[i - 1].caret, { autoAlpha: 0 }, at);
        if (w.st.groups.length) tl.add(G.fuseTimeline(w.st, { duration: 0.7, stagger: 0.12 }), at + 0.2);
        if (w.kept) tl.to(w.el, { textDecorationColor: 'rgba(15,111,103,.55)', duration: 0.5 }, at + 0.2);
      });
      tl.to({}, { duration: 0.8 });
      return tl;
    };
    var stepFor = function (p, tl) {
      var t = p * tl.duration();
      setStep(t < 1 ? 0 : t < firstKept ? 1 : 2);
    };

    // Tail widths are measured, so wait for the display font.
    var setupStory = function () {
      var mm = gsap.matchMedia();
      mm.add('(min-width: 960px)', function () {
        var tl = buildStory();
        window.ScrollTrigger.create({
          trigger: '[data-story]', start: 'top top', end: '+=150%', pin: true, scrub: 0.6,
          animation: tl, onUpdate: function (self) { stepFor(self.progress, tl); }
        });
      });
      mm.add('(max-width: 959px)', function () {
        var tl = buildStory();
        tl.eventCallback('onUpdate', function () { stepFor(tl.progress(), tl); });
        window.ScrollTrigger.create({ trigger: '.doc', start: 'top 70%', once: true, onEnter: function () { tl.timeScale(1.6).play(); } });
      });
      window.ScrollTrigger.refresh();
    };
    if (document.fonts && document.fonts.ready) document.fonts.ready.then(setupStory); else setupStory();
  }

  // ── 4. Apps wall ─────────────────────────────────────────────────
  var wins = $$('[data-type]');
  function typeInto(el, str, done) {
    el.textContent = '';
    var caret = document.createElement('span'); caret.className = 'caret';
    var cur = document.createTextNode('');
    el.appendChild(cur); el.appendChild(caret);
    var word = '', i = 0;
    (function step() {
      if (i >= str.length) {
        if (word) { /* trailing word without delimiter stays as typed */ }
        setTimeout(function () { caret.remove(); if (done) done(); }, 900);
        return;
      }
      var c = str[i++];
      if (E.isDelimiter(c)) {
        if (word && E.hasConvertiblePatterns(word)) {
          var st = G.build(word);
          var holder = document.createElement('span');
          holder.appendChild(st.node);
          cur.textContent = cur.textContent.slice(0, cur.textContent.length - word.length);
          el.insertBefore(holder, caret);
          G.setPre(st);
          G.fuseTimeline(st, { duration: 0.6 });
          cur = document.createTextNode(c);
          el.insertBefore(cur, caret);
        } else {
          cur.textContent += c;
        }
        word = '';
      } else {
        word += c;
        cur.textContent += c;
      }
      setTimeout(step, E.isDelimiter(c) ? 160 : 45 + Math.random() * 45);
    })();
  }
  function finalText(el, str) {
    el.textContent = '';
    var parts = str.split(/(\s+|[.,!?;:\-])/);
    parts.forEach(function (p) {
      if (!p) return;
      if (E.hasConvertiblePatterns(p)) { var st = G.build(p); el.appendChild(st.node); G.setDone(st); }
      else el.appendChild(document.createTextNode(p));
    });
  }
  if (!motion) {
    wins.forEach(function (w) { finalText(w, w.getAttribute('data-type')); });
  } else {
    wins.forEach(function (w) { w.textContent = ' '; });
    window.ScrollTrigger.create({
      trigger: '[data-wall]', start: 'top 72%', once: true,
      onEnter: function () {
        wins.forEach(function (w, i) {
          setTimeout(function () { typeInto(w, w.getAttribute('data-type')); }, i * 650);
        });
      }
    });
  }

  // ── 5. Finale glyphs ─────────────────────────────────────────────
  var acc = $$('.finale__glyphs .fg__acc');
  if (motion) {
    gsap.fromTo(acc, { opacity: 0, y: function (i) { return i < 2 ? '0.25em' : '-0.3em'; } }, {
      opacity: 1, y: 0, stagger: 0.15, ease: 'back.out(2)',
      scrollTrigger: { trigger: '[data-finale]', start: 'top 85%', end: 'top 40%', scrub: 0.8 }
    });
  }

  // Fonts change metrics; recompute pinned positions once they land.
  if (motion && document.fonts && document.fonts.ready) document.fonts.ready.then(function () { window.ScrollTrigger.refresh(); });
})();
