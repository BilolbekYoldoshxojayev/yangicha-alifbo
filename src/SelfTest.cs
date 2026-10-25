// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — SelfTest.cs
// `YangiAlifbo.exe --selftest <outDir> [words.txt]` exercises the published
// binary itself: engine answers, COM (UI Automation, WebView2), keyboard hook,
// updater JSON and the control center UI. Used to prove a trimmed build behaves
// exactly like an untrimmed one. Does not touch running instances or settings.
// ──────────────────────────────────────────────────────────────────────

using System.Diagnostics;
using System.Text;
using YangiAlifbo.Core;
using YangiAlifbo.UI;
using YangiAlifbo.Win32;

namespace YangiAlifbo;

internal static class SelfTest
{
    private sealed class FakeHost : IControlCenterHost
    {
        public bool IsEnabled { get; set; } = true;
        public TransliterationMode Mode { get; set; } = TransliterationMode.Strict;
        public bool IsChatSlangNormalizerEnabled { get; set; } = true;
        public bool IsNeuralEnabled { get; set; } = true;
        public UpdateController Updates { get; } = new((_, _) => { }, () => { });
        private bool _startup = true;
        public Action? Changed;
        public void ToggleEnabled() { IsEnabled = !IsEnabled; Changed?.Invoke(); }
        public void SetMode(TransliterationMode m) { Mode = m; Changed?.Invoke(); }
        public void SetChatSlang(bool e) { IsChatSlangNormalizerEnabled = e; Changed?.Invoke(); }
        public void SetNeural(bool e) { IsNeuralEnabled = e; Changed?.Invoke(); }
        public bool GetIsStartupEnabled() => _startup;
        public void SetStartup(bool e) { _startup = e; Changed?.Invoke(); }
        public void OpenWhitelist() { }
        public void OpenBlacklist() { }
        public void ExitApplication() { }
    }

    private static readonly List<string> Log = new();
    private static int _failures;

    private static string? _logPath;

    private static void Add(string line)
    {
        Log.Add(line);
        if (_logPath != null) File.AppendAllText(_logPath, line + Environment.NewLine);
    }

    private static void Check(string name, Func<string?> test)
    {
        try
        {
            string? problem = test();
            Add(problem == null ? "ok    " + name : $"FAIL  {name}: {problem}");
            if (problem != null) _failures++;
        }
        catch (Exception ex)
        {
            Add($"FAIL  {name}: {ex}");
            _failures++;
        }
    }

    public static int Run(string[] args)
    {
        string outDir = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "yangialifbo-selftest");
        Directory.CreateDirectory(outDir);
        _logPath = Path.Combine(outDir, "selftest.txt");
        File.WriteAllText(_logPath, "");
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Add("CRASH " + e.ExceptionObject);
        Application.ThreadException += (_, e) => Add("CRASH " + e.Exception);
        var total = Stopwatch.StartNew();

        LanguageDetector detector = null!;
        Check("engine loads", () =>
        {
            var sw = Stopwatch.StartNew();
            detector = new LanguageDetector();
            Add($"      load {sw.ElapsedMilliseconds} ms, {detector.UzbekLexiconCount:N0} forms, working set {Environment.WorkingSet / (1024 * 1024)} MB");
            return detector.UzbekLexiconCount > 900_000 ? null : "lexicon count " + detector.UzbekLexiconCount;
        });

        Check("known sentences convert", () =>
        {
            const string input = "O'zbekcha gaplashsak ham bo'laveradi. 5-chi sinf, yaxwi boldi! Is'hoq show va push haqida gapirdi.";
            const string expected = "Özbekça gaplaşsak ham bölaveradi. 5-çi sinf, yaxşi böldi! Is'hoq show va push haqida gapirdi.";
            string got = TextConverter.Convert(input, detector, TransliterationMode.Strict, true, out _);
            return got == expected ? null : got;
        });

        Check("neural fallback loads and scores", () =>
        {
            var n = detector.Neural;
            if (n == null) return "model not embedded";
            var sw = Stopwatch.StartNew();
            const int reps = 200;
            for (int i = 0; i < reps; i++) n.Score("sho'rvaxonalarimizdagilar");
            Add($"      {n.Models} models, threshold {n.Threshold:F6}, {sw.Elapsed.TotalMilliseconds / reps:F2} ms per word");
            if (!detector.IsUzbekOrLikely("sho'rvaxonalarimizdagilar")) return "unknown Uzbek word not accepted";
            foreach (string w in new[] { "fresh", "bishop's", "kuzushiji", "dashboard" })
                if (detector.IsUzbekOrLikely(w)) return "foreign word accepted: " + w;
            return null;
        });

