namespace Player.Net._2.UserInterface
{
    using DJPad.Core.Utils;
    using DJPad.Player;
    using DJPad.Types;
    using DJPad.UI;
    using Player.Net._3;
    using System.Collections.Generic;
    using System.Drawing;
    using Vortice.Direct2D1;
    using WindowState = DJPad.UI.WindowState;

    public abstract class BaseUi : IUserInterface<ID2D1Bitmap>
    {
        protected PlayerState Player;
        protected WindowState Window;

        public string Name { get; protected set; }

        public Size Size { get; protected set; }

        public bool TopMost { get; protected set; }

        public abstract IList<LightControl<ID2D1Bitmap>> GenerateUI(PlayerState playerState, WindowState windowState);

        protected ColorPalette GetPalette()
        {
            return Player.Playlist.Empty || Player.Playlist.Current.Metadata.AlbumArt == null
                        ? Resources.Unknown.GetPalette()
                        : this.Player.Playlist.Current.Metadata.AlbumArt.GetPalette();
        }
    }
}
