# DESIGN-REQUESTS — YangiAlifbo UI/UX Decisions

This log tracks all UI/UX decisions, deferred design choices, and placeholders built across platforms for review by Claude.

## Design Tokens (Mandatory Across All Platforms)
- **Lapis**: `#13205C`
- **Deep**: `#0E1846`
- **Porcelain**: `#EEF1EC`
- **Ink**: `#0F1530`
- **Turquoise**: `#2FB3A6`
- **Saffron**: `#F0B23A` *(Reserved strictly for new diacritics ş, ç, ö, ğ and primary CTA actions)*
- **Fonts**:
  - Display: `Unbounded` (OFL)
  - Text: `Commissioner` (OFL)
  - Available at: `src/UI/Web/fonts/`

---

## Log of Deferred Decisions & Placeholders

| ID | Platform / Component | Status | Deferred Decision | Functional Placeholder Built | Claude Review / Recommendation |
| :--- | :--- | :--- | :--- | :--- | :--- |
| D-01 | Browser Extension Popup | Queued (Workstream D) | Popup dimensions, quick-toggle layout, site exception list UI | Standard HTML/CSS using design tokens and system font fallbacks | Pending implementation |
| D-02 | Android Keyboard Layout | Queued (Workstream E) | Key layout choice (dedicated keys vs long-press popup for ş, ç, ö, ğ) | Simple Latin QWERTY layout with long-press diacritics + settings switch | Pending implementation |
| D-03 | macOS Menu Bar Item | Queued (Workstream F) | Menu bar icon monochrome vs color token, settings popover structure | Monochrome template image in status item with standard NSMenu | Pending implementation |
| D-04 | Linux IME Settings | Queued (Workstream G) | Desktop-native config UI vs GSettings schema | Minimal config file / GSettings schema keys | Pending implementation |
| D-05 | Windows 1.6.1 Notifications | Queued (Workstream H) | Balloon tooltip copy and click behavior | Standard Windows balloon notification with specified Uzbek copy | Pending implementation |