        if (args.Length > 2)
        {
            Check("engine dump", () =>
            {
                using var o = new StreamWriter(Path.Combine(outDir, "engine.tsv"), false, new UTF8Encoding(false)) { NewLine = "\n" };
                foreach (var w in File.ReadLines(args[2], Encoding.UTF8))
                {
                    o.Write(w); o.Write('\t');
                    o.Write(detector.ShouldConvert(w, ConversionMode.Strict) ? '1' : '0');
                    o.Write(detector.ShouldConvert(w, ConversionMode.Aggressive) ? '1' : '0');
                    o.Write(detector.IsUzbek(w, ConversionMode.Strict) ? '1' : '0');
                    o.Write(detector.IsForeignOrReject(w) ? '1' : '0'); o.Write('\t');
                    o.Write(ChatNormalizer.Normalize(w)); o.Write('\t');
                    o.Write(UzbekPhonetics.Convert(w)); o.Write('\t');
                    o.WriteLine(detector.IsUzbekOrLikely(w) ? '1' : '0');   // with the neural fallback (v1.8)
                }
                return null;
            });
        }

        Check("UI Automation focus tracker (COM)", () =>
        {
            using var t = new FocusTracker();
            t.Install();
            t.Uninstall();
            return null;
        });

        Check("keyboard hook install", () =>
        {
            using var hook = new KeyboardHook(detector, new FocusTracker());
            hook.Install();
            hook.Uninstall();
            return null;
        });

        Check("updater reads latest release", () =>
        {
            using var u = new Updater();
            var r = u.GetLatestAsync().GetAwaiter().GetResult();
            return r != null && r.Sha256 != null ? null : "no release or no checksum";
        });

        Check("update notes round-trip", () =>
        {
            string dir = Path.Combine(outDir, "swap");
            Directory.CreateDirectory(dir);
            string exe = Path.Combine(dir, "YangiAlifbo.exe"), dl = Path.Combine(dir, "new.exe");
            File.WriteAllText(exe, "old"); File.WriteAllText(dl, "new");
            var rel = new ReleaseInfo(new Version(9, 9, 9), "v9.9.9", "test", "## Notes\n- 5-çi", "https://x", "https://x", 3, null);
            if (!Updater.TryInstall(dl, rel, exe, start: false, out string err)) return err;
            var w = Updater.TakeWhatsNew();
            Directory.Delete(dir, true);
            return w?.Version == "9.9.9" && w.Notes.Contains("5-çi") ? null : "notes lost";
        });

        Check("fallback dashboard renders", () =>
        {
            using var hook = new KeyboardHook(detector, new FocusTracker());
            using var f = new DashboardForm(hook, detector);
            f.CreateControl();
            using var bmp = new Bitmap(f.Width, f.Height);
            f.DrawToBitmap(bmp, new Rectangle(0, 0, f.Width, f.Height));
            bmp.Save(Path.Combine(outDir, "dashboard.png"));
            return null;
        });

        Check("tray app starts and stops", () =>
        {
            // Same construction as a real launch (menu, tray icon, hooks, updater timer).
            using var tray = new TrayApplicationContext();
            return null;
        });

        RunControlCenter(detector, outDir);

        Add($"{(_failures == 0 ? "PASS" : "FAILED")}: {_failures} failure(s), {total.Elapsed.TotalSeconds:F1} s");
        return _failures;
    }

    private static void RunControlCenter(LanguageDetector detector, string outDir)
    {
        if (!ControlCenterForm.IsRuntimeAvailable())
        {
            Add("skip  control center: WebView2 runtime not installed");
            return;
        }

        var host = new FakeHost();
        using var hook = new KeyboardHook(detector, new FocusTracker());
        var form = new ControlCenterForm(host, hook, detector, null)
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-4000, 0),
            ShowInTaskbar = false
        };
        host.Changed = form.NotifyStateChanged;

        form.Shown += async (_, _) =>
        {
            async Task<string> Js(string code) => (await form.RunScriptAsync(code)).Trim('"');
            async Task Until(Func<Task<bool>> cond, string what)
            {
                for (int i = 0; i < 100; i++) { if (await cond()) return; await Task.Delay(100); }
                throw new TimeoutException(what);
            }

            try
            {
                await Until(() => Task.FromResult(form.IsWebReady), "page ready");
                await Until(async () => (await Js("document.querySelector('[data-output]').dataset.text || ''")).Length > 0, "playground output");

                string output = await Js("document.querySelector('[data-output]').dataset.text");
                Check("control center: live conversion", () => output.Contains("Özbekça") && output.Contains("5-çi") && output.Contains("show") ? null : output);
                string lex = await Js("document.querySelector('[data-bind=lexicon]').textContent");
                Check("control center: state message", () => lex.Replace(" ", "").Replace(" ", "") == detector.UzbekLexiconCount.ToString() ? null : lex);

                await Js("document.querySelector('.switch--big').click()");
                await Until(() => Task.FromResult(!host.IsEnabled), "master switch");
                await Js("document.querySelector('[data-mode=Aggressive]').click()");
                await Until(() => Task.FromResult(host.Mode == TransliterationMode.Aggressive), "mode switch");
                Check("control center: switches reach the app", () => null);

                form.ShowPage("yangilanish");
                await Task.Delay(500);
                string page = await Js("document.querySelector('.page.is-active').id");
                Check("control center: page navigation", () => page == "yangilanish" ? null : page);

                using (var fs = File.Create(Path.Combine(outDir, "control-center.png")))
                    await form.CaptureAsync(fs);
            }
            catch (Exception ex)
            {
                Check("control center", () => ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                Application.ExitThread();
            }
        };

        form.Show();
        Application.Run();
        form.Dispose();
    }
}
