namespace Player.Net._2.UserInterface
{
    using System.Drawing;
    using DJPad.Core.Utils;
    using DJPad.Player;
    using DJPad.Types;
    using DJPad.UI;
    using Resources;
    using System.Collections.Generic;

    public abstract class BaseUi : IUserInterface<Bitmap>
    {
        protected PlayerState Player;
        protected WindowState Window;

        public string Name { get; protected set; }

        public Size Size { get; protected set; }

        public bool TopMost { get; set; }

        public abstract IList<LightControl<Bitmap>> GenerateUI(PlayerState playerState, WindowState windowState);

        protected ColorPalette GetPalette()
        {
            return Player.Playlist.Empty || Player.Playlist.Current.Metadata.AlbumArt == null
                        ? Resources.Unknown.GetPalette()
                        : this.Player.Playlist.Current.Metadata.AlbumArt.GetPalette();
        }
    }
}
