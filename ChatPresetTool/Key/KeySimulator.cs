namespace ChatPresetTool
{
    public static class KeySimulator
    {
        public static void PressKey(byte key)
        {
            NativeMethods.keybd_event(key, (byte)NativeMethods.MapVirtualKey(key, 0), 0L, 0L);
        }

        public static void ReleaseKey(byte key)
        {
            NativeMethods.keybd_event(key, (byte)NativeMethods.MapVirtualKey(key, 0), 2L, 0L);
        }
    }
}