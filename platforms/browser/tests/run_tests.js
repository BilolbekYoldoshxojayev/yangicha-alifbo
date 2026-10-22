// YangiAlifbo — Browser Extension Automated Test Suite
// Tests Wasm initialization, Manifest V3 structure, Content Script DOM handling,
// Password security immunity, and Caret adjustment.

const fs = require('fs');
const path = require('path');
const vm = require('vm');

const BROWSER_DIR = path.resolve(__dirname, '..');

let totalTests = 0;
let passedTests = 0;
let failedTests = 0;

function assert(condition, message) {
  totalTests++;
  if (condition) {
    passedTests++;
    console.log(`  ✓ PASS: ${message}`);
  } else {
    failedTests++;
    console.error(`  ✗ FAIL: ${message}`);
  }
}

async function runTests() {
  console.log('====================================================');
  console.log('  YangiAlifbo — Browser Extension Test Suite');
  console.log('====================================================\n');

  // TEST SUITE 1: Manifest V3 Schema & File Existence
  console.log('[1/4] Testing Manifest V3 & File References...');
  const manifestPath = path.join(BROWSER_DIR, 'manifest.json');
  assert(fs.existsSync(manifestPath), 'manifest.json exists');
  const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));

  assert(manifest.manifest_version === 3, 'manifest_version is 3');
  assert(manifest.name && manifest.name.includes('YangiAlifbo'), 'Extension name is YangiAlifbo');
  assert(manifest.version === '1.0.0', 'Extension version is 1.0.0');

  // Hard Rule 6: Permissions must only be storage, NO network permissions
  assert(Array.isArray(manifest.permissions) && manifest.permissions.length === 1 && manifest.permissions[0] === 'storage',
    'Strict privacy: Only "storage" permission requested, no network/telemetry permissions');

  // Check icons
  for (const [size, iconRelPath] of Object.entries(manifest.icons)) {
    const iconFullPath = path.join(BROWSER_DIR, iconRelPath);
    assert(fs.existsSync(iconFullPath), `Icon ${size}x${size} exists at ${iconRelPath}`);
    const stat = fs.statSync(iconFullPath);
    assert(stat.size > 0, `Icon ${size}x${size} is non-empty (${stat.size} bytes)`);
  }

  // Check popup, background, content scripts
  assert(fs.existsSync(path.join(BROWSER_DIR, manifest.action.default_popup)), 'Popup HTML exists');
  assert(fs.existsSync(path.join(BROWSER_DIR, manifest.background.service_worker)), 'Background service worker exists');
  assert(fs.existsSync(path.join(BROWSER_DIR, 'popup.css')), 'Popup CSS exists');
  assert(fs.existsSync(path.join(BROWSER_DIR, 'popup.js')), 'Popup JS exists');

  for (const cs of manifest.content_scripts) {
    for (const script of cs.js) {
      assert(fs.existsSync(path.join(BROWSER_DIR, script)), `Content script component ${script} exists`);
    }
  }

  for (const war of manifest.web_accessible_resources) {
    for (const res of war.resources) {
      assert(fs.existsSync(path.join(BROWSER_DIR, res)), `Web accessible resource ${res} exists`);
    }
  }

  // TEST SUITE 2: Wasm Core Functionality in Extension
  console.log('\n[2/4] Testing WebAssembly Core in Browser Module Context...');
  const wasmBinaryPath = path.join(BROWSER_DIR, 'wasm', 'yangi_wasm_bg.wasm');
  const wasmJsPath = path.join(BROWSER_DIR, 'wasm', 'yangi_wasm.js');

  const wasmCode = fs.readFileSync(wasmBinaryPath);
  const wasmJs = fs.readFileSync(wasmJsPath, 'utf8');

  const vmContext = {
    WebAssembly,
    TextDecoder,
    TextEncoder,
    console,
    setTimeout,
    clearTimeout
  };
  vm.createContext(vmContext);
  const wasmBindgen = vm.runInContext(wasmJs + '; wasm_bindgen;', vmContext);
  await wasmBindgen(wasmCode);

  assert(typeof wasmBindgen.convertText === 'function', 'wasm_bindgen.convertText is exported');
  assert(typeof wasmBindgen.convertWord === 'function', 'wasm_bindgen.convertWord is exported');
  assert(typeof wasmBindgen.shouldConvert === 'function', 'wasm_bindgen.shouldConvert is exported');
  assert(typeof wasmBindgen.isUzbek === 'function', 'wasm_bindgen.isUzbek is exported');
  assert(typeof wasmBindgen.isForeignOrReject === 'function', 'wasm_bindgen.isForeignOrReject is exported');

  // Test Uzbek transliteration
  assert(wasmBindgen.convertWord('shahar', 'strict', false, false) === 'şahar', 'shahar -> şahar');
  assert(wasmBindgen.convertWord('chiroq', 'strict', false, false) === 'çiroq', 'chiroq -> çiroq');
  assert(wasmBindgen.convertWord('o\'quvchi', 'strict', false, false) === 'öquvçi', "o'quvchi -> öquvçi");
  assert(wasmBindgen.convertWord('g\'alaba', 'strict', false, false) === 'ğalaba', "g'alaba -> ğalaba");

  // Test Hard Rule 4: English words must NEVER be converted
  assert(wasmBindgen.convertWord('school', 'strict', false, false) === 'school', 'school -> school (preserved)');
  assert(wasmBindgen.convertWord('chat', 'strict', false, false) === 'chat', 'chat -> chat (preserved)');
  assert(wasmBindgen.convertWord('share', 'strict', false, false) === 'share', 'share -> share (preserved)');
  assert(wasmBindgen.convertWord('google', 'strict', false, false) === 'google', 'google -> google (preserved)');

  // Test Tutuq sign protection (Is'hoq, as-hob)
  assert(wasmBindgen.convertWord("Is'hoq", 'strict', false, false) === "Is'hoq", "Is'hoq -> Is'hoq (protected)");

  // Test Slang mode
  assert(wasmBindgen.convertWord('bovot', 'strict', true, false) === 'bövotti', 'bovot -> bövotti with slang');
  assert(wasmBindgen.convertWord('bovot', 'strict', false, false) === 'bovot', 'bovot unchanged without slang');

  // TEST SUITE 3: Content Script Simulation & Event Dispatching
  console.log('\n[3/4] Testing Content Script Interception & Security Guards...');
  
  // Set up full browser mock environment
  const mockStorage = {
    enabled: true,
    mode: 'strict',
    slang: false,
    useMl: false,
    disabledDomains: []
  };

  const listeners = {
    input: [],
    keydown: []
  };

  class MockElement {
    constructor(tagName, attrs = {}) {
      this.tagName = tagName.toUpperCase();
      this.type = attrs.type || 'text';
      this.attributes = { ...attrs };
      this.value = attrs.value || '';
      this.selectionStart = 0;
      this.selectionEnd = 0;
      this.isContentEditable = !!attrs.isContentEditable;
    }

    getAttribute(name) {
      return this.attributes[name] || null;
    }

    setAttribute(name, val) {
      this.attributes[name] = val;
    }

    setSelectionRange(start, end) {
      this.selectionStart = start;
      this.selectionEnd = end;
    }

    closest(selector) {
      return null;
    }

    dispatchEvent(ev) {
      return true;
    }
  }

  const mockWindow = {
    location: { hostname: 'test.uz' },
    getSelection: () => null,
    __yangi_alifbo__: null
  };

  const mockDocument = {
    addEventListener: (type, fn) => {
      if (listeners[type]) listeners[type].push(fn);
    },
    execCommand: (cmd, showUI, text) => {
      return false; // Test the robust direct fallback path
    },
    createRange: () => ({
      setStart: () => {},
      setEnd: () => {},
      collapse: () => {}
    }),
    activeElement: null
  };

  const mockChrome = {
    runtime: {
      getURL: (p) => p
    },
    storage: {
      local: {
        get: (keys, cb) => cb({ ...mockStorage }),
        set: (obj, cb) => {
          Object.assign(mockStorage, obj);
          if (cb) cb();
        }
      },
      onChanged: {
        addListener: () => {}
      }
    }
  };

  const contentContext = {
    window: mockWindow,
    document: mockDocument,
    chrome: mockChrome,
    wasm_bindgen: wasmBindgen,
    Node: { TEXT_NODE: 3 },
    Event: class { constructor(t) { this.type = t; } },
    console,
    setTimeout,
    clearTimeout
  };

  vm.createContext(contentContext);
  const contentCode = fs.readFileSync(path.join(BROWSER_DIR, 'content.js'), 'utf8');
  vm.runInContext(contentCode, contentContext);

  const yangi = mockWindow.__yangi_alifbo__;
  assert(yangi !== undefined, 'Content script exports window.__yangi_alifbo__');

  // Verify Security Guards (Hard Rules 6 & 7):
  const passwordInput = new MockElement('input', { type: 'password' });
  assert(yangi.isIgnoredField(passwordInput) === true, 'SECURITY: <input type="password"> MUST be ignored');

  const acPasswordInput = new MockElement('input', { type: 'text', autocomplete: 'current-password' });
  assert(yangi.isIgnoredField(acPasswordInput) === true, 'SECURITY: autocomplete="current-password" MUST be ignored');

  const ccNumberInput = new MockElement('input', { type: 'text', autocomplete: 'cc-number' });
  assert(yangi.isIgnoredField(ccNumberInput) === true, 'SECURITY: autocomplete="cc-number" MUST be ignored');

  const emailInput = new MockElement('input', { type: 'email' });
  assert(yangi.isIgnoredField(emailInput) === true, 'SECURITY: <input type="email"> MUST be ignored');

  const normalInput = new MockElement('input', { type: 'text' });
  assert(yangi.isIgnoredField(normalInput) === false, 'Standard <input type="text"> is NOT ignored');

  const textarea = new MockElement('textarea');
  assert(yangi.isIgnoredField(textarea) === false, '<textarea> is NOT ignored');

  // TEST SUITE 4: Interactive Typing & Delimiter Word Transliteration
  console.log('\n[4/4] Testing Word Boundary Delimiter Handling & Caret Position...');
  await yangi.initWasm();
  
  // Simulate typing "shahar " into an input
  const inputEl = new MockElement('input', { type: 'text' });
  inputEl.value = 'shahar ';
  inputEl.selectionStart = 7;
  inputEl.selectionEnd = 7;

  // Trigger input event
  yangi.handleInputOrTextarea(inputEl);
  assert(inputEl.value === 'şahar ', 'Word boundary typing: "shahar " transliterated to "şahar "');
  assert(inputEl.selectionStart === 6, 'Caret adjusted backwards by 1 because "shahar" (6) -> "şahar" (5)');

  // Simulate typing non-delimiter: "shah" -> should NOT convert yet
  const partialEl = new MockElement('input', { type: 'text' });
  partialEl.value = 'shah';
  partialEl.selectionStart = 4;
  partialEl.selectionEnd = 4;
  yangi.handleInputOrTextarea(partialEl);
  assert(partialEl.value === 'shah', 'Partial typing "shah" is not prematurely converted');

  // Simulate typing apostrophe: "o'" -> apostrophe is NOT a delimiter, should NOT convert yet
  const apostropheEl = new MockElement('input', { type: 'text' });
  apostropheEl.value = "o'";
  apostropheEl.selectionStart = 2;
  apostropheEl.selectionEnd = 2;
  yangi.handleInputOrTextarea(apostropheEl);
  assert(apostropheEl.value === "o'", "Apostrophe is part of the word, not a delimiter");

  // Now user types space after "o'quvchi "
  const fullWordEl = new MockElement('input', { type: 'text' });
  fullWordEl.value = "o'quvchi ";
  fullWordEl.selectionStart = 9;
  fullWordEl.selectionEnd = 9;
  yangi.handleInputOrTextarea(fullWordEl);
  assert(fullWordEl.value === "öquvçi ", "\"o'quvchi \" transliterated to \"öquvçi \" upon space delimiter");

  // Test English word typing: "school " -> must remain "school "
  const englishEl = new MockElement('input', { type: 'text' });
  englishEl.value = 'school ';
  englishEl.selectionStart = 7;
  englishEl.selectionEnd = 7;
  yangi.handleInputOrTextarea(englishEl);
  assert(englishEl.value === 'school ', 'English word "school " remains untouched upon delimiter (Hard Rule 4)');

  // Test per-site disabling
  yangi.setSettings({ disabledDomains: ['test.uz'] });
  assert(yangi.isDomainActive() === false, 'Site test.uz is successfully recognized as disabled');
  const disabledSiteEl = new MockElement('input', { type: 'text' });
  disabledSiteEl.value = 'shahar ';
  disabledSiteEl.selectionStart = 7;
  disabledSiteEl.selectionEnd = 7;
  yangi.handleInputOrTextarea(disabledSiteEl);
  assert(disabledSiteEl.value === 'shahar ', 'Transliteration bypassed when site is in disabled list');

  console.log('\n====================================================');
  console.log(`  Tests completed: ${totalTests}`);
  console.log(`  Passed: ${passedTests}`);
  console.log(`  Failed: ${failedTests}`);
  console.log('====================================================');

  if (failedTests > 0) {
    process.exit(1);
  }
}

runTests().catch(err => {
  console.error('Test runner fatal error:', err);
  process.exit(1);
});
