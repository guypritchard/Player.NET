namespace DJPad.UI
{
    using System.Drawing;
    using DJPad.Core;
    using DJPad.Core.Utils;
    using Microsoft.WindowsAPICodePack.Taskbar;
    using System;

    using Resources;
    using System.Runtime.InteropServices;

    public class WindowsSpecificShell
    {
        private static readonly System.Reflection.MethodInfo SetOverlayIconForHandle = typeof(TaskbarManager).GetMethod(
            "SetOverlayIcon", new[] { typeof(IntPtr), typeof(Icon), typeof(string) });
        private static readonly System.Reflection.MethodInfo SetProgressStateForHandle = typeof(TaskbarManager).GetMethod(
            "SetProgressState", new[] { typeof(TaskbarProgressBarState), typeof(IntPtr) });
        private static readonly System.Reflection.MethodInfo SetProgressValueForHandle = typeof(TaskbarManager).GetMethod(
            "SetProgressValue", new[] { typeof(int), typeof(int), typeof(IntPtr) });

        public void SetOverlayIcon(IntPtr windowHandle, IPlaylistItem item, bool playing)
        {
            if (item != null && TaskbarManager.IsPlatformSupported && windowHandle != IntPtr.Zero)
            {
                using var coverArt = (item.SynchronousArt ?? Resources.Unknown).Overlay(
                    playing ? Resources.Player_Play_Small : Resources.Player_Pause_Small,
                    new Rectangle(new Point(), (item.SynchronousArt ?? Resources.Unknown).Size));
                var iconHandle = coverArt.GetHicon();
                try
                {
                    using var borrowedIcon = Icon.FromHandle(iconHandle);
                    using var icon = (Icon)borrowedIcon.Clone();
                    SetOverlayIconForHandle.Invoke(TaskbarManager.Instance, new object[] { windowHandle, icon, item.Metadata.Title });
                }
                finally
                {
                    DestroyIcon(iconHandle);
                }
            }
        }

        public void SetPlaybackProgress(IntPtr windowHandle, int value, bool playing)
        {
            if (!TaskbarManager.IsPlatformSupported || windowHandle == IntPtr.Zero)
            {
                return;
            }

            var state = playing ? TaskbarProgressBarState.Normal : TaskbarProgressBarState.Paused;
            SetProgressStateForHandle.Invoke(TaskbarManager.Instance, new object[] { state, windowHandle });
            SetProgressValueForHandle.Invoke(TaskbarManager.Instance, new object[] { Math.Clamp(value, 0, 1000), 1000, windowHandle });
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr handle);

    }
}
