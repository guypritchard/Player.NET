namespace DJPad.Formats.Wave
{
    using System;
    using System.Runtime.InteropServices;

    internal class WindowsNative
    {
        #region Public Methods and Operators

        [DllImport("user32.dll", SetLastError = false)]
        public static extern IntPtr GetDesktopWindow();

        #endregion
    }
}