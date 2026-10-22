// YangiAlifbo — Content Script
// High-performance real-time transliteration for input, textarea, and contenteditable.
// Powered by client-side WebAssembly core. Zero network telemetry.

(() => {
  'use strict';

  // State
  let wasmReady = false;
  let isConverting = false;
  let settings = {
    enabled: true,
    mode: 'strict',
    slang: false,
    useMl: false,
    disabledDomains: []
  };

  // Word delimiter regex: spaces, newlines, tabs, and standard punctuation.
  // Apostrophes (' ’ ʻ ʼ `) and hyphens (-) are intentionally excluded because
  // they form legitimate parts of Uzbek orthography and compound words.
  const DELIMITER_REGEX = /[\s.,!?;:()\[\]{}"/\\<>«»]/;

  // Initialize WebAssembly
  async function initWasm() {
    try {
      if (typeof wasm_bindgen !== 'undefined') {
        const wasmUrl = chrome.runtime.getURL('wasm/yangi_wasm_bg.wasm');
        await wasm_bindgen(wasmUrl);
        wasmReady = true;
      }
    } catch (err) {
      console.warn('[YangiAlifbo] Wasm initialization deferred:', err);
    }
  }

  // Load and listen for settings
  function initSettings() {
    if (typeof chrome !== 'undefined' && chrome.storage && chrome.storage.local) {
      chrome.storage.local.get(['enabled', 'mode', 'slang', 'useMl', 'disabledDomains'], (data) => {
        if (data.enabled !== undefined) settings.enabled = data.enabled;
        if (data.mode !== undefined) settings.mode = data.mode;
        if (data.slang !== undefined) settings.slang = data.slang;
        if (data.useMl !== undefined) settings.useMl = data.useMl;
        if (data.disabledDomains !== undefined) settings.disabledDomains = data.disabledDomains;
      });

      chrome.storage.onChanged.addListener((changes, area) => {
        if (area === 'local') {
          if (changes.enabled) settings.enabled = changes.enabled.newValue;
          if (changes.mode) settings.mode = changes.mode.newValue;
          if (changes.slang) settings.slang = changes.slang.newValue;
          if (changes.useMl) settings.useMl = changes.useMl.newValue;
          if (changes.disabledDomains) settings.disabledDomains = changes.disabledDomains.newValue;
        }
      });
    }
  }

  // Check if auto-conversion should run on this domain
  function isDomainActive() {
    if (!settings.enabled) return false;
    const hostname = window.location.hostname;
    if (settings.disabledDomains && settings.disabledDomains.includes(hostname)) {
      return false;
    }
    return true;
  }

  // Hard Rule 6 & 7: Check if field is a password or should be ignored
  function isIgnoredField(el) {
    if (!el) return true;

    // Check input type
    const type = (el.type || '').toLowerCase();
    if (type === 'password') return true;
    if (['email', 'url', 'number', 'tel', 'color', 'file', 'range'].includes(type)) return true;

    // Check autocomplete / sensitive attributes
    const autocomplete = (el.getAttribute('autocomplete') || '').toLowerCase();
    if (autocomplete.includes('password') || autocomplete.includes('cc-') || autocomplete === 'one-time-code') {
      return true;
    }

    // Check element names or IDs that suggest sensitive security data
    const name = (el.name || '').toLowerCase();
    const id = (el.id || '').toLowerCase();
    if (name.includes('password') || id.includes('password')) return true;

    // Check explicit opt-out
    if (el.getAttribute('data-yangi-ignore') === 'true') return true;

    // Check code editors / pre tags
    if (el.closest && el.closest('pre, code, .monaco-editor, .ace_editor, .CodeMirror')) {
      return true;
    }

    return false;
  }

  // Handle input / textarea elements
  function handleInputOrTextarea(target) {
    if (!wasmReady || isConverting || !isDomainActive() || isIgnoredField(target)) return;

    const val = target.value;
    const selStart = target.selectionStart;
    const selEnd = target.selectionEnd;

    // Only process when caret is collapsed (no active selection range)
    if (selStart !== selEnd || selStart <= 0) return;

    // Check if the character just typed (at selStart - 1) is a delimiter
    const prevChar = val[selStart - 1];
    if (!DELIMITER_REGEX.test(prevChar)) return;

    // Find the word immediately preceding the delimiter(s)
    let wordEnd = selStart - 1;
    // Skip any trailing consecutive delimiters
    while (wordEnd > 0 && DELIMITER_REGEX.test(val[wordEnd - 1])) {
      wordEnd--;
    }
    if (wordEnd <= 0) return;

    let wordStart = wordEnd;
    while (wordStart > 0 && !DELIMITER_REGEX.test(val[wordStart - 1])) {
      wordStart--;
    }

    const rawWord = val.substring(wordStart, wordEnd);
    if (!rawWord || rawWord.length < 2) return;

    try {
      if (wasm_bindgen.shouldConvert(rawWord, settings.mode, settings.useMl)) {
        const converted = wasm_bindgen.convertWord(rawWord, settings.mode, settings.slang, settings.useMl);
        if (converted && converted !== rawWord) {
          isConverting = true;
          const delta = converted.length - rawWord.length;

          // Attempt execCommand to preserve Undo/Redo stack
          target.setSelectionRange(wordStart, wordEnd);
          const ok = document.execCommand('insertText', false, converted);

          if (ok) {
            const newPos = selStart + delta;
            target.setSelectionRange(newPos, newPos);
          } else {
            // Fallback: direct value update
            const before = val.substring(0, wordStart);
            const after = val.substring(wordEnd);
            target.value = before + converted + after;
            const newPos = selStart + delta;
            target.setSelectionRange(newPos, newPos);
            target.dispatchEvent(new Event('input', { bubbles: true }));
          }
          isConverting = false;
        }
      }
    } catch (e) {
      isConverting = false;
      console.warn('[YangiAlifbo] Conversion error:', e);
    }
  }

  // Handle contenteditable elements
  function handleContentEditable(target) {
    if (!wasmReady || isConverting || !isDomainActive() || isIgnoredField(target)) return;

    const sel = window.getSelection();
    if (!sel || !sel.rangeCount) return;

    const range = sel.getRangeAt(0);
    if (!range.collapsed) return;

    const textNode = range.startContainer;
    if (!textNode || textNode.nodeType !== Node.TEXT_NODE) return;

    const text = textNode.nodeValue || '';
    const offset = range.startOffset;
    if (offset <= 0) return;

    const prevChar = text[offset - 1];
    if (!DELIMITER_REGEX.test(prevChar)) return;

    let wordEnd = offset - 1;
    while (wordEnd > 0 && DELIMITER_REGEX.test(text[wordEnd - 1])) {
      wordEnd--;
    }
    if (wordEnd <= 0) return;

    let wordStart = wordEnd;
    while (wordStart > 0 && !DELIMITER_REGEX.test(text[wordStart - 1])) {
      wordStart--;
    }

    const rawWord = text.substring(wordStart, wordEnd);
    if (!rawWord || rawWord.length < 2) return;

    try {
      if (wasm_bindgen.shouldConvert(rawWord, settings.mode, settings.useMl)) {
        const converted = wasm_bindgen.convertWord(rawWord, settings.mode, settings.slang, settings.useMl);
        if (converted && converted !== rawWord) {
          isConverting = true;
          const delta = converted.length - rawWord.length;

          const wordRange = document.createRange();
          wordRange.setStart(textNode, wordStart);
          wordRange.setEnd(textNode, wordEnd);

          sel.removeAllRanges();
          sel.addRange(wordRange);

          const ok = document.execCommand('insertText', false, converted);
          if (ok) {
            const newOffset = offset + delta;
            const newRange = document.createRange();
            newRange.setStart(textNode, Math.min(newOffset, (textNode.nodeValue || '').length));
            newRange.collapse(true);
            sel.removeAllRanges();
            sel.addRange(newRange);
          } else {
            // Direct replacement fallback
            textNode.nodeValue = text.substring(0, wordStart) + converted + text.substring(wordEnd);
            const newOffset = offset + delta;
            const newRange = document.createRange();
            newRange.setStart(textNode, Math.min(newOffset, textNode.nodeValue.length));
            newRange.collapse(true);
            sel.removeAllRanges();
            sel.addRange(newRange);
            target.dispatchEvent(new Event('input', { bubbles: true }));
          }
          isConverting = false;
        }
      }
    } catch (e) {
      isConverting = false;
      console.warn('[YangiAlifbo] ContentEditable conversion error:', e);
    }
  }

  // Unified event dispatcher
  function onInput(e) {
    const target = e.target;
    if (!target) return;

    const tag = target.tagName ? target.tagName.toUpperCase() : '';
    if (tag === 'INPUT' || tag === 'TEXTAREA') {
      handleInputOrTextarea(target);
    } else if (target.isContentEditable) {
      handleContentEditable(target);
    }
  }

  // Shortcut handler: Alt+Shift+U transliterates selected text or current element
  function onKeyDown(e) {
    if (e.altKey && e.shiftKey && (e.key === 'U' || e.key === 'u')) {
      e.preventDefault();
      convertSelectionOrActiveElement();
    }
  }

  // Manual convert selection or active element
  function convertSelectionOrActiveElement() {
    if (!wasmReady) return;
    const active = document.activeElement;
    if (isIgnoredField(active)) return;

    // Check if input or textarea has a selection
    if (active && (active.tagName === 'INPUT' || active.tagName === 'TEXTAREA')) {
      const start = active.selectionStart;
      const end = active.selectionEnd;
      if (start !== end) {
        const selected = active.value.substring(start, end);
        const converted = wasm_bindgen.convertText(selected, settings.mode, settings.slang, settings.useMl);
        active.setSelectionRange(start, end);
        if (!document.execCommand('insertText', false, converted)) {
          active.value = active.value.substring(0, start) + converted + active.value.substring(end);
          active.setSelectionRange(start + converted.length, start + converted.length);
          active.dispatchEvent(new Event('input', { bubbles: true }));
        }
        return;
      }
    }

    // Check window selection
    const sel = window.getSelection();
    if (sel && !sel.isCollapsed && sel.rangeCount > 0) {
      const selected = sel.toString();
      if (selected) {
        const converted = wasm_bindgen.convertText(selected, settings.mode, settings.slang, settings.useMl);
        document.execCommand('insertText', false, converted);
      }
    }
  }

  // Register listeners
  document.addEventListener('input', onInput, true);
  document.addEventListener('keydown', onKeyDown, true);

  // Initialize
  initSettings();
  initWasm();

  // Export for test suites or console debugging
  window.__yangi_alifbo__ = {
    initWasm,
    getSettings: () => ({ ...settings }),
    setSettings: (s) => Object.assign(settings, s),
    isDomainActive,
    isIgnoredField,
    handleInputOrTextarea,
    handleContentEditable
  };
})();
