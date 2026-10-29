using System.Runtime.InteropServices;

namespace HdrSwitcher.App;

internal static class NativeMethods
{
    internal const int WmHotkey = 0x0312;
    internal const int HotkeyId = 0x4844;

    internal const uint ModAlt = 0x0001;
    internal const uint ModControl = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(IntPtr hIcon);
}
