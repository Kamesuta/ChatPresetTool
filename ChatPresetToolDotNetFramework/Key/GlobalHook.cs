using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ChatPresetTool
{
    public class GlobalHook
    {
        public delegate void KeyEvent();

        public event KeyEvent KeyEvents;

        public Window Window { get; }
        public uint HookKey { get; }
        public int HookId { get; }

        public uint HookModifier { get; set; }

        private const int WM_HOTKEY = 0x0312;
        private IntPtr _windowHandle;
        private HwndSource _source;

        public GlobalHook(Window window, uint hookKey, int hookId)
        {
            Window = window;
            HookKey = hookKey;
            HookId = hookId;
        }

        public void EnableHook()
        {
            _windowHandle = new WindowInteropHelper(Window).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HWndHook);

            int result = NativeMethods.RegisterHotKey(_windowHandle, HookId, HookModifier, HookKey);
        }

        private IntPtr HWndHook(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_HOTKEY:
                    if (wParam.ToInt32() == HookId)
                    {
                        int vkCode = (((int)lParam >> 16) & 0xFFFF);
                        if (vkCode == HookKey)
                        {
                            KeyEvents?.Invoke();
                        }
                        handled = true;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        public void DisableHook()
        {
            _source.RemoveHook(HWndHook);
            NativeMethods.UnregisterHotKey(_windowHandle, HookId);
            _source = null;
            _windowHandle = IntPtr.Zero;
        }
    }
}