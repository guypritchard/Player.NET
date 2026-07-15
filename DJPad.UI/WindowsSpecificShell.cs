namespace DJPad.UI
{
    using System.Drawing;
    using DJPad.Core;
    using DJPad.Core.Utils;
    using System;

    using Resources;
    using System.Runtime.InteropServices;

    public class WindowsSpecificShell
    {
        private static readonly ITaskbarList4 Taskbar = CreateTaskbar();

        public void SetOverlayIcon(IntPtr windowHandle, IPlaylistItem item, bool playing)
        {
            if (item != null && Taskbar != null && windowHandle != IntPtr.Zero)
            {
                using var coverArt = (item.SynchronousArt ?? Resources.Unknown).Overlay(
                    playing ? Resources.Player_Play_Small : Resources.Player_Pause_Small,
                    new Rectangle(new Point(), (item.SynchronousArt ?? Resources.Unknown).Size));
                var iconHandle = coverArt.GetHicon();
                try
                {
                    Taskbar.SetOverlayIcon(windowHandle, iconHandle, item.Metadata.Title);
                }
                finally
                {
                    DestroyIcon(iconHandle);
                }
            }
        }

        public void SetPlaybackProgress(IntPtr windowHandle, int value, bool playing)
        {
            if (Taskbar == null || windowHandle == IntPtr.Zero)
            {
                return;
            }

            var state = playing ? TaskbarProgressState.Normal : TaskbarProgressState.Paused;
            Taskbar.SetProgressState(windowHandle, state);
            Taskbar.SetProgressValue(windowHandle, (ulong)Math.Clamp(value, 0, 1000), 1000);
        }

        private static ITaskbarList4 CreateTaskbar()
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            {
                return null;
            }

            try
            {
                var taskbar = (ITaskbarList4)new TaskbarList();
                return taskbar.HrInit() >= 0 ? taskbar : null;
            }
            catch (COMException)
            {
                return null;
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr handle);

        private enum TaskbarProgressState : uint
        {
            Normal = 2,
            Paused = 8
        }

        [ComImport]
        [Guid("56FDF344-FD6D-11D0-958A-006097C9A090")]
        private class TaskbarList
        {
        }

        [ComImport]
        [Guid("C43DC798-95D1-4BEA-9030-BB99E2983A1A")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList4
        {
            [PreserveSig] int HrInit();
            [PreserveSig] int AddTab(IntPtr windowHandle);
            [PreserveSig] int DeleteTab(IntPtr windowHandle);
            [PreserveSig] int ActivateTab(IntPtr windowHandle);
            [PreserveSig] int SetActiveAlt(IntPtr windowHandle);
            [PreserveSig] int MarkFullscreenWindow(IntPtr windowHandle, [MarshalAs(UnmanagedType.Bool)] bool fullscreen);
            [PreserveSig] int SetProgressValue(IntPtr windowHandle, ulong completed, ulong total);
            [PreserveSig] int SetProgressState(IntPtr windowHandle, TaskbarProgressState state);
            [PreserveSig] int RegisterTab(IntPtr tabHandle, IntPtr parentHandle);
            [PreserveSig] int UnregisterTab(IntPtr tabHandle);
            [PreserveSig] int SetTabOrder(IntPtr tabHandle, IntPtr insertBeforeHandle);
            [PreserveSig] int SetTabActive(IntPtr tabHandle, IntPtr parentHandle, uint reserved);
            [PreserveSig] int ThumbBarAddButtons(IntPtr windowHandle, uint buttonCount, IntPtr buttons);
            [PreserveSig] int ThumbBarUpdateButtons(IntPtr windowHandle, uint buttonCount, IntPtr buttons);
            [PreserveSig] int ThumbBarSetImageList(IntPtr windowHandle, IntPtr imageList);
            [PreserveSig] int SetOverlayIcon(IntPtr windowHandle, IntPtr iconHandle, [MarshalAs(UnmanagedType.LPWStr)] string description);
            [PreserveSig] int SetThumbnailTooltip(IntPtr windowHandle, [MarshalAs(UnmanagedType.LPWStr)] string tooltip);
            [PreserveSig] int SetThumbnailClip(IntPtr windowHandle, IntPtr clip);
        }
    }
}
