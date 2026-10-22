// YangiAlifbo — Popup Controller
// Manages extension settings, per-site toggling, and live conversion sandbox.

document.addEventListener('DOMContentLoaded', async () => {
  let wasmReady = false;
  let currentDomain = '';
  let settings = {
    enabled: true,
    mode: 'strict',
    slang: false,
    useMl: false,
    disabledDomains: []
  };

  // DOM Elements
  const masterToggle = document.getElementById('master-toggle');
  const siteDomainEl = document.getElementById('site-domain');
  const siteStatusText = document.getElementById('site-status-text');
  const btnToggleSite = document.getElementById('btn-toggle-site');
  const btnModeStrict = document.getElementById('btn-mode-strict');
  const btnModeAggressive = document.getElementById('btn-mode-aggressive');
  const toggleSlang = document.getElementById('toggle-slang');
  const toggleMl = document.getElementById('toggle-ml');
  const sandboxInput = document.getElementById('sandbox-input');
  const sandboxOutput = document.getElementById('sandbox-output');
  const btnCopy = document.getElementById('btn-copy');

  // Initialize Wasm
  async function initWasm() {
    try {
      if (typeof wasm_bindgen !== 'undefined') {
        const wasmUrl = chrome.runtime.getURL('wasm/yangi_wasm_bg.wasm');
        await wasm_bindgen(wasmUrl);
        wasmReady = true;
        updateSandbox();
      }
    } catch (err) {
      console.error('[YangiAlifbo Popup] Wasm init failed:', err);
    }
  }

  // Detect current tab domain
  async function detectCurrentDomain() {
    try {
      const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
      if (tab && tab.url) {
        const url = new URL(tab.url);
        if (url.protocol.startsWith('http')) {
          currentDomain = url.hostname;
          siteDomainEl.textContent = currentDomain;
          siteDomainEl.title = currentDomain;
        } else {
          currentDomain = '';
          siteDomainEl.textContent = 'Ichki sahifa';
          btnToggleSite.disabled = true;
        }
      } else {
        siteDomainEl.textContent = 'Noma\'lum';
        btnToggleSite.disabled = true;
      }
    } catch (e) {
      siteDomainEl.textContent = 'Noma\'lum';
      btnToggleSite.disabled = true;
    }
    updateSiteStatusUI();
  }

  // Load Settings from chrome.storage
  async function loadSettings() {
    return new Promise((resolve) => {
      chrome.storage.local.get(['enabled', 'mode', 'slang', 'useMl', 'disabledDomains'], (data) => {
        if (data.enabled !== undefined) settings.enabled = data.enabled;
        if (data.mode !== undefined) settings.mode = data.mode;
        if (data.slang !== undefined) settings.slang = data.slang;
        if (data.useMl !== undefined) settings.useMl = data.useMl;
        if (data.disabledDomains !== undefined) settings.disabledDomains = data.disabledDomains;

        syncUI();
        resolve();
      });
    });
  }

  // Synchronize UI with settings
  function syncUI() {
    masterToggle.checked = settings.enabled;

    if (settings.mode === 'aggressive') {
      btnModeAggressive.classList.add('active');
      btnModeStrict.classList.remove('active');
    } else {
      btnModeStrict.classList.add('active');
      btnModeAggressive.classList.remove('active');
    }

    toggleSlang.checked = settings.slang;
    toggleMl.checked = settings.useMl;

    updateSiteStatusUI();
    updateSandbox();
  }

  // Update site status badge and button
  function updateSiteStatusUI() {
    if (!currentDomain) {
      siteStatusText.textContent = 'Nofaol';
      siteStatusText.className = 'status-indicator paused';
      btnToggleSite.textContent = 'Mavjud emas';
      return;
    }

    const isSiteDisabled = settings.disabledDomains.includes(currentDomain);
    if (!settings.enabled) {
      siteStatusText.textContent = 'Kengaytma o\'chiq';
      siteStatusText.className = 'status-indicator paused';
      btnToggleSite.textContent = 'Kengaytma o\'chiq';
      btnToggleSite.disabled = true;
    } else if (isSiteDisabled) {
      siteStatusText.textContent = 'To\'xtatilgan';
      siteStatusText.className = 'status-indicator paused';
      btnToggleSite.textContent = 'Ushbu saytda yoqish';
      btnToggleSite.disabled = false;
    } else {
      siteStatusText.textContent = 'Faol';
      siteStatusText.className = 'status-indicator';
      btnToggleSite.textContent = 'Ushbu saytda to\'xtatish';
      btnToggleSite.disabled = false;
    }
  }

  // Live conversion sandbox
  function updateSandbox() {
    const text = sandboxInput.value.trim();
    if (!text) {
      sandboxOutput.innerHTML = '<span style="color: #94A3B8;">Özbekiston, şahar çiroqlari, ğalaba...</span>';
      return;
    }

    if (!wasmReady) {
      sandboxOutput.textContent = 'Wasm yuklanmoqda...';
      return;
    }

    try {
      const converted = wasm_bindgen.convertText(text, settings.mode, settings.slang, settings.useMl);
      
      // Escape HTML
      const escaped = converted
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');

      // Highlight reformed 2024 Latin characters (ş, ç, ö, ğ, Ş, Ç, Ö, Ğ)
      const highlighted = escaped.replace(/([şçöğŞÇÖĞ])/g, '<span class="reformed">$1</span>');
      sandboxOutput.innerHTML = highlighted;
    } catch (err) {
      sandboxOutput.textContent = text;
    }
  }

  // Master Toggle event
  masterToggle.addEventListener('change', async () => {
    settings.enabled = masterToggle.checked;
    await chrome.storage.local.set({ enabled: settings.enabled });
    updateSiteStatusUI();
  });

  // Per-site Toggle event
  btnToggleSite.addEventListener('click', async () => {
    if (!currentDomain || !settings.enabled) return;

    const idx = settings.disabledDomains.indexOf(currentDomain);
    if (idx >= 0) {
      settings.disabledDomains.splice(idx, 1);
    } else {
      settings.disabledDomains.push(currentDomain);
    }

    await chrome.storage.local.set({ disabledDomains: settings.disabledDomains });
    updateSiteStatusUI();
  });

  // Mode Selection
  btnModeStrict.addEventListener('click', async () => {
    settings.mode = 'strict';
    btnModeStrict.classList.add('active');
    btnModeAggressive.classList.remove('active');
    await chrome.storage.local.set({ mode: 'strict' });
    updateSandbox();
  });

  btnModeAggressive.addEventListener('click', async () => {
    settings.mode = 'aggressive';
    btnModeAggressive.classList.add('active');
    btnModeStrict.classList.remove('active');
    await chrome.storage.local.set({ mode: 'aggressive' });
    updateSandbox();
  });

  // Slang Toggle
  toggleSlang.addEventListener('change', async () => {
    settings.slang = toggleSlang.checked;
    await chrome.storage.local.set({ slang: settings.slang });
    updateSandbox();
  });

  // ML Toggle
  toggleMl.addEventListener('change', async () => {
    settings.useMl = toggleMl.checked;
    await chrome.storage.local.set({ useMl: settings.useMl });
    updateSandbox();
  });

  // Sandbox Input
  sandboxInput.addEventListener('input', updateSandbox);

  // Copy button
  btnCopy.addEventListener('click', async () => {
    const rawConverted = sandboxOutput.textContent;
    if (!rawConverted || rawConverted.startsWith('Özbekiston, şahar')) return;

    try {
      await navigator.clipboard.writeText(rawConverted);
      const originalText = btnCopy.textContent;
      btnCopy.textContent = 'Nusxalandi! ✓';
      btnCopy.classList.add('copied');
      setTimeout(() => {
        btnCopy.textContent = originalText;
        btnCopy.classList.remove('copied');
      }, 1500);
    } catch (e) {
      console.error('Clipboard copy failed:', e);
    }
  });

  // Start initialization
  await loadSettings();
  await detectCurrentDomain();
  await initWasm();
});
