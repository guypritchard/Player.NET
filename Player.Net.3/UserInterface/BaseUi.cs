namespace Player.Net._2.UserInterface
{
    using DJPad.Core.Utils;
    using DJPad.Player;
    using DJPad.Types;
    using DJPad.UI;
    using Player.Net._3;
    using System.Collections.Generic;
    using System.Drawing;

    public abstract class BaseUi : IUserInterface<SharpDX.Direct2D1.Bitmap>
    {
        protected PlayerState Player;
        protected WindowState Window;

        public string Name { get; protected set; }

        public Size Size { get; protected set; }

        public bool TopMost { get; protected set; }

        public abstract IList<LightControl<SharpDX.Direct2D1.Bitmap>> GenerateUI(PlayerState playerState, WindowState windowState);

        protected ColorPalette GetPalette()
        {
            return Player.Playlist.Empty || Player.Playlist.Current.Metadata.AlbumArt == null
                        ? Resources.Unknown.GetPalette()
                        : this.Player.Playlist.Current.Metadata.AlbumArt.GetPalette();
        }
    }
}
