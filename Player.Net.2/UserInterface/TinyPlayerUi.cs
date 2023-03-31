namespace Player.Net._2.UserInterface
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Core.Utils;
    using DJPad.Core.Vis;
    using DJPad.Player;
    using DJPad.Types;
    using DJPad.UI;
    using Resources;
    using DJPad.UI.GdiPlus;

    public class TinyPlayerUi : BaseUi
    {
        private readonly IVisualisation Visualisation = new Oscilloscope();
        private ColorPalette Palette;

        public override IList<LightControl<Bitmap>> GenerateUI(PlayerState state, WindowState window)
        {
            this.Size = new Size(400, 50);
            this.Player = state;
            this.Window = window;
            this.TopMost = true;
            this.Palette = this.Palette ?? this.GetPalette();
            this.Name = Resources.Player_Name;

            this.Visualisation.SampleSource = Player.Audio;

           //  window.Form.Region = System.Drawing.Region.FromHrgn(WindowsSpecificShell.CreateRoundRectRgn(0, 0, this.Size.Width, this.Size.Height, 5, 5));

            this.Player.PlayStateChanged += (i, s) => window.Form.InvokeIfRequired(() =>
            {
                this.Palette = Player.Playlist.Empty || Player.Playlist.Current.Metadata.AlbumArt == null
                    ? Resources.Unknown.GetPalette()
                    : this.Player.Playlist.Current.Metadata.AlbumArt.GetPalette();

                if (s == Status.Playing || s == Status.Buffering)
                {
                    Window.StartRendering();
                }
                else
                {
                    Window.StopRendering();
                }
            });

            return new List<LightControl<Bitmap>>
                                  {
                                      new LightButton
                                      {
                                          Name = "ChangeUi",
                                          OnDoubleClick = p => window.ChangeUi()
                                      },
                                      new LightPanel
                                      {
                                          Name = "Visualisation",
                                          AlwaysVisible = true,
                                          Extents = new Rectangle(new Point(50, 0), this.Size),
                                          Image = () => !Player.Playlist.Empty
                                              ? this.Visualisation.Draw(new Size(this.Size.Width - 50, this.Size.Height), this.Palette.Darkest.Lighten(), Player.IsPlaying, Player.Playlist.Current.Source.Duration, this.Palette) 
                                              : null
                                      },
                                      new LightPlaylistItem()
                                      {
                                          Name="PlayListItem",
                                          AlwaysVisible = true,
                                          Background = () => Color.Transparent,
                                          Item = () => Player.Playlist.Current,
                                          Playlist = () => Player.Playlist,
                                          HideTime = true,
                                          Extents = new Rectangle()
                                                    {
                                                        X = 0,
                                                        Y = Size.Height - 50,
                                                        Height = 50, 
                                                        Width = Size.Width
                                                    }
                                      },
                                      new LightPanel
                                      {
                                          Name = "ButtonUnderlay",
                                          Extents = new Rectangle
                                                    {
                                                        X = 0,
                                                        Y = 0,
                                                        Width = Size.Width,
                                                        Height = Size.Height
                                                    },
                                          Image = () => BitmapExtensions.CreateImage(new Size(Size.Width, Size.Height), Color.FromArgb(150, 50, 50, 50))
                                      },
                                      new LightScroll()
                                      {
                                          Name = "Progress",
                                          Extents = new Rectangle
                                                    {
                                                        X = 50,
                                                        Y = Size.Height - 8,
                                                        Height = 8,
                                                        Width = Size.Width - 50
                                                    },
                                          OnScroll = p =>
                                                     {
                                                         var wasPlaying = Player.IsPlaying;
                                                         if (wasPlaying)
                                                         { 
                                                            Player.Stop();
                                                         }
                                                         
                                                         double position = Player.Playlist.Current.Source.Duration.TotalMilliseconds*p / 100;
                                                         Player.Playlist.Current.Source.Position = TimeSpan.FromMilliseconds(position);
                                                         
                                                         if (wasPlaying)
                                                         { 
                                                            Player.TogglePlay();
                                                         }
                                                     },
                                          Image = () =>
                                                  {
                                                        if (Player.Playlist.Empty)
                                                        {
                                                                return null;
                                                        }

                                                        double percentagePlayed = Player.Playlist.Current.Source.Position.TotalSeconds 
                                                                                  / Player.Playlist.Current.Source.Duration.TotalSeconds;

                                                        if (double.IsNaN(percentagePlayed))
                                                        {
                                                            percentagePlayed = 0.0d;
                                                        }

                                                        var image = new Bitmap(Size.Width, 10, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                                                        using (var g = Graphics.FromImage(image))
                                                        {
                                                            g.DrawLine(new Pen(Color.White, 1.0f), 0, image.Height - 1, image.Width, image.Height - 1);
                                                            g.DrawLine(new Pen(Color.White, 5f), new Point((int)(percentagePlayed * image.Width), 0), new Point((int)(percentagePlayed * image.Width), image.Height));
                                                        }

                                                        return image;
                                                  }
                                      },
                                      new LightTextPanel()
                                      {
                                          Name = "Position",
                                          AlwaysVisible = true,
                                          Extents = new Rectangle
                                                    {
                                                        X = 0,
                                                        Y = -14,
                                                        Width = Size.Width,
                                                        Height = Size.Height
                                                    },
                                          Font = new Font("Segoe UI Light", 16.0f, FontStyle.Bold),
                                          Justify = LightTextPanel.Justification.Right,
                                          Color = () => Palette.Brightest,
                                          BorderColor = () => Palette.Darkest,

                                          Text = () => Player.Playlist.Empty
                                                      ? string.Empty
                                                      : Player.Playlist.Current.Source.Position.Consise()
                                      },
                                      new LightButton
                                      {
                                          Name = "Next",
                                          Extents = new Rectangle
                                                    {
                                                        X = 50 + Size.Width / 2,
                                                        Y = 6,
                                                        Width = 40,
                                                        Height = 40,
                                                    },
                                          OnClick = p =>
                                                    {
                                                        Player.Next();
                                                    },
                                          Image = () => !Player.CanPlay || Player.Playlist.End ? null : Resources.Player_Next
                                      },
                                      new LightButton
                                      {
                                          Name = "Previous",
                                          Extents = new Rectangle
                                                    {
                                                        X = (Size.Width / 2) - 50,
                                                        Y = 6,
                                                        Width = 40,
                                                        Height = 40,
                                                    },
                                          OnClick = p =>
                                                    {
                                                        Player.Previous();
                                                    },
                                          Image = () => !Player.CanPlay || Player.Playlist.Start ? null : Resources.Player_Previous
                                      },
                                      new LightButton
                                      {
                                          Name = "Play/Pause",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width / 2 - 10,
                                                        Y = -4,
                                                        Width = 60,
                                                        Height = 60,
                                                    },
                                          OnClick = p =>
                                                    {
                                                        if (Player.CanPlay)
                                                        {
                                                            this.Visualisation.SampleSource = Player.Audio;
                                                        }

                                                        Player.TogglePlay();
                                                    },
                                          Image = () =>
                                                  {
                                                      if (!Player.CanPlay)
                                                      {
                                                          return null;
                                                      }

                                                      return Player.IsPlaying ? Resources.Player_Pause : Resources.Player_Play;
                                                  }
                                      },
                                      new LightButton
                                      {
                                          Name = "Open",
                                          Extents = new Rectangle
                                                    {
                                                        X = 16,
                                                        Y = 16,
                                                        Width = 20,
                                                        Height = 20,
                                                    },
                                          OnClick = p => 
                                                    {
                                                        Player.Open();
                                                    },
                                          Image = () => System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Control
                                                              ? Resources.Player_Folder_Open
                                                              : Resources.Player_Folder
                                      },
                                      new LightMenu
                                      {
                                          Name = "Menu",
                                          Background = () => Color.Transparent,
                                          Extents = new Rectangle
                                                    { 
                                                        X = 0,
                                                        Y = 0,
                                                        Height = 15,
                                                        Width = Size.Width,
                                                    },
                                          Children = new List<LightButton> {
                                                  new LightButton
                                                  {
                                                      Name = "Exit",
                                                      Extents = new Rectangle { Y = 2, X = Size.Width - 17, Height = 14, Width = 14 },
                                                      OnClick = p =>
                                                                {
                                                                    Window.Close();
                                                                },
                                                      Image = () => Resources.Cancel
                                                  },
                                              }
                                        }
                   };
        }

    }
}