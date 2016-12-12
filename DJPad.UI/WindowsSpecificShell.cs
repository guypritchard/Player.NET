namespace DJPad.UI
{
    using System.Drawing;
    using DJPad.Core;
    using DJPad.Core.Utils;
    using Microsoft.WindowsAPICodePack.Taskbar;
    using System;

    using Resources;
    using System.Runtime.InteropServices;

    public interface IShellIntegration
    {

    }

    public class WindowsSpecificShell : IShellIntegration
    {
        public void SetOverlayIcon(IntPtr windowHandle, IPlaylistItem item, bool playing)
        {
            if (item != null && TaskbarManager.IsPlatformSupported && windowHandle != IntPtr.Zero)
            {
                var coverArt = item.SynchronousArt ?? Resources.Unknown;
                coverArt = coverArt.Overlay(playing ? Resources.Player_Play_Small : Resources.Player_Pause_Small, new Rectangle(new Point(), coverArt.Size));
                TaskbarManager.Instance.SetOverlayIcon(coverArt.ToIcon(), item.Metadata.Title);
            }
        }

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn
          (
              int nLeftRect, // x-coordinate of upper-left corner
              int nTopRect, // y-coordinate of upper-left corner
              int nRightRect, // x-coordinate of lower-right corner
              int nBottomRect, // y-coordinate of lower-right corner
              int nWidthEllipse, // height of ellipse
              int nHeightEllipse // width of ellipse
           );

        //public void SetShellButtons(IntPtr windowHandle, IEnumerable<LightButton> buttons)
        //{
        //    if (TaskbarManager.IsPlatformSupported && windowHandle != IntPtr.Zero)
        //    {
        //        var shellbuttons = buttons.Select(b => new ThumbnailToolBarButton(b.Image().ToIcon(), b.Name));

        //        TaskbarManager.Instance.ThumbnailToolBars.AddButtons(windowHandle, shellbuttons.ToArray());
        //    }
        //}
    }
}
