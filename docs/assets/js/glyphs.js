/*
 * Letter-fusion rendering shared by the hero, story, apps wall and table.
 * A convertible pair (sh, ch, o', g') becomes:
 *   <span class="gl"><span class="gl__acc">ş</span><span class="gl__base">s</span></span><span class="gl__tail">h</span>
 * The saffron accent glyph sits under the base letter, so once the tail
 * collapses only the new diacritic shows in colour.
 */
(function (root) {
  'use strict';

  var UP = { o: 1, g: 1 };

  function el(tag, cls, text) {
    var n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text != null) n.textContent = text;
    return n;
  }

  /* Renders `word` into a fragment. Returns { node, groups, text, converted }. */
  function build(word, opts) {
    opts = opts || {};
    var detail = root.YAEngine.convertDetailed(word);
    // Assistive tech reads the result once; the stacked glyphs stay hidden from it.
    var out = document.createDocumentFragment();
    var frag = el('span');
    frag.setAttribute('aria-hidden', 'true');
    var groups = [];
    var pos = 0;
    var doConvert = opts.convert !== false && detail.parts.some(function (p) { return p.from !== p.to; });

    detail.parts.forEach(function (p) {
      var orig = word.substr(pos, p.from.length);
      pos += p.from.length;
      if (doConvert && p.from !== p.to) {
        var lo = orig[0].toLowerCase();
        var gl = el('span', 'gl ' + (UP[lo] ? 'is-up' : 'is-down'));
        var acc = el('span', 'gl__acc', p.to);
        var base = el('span', 'gl__base', orig[0]);
        gl.appendChild(acc);
        gl.appendChild(base);
        var tail = el('span', 'gl__tail', orig[1]);
        frag.appendChild(gl);
        frag.appendChild(tail);
        groups.push({ gl: gl, acc: acc, tail: tail, up: !!UP[lo] });
      } else {
        frag.appendChild(document.createTextNode(orig));
      }
    });

    out.appendChild(frag);
    out.appendChild(el('span', 'sr-only', doConvert ? detail.text : word));
    return { node: out, groups: groups, text: detail.text, converted: false };
  }

  function setPre(state) {
    state.groups.forEach(function (g) {
      g.acc.style.opacity = '0';
      g.acc.style.transform = '';
      g.tail.style.width = '';
      g.tail.style.opacity = '';
      g.tail.style.display = '';
    });
    state.converted = false;
  }

  function setDone(state) {
    state.groups.forEach(function (g) {
      g.acc.style.opacity = '1';
      g.acc.style.transform = 'none';
      g.tail.style.display = 'none';
    });
    state.converted = true;
  }

  /* Returns a GSAP timeline that fuses every pair in `state`. */
  function fuseTimeline(state, opts) {
    opts = opts || {};
    var gsap = root.gsap;
    var tl = gsap.timeline({ paused: !!opts.paused });
    var dur = opts.duration || 0.55;
    state.groups.forEach(function (g, i) {
      var w = g.tail.getBoundingClientRect().width;
      var at = i * (opts.stagger != null ? opts.stagger : 0.08);
      tl.fromTo(g.tail, { width: w, opacity: 1 }, { width: 0, opacity: 0, duration: dur * 0.8, ease: 'power3.inOut' }, at);
      tl.fromTo(g.acc,
        { opacity: 0, y: g.up ? '-0.32em' : '0.28em' },
        { opacity: 1, y: 0, duration: dur, ease: 'back.out(2.2)' }, at + dur * 0.25);
    });
    tl.eventCallback('onComplete', function () { state.converted = true; });
    return tl;
  }

  root.YAGlyphs = { build: build, setPre: setPre, setDone: setDone, fuseTimeline: fuseTimeline };
})(window);
