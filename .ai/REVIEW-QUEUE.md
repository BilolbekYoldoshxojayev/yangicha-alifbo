# REVIEW-QUEUE — Security & Privacy Review

This document logs anything security- or privacy-sensitive (permissions, local storage, network use, entitlements, accessibility APIs, password field detection) across all platforms for formal security review.

## Privacy & Security Guarantees (Non-Negotiable)
1. **Zero Text Transmission**: No platform build may transmit typed text over any network or external IPC.
2. **Zero Text Logging**: No keystroke logging, disk caching of typed text, or telemetry containing user input.
3. **Network Isolation**: Only the Windows updater is permitted network access (HTTPS to GitHub API / GitHub Releases with SHA-256 validation). No other component on any platform connects to the network.
4. **Password Field Protection**: Password fields, secure text fields, and masked inputs must be detected and bypassed immediately on all platforms.
5. **No Release Signing / Publishing**: No tags, stores, or release certificates on `multiplatform`.

---

## Review Queue Items

| ID | Platform | Subsystem | Sensitivity Area | Description & Mechanism | Review Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| SEC-01 | Windows (shipped) | Win32 Hooks | Keystroke interception | `WH_KEYBOARD_LL` low-level hook. Memory-only rolling buffer, password window detection (`ES_PASSWORD` & UI Automation `IsPassword`). | Baseline verified in v1.6.0 |
| SEC-02 | Browser Extension | Manifest V3 | DOM Input Access | Content script listens to `input`/`beforeinput` events in editable elements. Explicitly skips `<input type="password">`, `type="email"`, `type="url"`, `autocomplete="current-password"`, `autocomplete="new-password"`, `autocomplete="cc-number"`, and pre/code tags. Zero network permissions in manifest. | **AUDITED & PASSED** (Workstream D & J: 52 automated tests + CI security lint) |
| SEC-03 | Android | InputMethodService | Keystroke handling | Custom IME service. Requires zero network permissions in `AndroidManifest.xml`. Must check `EditorInfo.inputType` for `TYPE_TEXT_VARIATION_PASSWORD`, `TYPE_TEXT_VARIATION_VISIBLE_PASSWORD`, `TYPE_TEXT_VARIATION_WEB_PASSWORD`. | Queued for Workstream E |
| SEC-04 | macOS | InputMethodKit | Input interception | Input Method Kit server. Must respect `NSWorkspace` / `TSMGetInputSourceProperty` secure input mode (`kTSMDocumentInputModeSecureInput`). Zero network entitlements in sandbox. | Queued for Workstream F |
| SEC-05 | Linux | IBus / Fcitx5 | IME Engine | Native input method engine plugin. Must respect `IBUS_INPUT_PURPOSE_PASSWORD`. Zero network communication. | Queued for Workstream G |
| SEC-06 | iOS | Keyboard Extension | App Extension Sandbox | Custom keyboard extension. `RequestsOpenAccess` must be `NO` (no network, no shared container leaks). Must respect `isSecureTextEntry`. | Queued for Workstream I |
| SEC-07 | Core (Rust) | `yangi-core` | Memory safety | Pure computation engine. Zero external network/file I/O beyond reading the immutable binary dictionary file. Memory mapped safely. | Queued for Workstream B |
