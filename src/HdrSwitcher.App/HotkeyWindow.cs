using System.Runtime.InteropServices;

namespace HdrSwitcher.App;

/// <summary>
/// Janela invisível (message-only) que recebe o atalho global Ctrl+Alt+H.
/// </summary>
internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    public const string HotkeyDisplayName = "Ctrl+Alt+H";

    private bool _registered;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "HdrSwitcher.HotkeyWindow",
            Parent = (IntPtr)(-3), // HWND_MESSAGE
        });

        _registered = NativeMethods.RegisterHotKey(
            Handle,
            NativeMethods.HotkeyId,
            NativeMethods.ModControl | NativeMethods.ModAlt,
            (uint)Keys.H);

        if (!_registered)
            HotkeyError = Marshal.GetLastWin32Error();
    }

    public event EventHandler? HotkeyPressed;

    public bool Registered => _registered;

    public int? HotkeyError { get; }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmHotkey && m.WParam.ToInt32() == NativeMethods.HotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_registered)
        {
            NativeMethods.UnregisterHotKey(Handle, NativeMethods.HotkeyId);
            _registered = false;
        }

        DestroyHandle();
    }
}
