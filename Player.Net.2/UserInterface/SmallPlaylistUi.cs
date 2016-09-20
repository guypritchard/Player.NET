namespace Player.Net._2.UserInterface
{
    using System.Collections.Generic;

    using DJPad.Player;
    using DJPad.UI;
    using System.Drawing;

    public class SmallPlaylistUi : IUserInterface<Bitmap>
    {
        public string Name { get; set; }

        public Size Size { get; set; }

        public IList<LightControl<Bitmap>> GenerateUI(PlayerState state, WindowState window)
        {
            this.Size = new Size(400, 400);
            var background = Color.Black;

            state.PlaylistChanged += () => window.Repaint();

            return new List<LightControl<Bitmap>>
                {
                    new DJPad.UI.GdiPlus.LightPlaylist
                    {
                        Name = "Playlist",
                        AlwaysVisible = true,
                        Background = () => background,
                        Extents = new Rectangle(new Point(), this.Size),
                        Redraw = window.Repaint,
                        List = () => state.Playlist,
                        IsPlaying = () => state.IsPlaying,
                        //OnItemClicked = i =>
                        //{
                        //    state.Playlist.MoveToItem(i.FullFileName);
                        //},
                        OnItemDoubleClicked = i =>
                        {
                            state.Playlist.MoveToItem(i.FullFileName);
                            state.Play(i, true);
                        }
                    }
                };
        }
    }
}
