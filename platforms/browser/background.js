// YangiAlifbo — Background Service Worker
// Manages default configuration and extension lifecycle.

chrome.runtime.onInstalled.addListener(async () => {
  const existing = await chrome.storage.local.get([
    'enabled',
    'mode',
    'slang',
    'useMl',
    'disabledDomains'
  ]);

  const defaults = {
    enabled: true,
    mode: 'strict',
    slang: false,
    useMl: false,
    disabledDomains: []
  };

  const toSet = {};
  for (const [k, v] of Object.entries(defaults)) {
    if (existing[k] === undefined) {
      toSet[k] = v;
    }
  }

  if (Object.keys(toSet).length > 0) {
    await chrome.storage.local.set(toSet);
  }

  updateBadge(existing.enabled !== undefined ? existing.enabled : defaults.enabled);
});

chrome.storage.onChanged.addListener((changes, area) => {
  if (area === 'local' && changes.enabled !== undefined) {
    updateBadge(changes.enabled.newValue);
  }
});

function updateBadge(enabled) {
  if (enabled) {
    chrome.action.setBadgeText({ text: '' });
  } else {
    chrome.action.setBadgeText({ text: 'OFF' });
    chrome.action.setBadgeBackgroundColor({ color: '#A0AEC0' });
  }
}
