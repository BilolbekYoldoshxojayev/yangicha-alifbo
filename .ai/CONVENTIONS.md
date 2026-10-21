# CONVENTIONS — YangiAlifbo

## Coding Standards
- C# 12 / 13 idioms: file-scoped namespaces, top-level records, primary constructors where applicable.
- Nullable reference types enabled (`#nullable enable`).
- Explicit Win32 struct layouts (`[StructLayout(LayoutKind.Sequential)]`).
- Memory hygiene: All `IDisposable` resources (hooks, win-events, bitmaps, icons) disposed cleanly.
- Thread safety: Tray context owns main UI thread message loop.
