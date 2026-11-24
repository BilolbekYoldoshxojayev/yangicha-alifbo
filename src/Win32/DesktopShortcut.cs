// ──────────────────────────────────────────────────────────────────────
// YangiAlifbo — DesktopShortcut.cs
// Creates or removes "YangiAlifbo.lnk" on the user's desktop (IShellLinkW).
// ──────────────────────────────────────────────────────────────────────

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace YangiAlifbo.Win32;

public static class DesktopShortcut
{
    public static string PathFor(string desktopDir) => Path.Combine(desktopDir, "YangiAlifbo.lnk");

    private static string DefaultPath =>
        PathFor(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));

    public static bool Exists() => File.Exists(DefaultPath);

    /// <summary>Writes a shortcut to <paramref name="target"/> (icon taken from the exe).</summary>
    // The trimmer drops interface methods this code never calls, which shifts the COM
    // vtable (SetPath then lands on another slot); keep the whole interface.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IShellLinkW))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IPersistFile))]
    public static void Create(string target, string? lnkPath = null)
    {
        lnkPath ??= DefaultPath;
        var link = (IShellLinkW)new ShellLink();
        try
        {
            link.SetPath(target);
            link.SetWorkingDirectory(Path.GetDirectoryName(target)!);
            link.SetIconLocation(target, 0);
            link.SetDescription("YangiAlifbo — o‘zbek yozuvini yangi alifboga o‘giradi");
            ((IPersistFile)link).Save(lnkPath, true);
        }
        finally
        {
            Marshal.FinalReleaseComObject(link);
        }
    }

    public static void Remove(string? lnkPath = null)
    {
        lnkPath ??= DefaultPath;
        if (File.Exists(lnkPath)) File.Delete(lnkPath);
    }

    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}
