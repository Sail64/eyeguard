using System.Runtime.InteropServices;
using System.Windows.Input;

namespace EyeGuard;

public class GlobalKeyboardHook : IDisposable
{
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc _proc;
    private bool _isActive;

    // Allowed keys during lock screen (for password input)
    private static readonly HashSet<Key> AllowedKeys = new()
    {
        Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J,
        Key.K, Key.L, Key.M, Key.N, Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T,
        Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
        Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9,
        Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4,
        Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9,
        Key.Space, Key.Back, Key.Enter, Key.Tab,
        Key.OemMinus, Key.OemPlus, Key.OemComma, Key.OemPeriod, Key.OemQuestion,
        Key.OemTilde, Key.OemOpenBrackets, Key.OemCloseBrackets, Key.OemPipe,
        Key.OemSemicolon, Key.OemQuotes,
        Key.Left, Key.Right, Key.Up, Key.Down,
        Key.Home, Key.End, Key.Delete, Key.Insert,
        Key.LeftShift, Key.RightShift, Key.LeftCtrl, Key.RightCtrl,
        Key.LeftAlt, Key.RightAlt, Key.CapsLock,
        Key.NumLock, Key.Scroll
    };

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
        }
    }

    public GlobalKeyboardHook()
    {
        _proc = HookCallback;
        _hookId = SetHook(_proc);
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
            GetModuleHandle(curModule?.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (_isActive && nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            var key = KeyInterop.KeyFromVirtualKey(vkCode);

            bool isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
            bool isKeyUp = wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP;

            if (isKeyDown || isKeyUp)
            {
                // Always block these system keys
                if (key == Key.LWin || key == Key.RWin || key == Key.Escape)
                {
                    return (IntPtr)1;
                }

                // Block Alt+Tab, Alt+Esc, Ctrl+Esc combinations
                if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                {
                    if (key == Key.Tab || key == Key.Escape || key == Key.Space || key == Key.F4)
                    {
                        return (IntPtr)1;
                    }
                }

                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (key == Key.Escape || key == Key.LWin || key == Key.RWin)
                    {
                        return (IntPtr)1;
                    }
                }

                // Block function keys
                if (key >= Key.F1 && key <= Key.F24)
                {
                    return (IntPtr)1;
                }

                // Block PrintScreen
                if (key == Key.Snapshot)
                {
                    return (IntPtr)1;
                }

                // Allow password input keys
                if (!AllowedKeys.Contains(key))
                {
                    return (IntPtr)1;
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        UnhookWindowsHookEx(_hookId);
        GC.SuppressFinalize(this);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
