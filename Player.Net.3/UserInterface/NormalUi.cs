namespace Player.Net._2.UserInterface
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using DJPad.Core;
    using DJPad.Core.Utils;
    using DJPad.Player;
    using DJPad.Types;
    using DJPad.UI;
    using Player.Net._3;
    using System.Windows.Forms;
    using System.IO;
    using DJPad.UI.D2D;
    using CircularOscilloscope = Player.Net._2.UserInterface.Vis.CircularOscilloscope;

    public class NormalUi : BaseUi
    {
        private ColorPalette Palette;

        public NormalUi()
        {
            this.Size = new Size(400, 400);
        }
        
        private readonly CircularOscilloscope OverlayVisualisation = new CircularOscilloscope();

        public override IList<LightControl<SharpDX.Direct2D1.Bitmap>> GenerateUI(PlayerState state, WindowState window)
        {
            this.Player = state;
            var d2dWindow = window as D2DWindowState;

            this.Palette = this.Palette ?? Resources.Unknown.GetPalette();

            this.Player.PlayStateChanged += (i,s) => window.Form.InvokeIfRequired(() =>
            {
                this.Palette = Player.Playlist.Empty || Player.Playlist.Current.Metadata.AlbumArt == null
                    ? Resources.Unknown.GetPalette()
                    : this.Player.Playlist.Current.SynchronousArt.GetPalette();

                if (s == Status.Playing)
                {
                    Window.StartRendering();
                }
                else
                {
                    Window.StopRendering();
                }
            });

            state.PlaylistChanged += () => window.Repaint();
            this.OverlayVisualisation.SampleSource = Player.Audio;

            return new List<LightControl<SharpDX.Direct2D1.Bitmap>>
                                  {
                                      new ImageCachingLightControl
                                      {
                                          Name = "AlbumArt",
                                          AlwaysVisible = true,
                                          Extents = new Rectangle(new Point(), this.Size),
                                          SourceImage = c => Player.Playlist.Empty || Player.Playlist.Current.SynchronousArt == null
                                                  ? Resources.Unknown.ToD2dBitmap(d2dWindow.Target)
                                                  : Player.Playlist.Current.SynchronousArt.ToD2dBitmap(d2dWindow.Target),
                                          CacheKey = () => !Player.Playlist.Empty ? Player.Playlist.Current + "_" + Player.Playlist.Current.HasLoadedMetadata : string.Empty
                                      },
                                      new D2DLightPanel
                                      {
                                          Name = "OverlayVisualisation",
                                          AlwaysVisible = true,
                                          Extents = new Rectangle(new Point(), this.Size),
                                          Image = () =>
                                          {
                                              if (Player.CanPlay)
                                              {

                                                  this.OverlayVisualisation.Draw(d2dWindow.Target, Color.Transparent, 400, 400, Player.IsPlaying, Player.Playlist.Current.Source.Duration, this.Palette);
                                              }
                                              return null;
                                          }
                                      },
                                      new D2DLightTextPanel()
                                      {
                                          Name = "Position",
                                          AlwaysVisible = true,
                                          Extents = new Rectangle
                                                    {
                                                        X = 0,
                                                        Y = 0,
                                                        Width = Size.Width,
                                                        Height = Size.Height
                                                    },
                                          Font = new Font("Segoe UI Light", this.Size.Height/4, FontStyle.Bold),
                                          Justify = D2DLightTextPanel.Justification.Center,
                                          Color = () => Color.Gainsboro,
                                          BorderColor = () => Color.SlateGray,
                                          Text = () => Player.Playlist.Empty
                                                      ? string.Empty
                                                      : Player.Playlist.Current.Source.Position.Consise(),
                                          Target = d2dWindow.Target,
                                      },
                                      new ImageCachingLightControl
                                      {
                                          Name = "ButtonUnderlay",
                                          Extents = new Rectangle
                                                    {
                                                        X = 0,
                                                        Y = 0,
                                                        Width = Size.Width,
                                                        Height = Size.Height
                                                    },
                                          SourceImage = c => BitmapExtensions.CreateImage(new Size(Size.Width, Size.Height), Color.FromArgb(150, 50, 50, 50)).ToD2dBitmap(d2dWindow.Target)
                                      },
                                      new LightButton
                                      {
                                          Name = "Open",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width/2 - Size.Width/16,
                                                        Y = Size.Height/2 - Size.Height/5,
                                                        Width = Size.Width/8,
                                                        Height = Size.Height/8,
                                                    },
                                          OnClick = p => 
                                                    {
                                                        var open = new OpenFileDialog
                                                        {
                                                            Filter = Resources.PlayableFiles,
                                                            AutoUpgradeEnabled = true,
                                                            InitialDirectory = Player.Playlist.Empty
                                                                ? Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                                                                : Path.GetDirectoryName(Player.Playlist.Current.FullFileName),
                                                            RestoreDirectory = false
                                                        };

                                                        if (open.ShowDialog() == DialogResult.OK)
                                                        {
                                                            Player.NewPlaylist(open.FileName, true);
                                                        }
                                                    },
                                          Image = () => Resources.Folder_White.ToD2dBitmap(d2dWindow.Target),
                                      },
                                      //new LightButton
                                      //{
                                      //    Name = "Playlist",
                                      //    Extents = new Rectangle
                                      //              {
                                      //                  X = Size.Width/2 - Size.Width/8 - Size.Width/16,
                                      //                  Width = Size.Width/8,
                                      //                  Y = Size.Height/2 + Size.Height/12,
                                      //                  Height = Size.Height/8,
                                      //              },
                                      //    OnClick = p => 
                                      //              {
                                      //                  this.playList = this.playList ?? new PlayList(state, window);
                                      //                  this.playList.Location = new Point(window.Location().X + Size.Width, window.Location().Y);

                                      //                  if (this.playList.Visible)
                                      //                  {
                                      //                      this.playList.Hide();
                                      //                  }
                                      //                  else
                                      //                  {
                                      //                      this.playList.Show(this.Window.Form);
                                      //                  }
                                      //              },
                                      //    Image = () => this.playList != null && this.playList.Visible 
                                      //                  ? PlayerResouces.Player_List
                                      //                  : PlayerResouces.Player_ListDisabled
                                      //},                
                                      new LightButton
                                      {
                                          Name = "Settings",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width/2 - Size.Width/16,
                                                        Y = Size.Height/2 + Size.Height/12,
                                                        Width = Size.Width/8,
                                                        Height = Size.Height/8,
                                                    },
                                          OnClick = p =>
                                                    {
                                                        // this.backgroundVisIndex++;
                                                    },
                                          Image = () => Resources.Settings.ToD2dBitmap(d2dWindow.Target)
                                      },                     
                                      new LightButton
                                      {
                                          Name = "Random",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width/2 + Size.Width/16,
                                                        Y = Size.Height/2 + Size.Height/12,
                                                        Width = Size.Width/8,
                                                        Height = Size.Height/8,
                                                    },
                                          OnClick = p => {
                                                            this.Player.Playlist.Random = !this.Player.Playlist.Random;
                                                            this.Player.Configuration.Randomise =this.Player.Playlist.Random;
                                                         },
                                          Image = () => this.Player.Playlist.Random ? Resources.Random.ToD2dBitmap(d2dWindow.Target) : Resources.RandomDisabled.ToD2dBitmap(d2dWindow.Target)
                                      },                
                                      new LightButton
                                      {
                                          Name = "Play/Pause",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width/2 - Size.Width/6,
                                                        Y = Size.Height/2 - Size.Height/6,
                                                        Width = Size.Width/3,
                                                        Height = Size.Height/3,
                                                    },
                                          OnClick = p =>
                                                    {
                                                        if (Player.CanPlay)
                                                        {
                                                            // InitVisualisations(window.Repaint);
                                                        }

                                                        Player.TogglePlay();
                                                    },
                                          Image = () =>
                                                  {
                                                      if (!Player.CanPlay)
                                                      {
                                                          return null;
                                                      }

                                                      return Player.IsPlaying
                                                          ? Resources.Player_Pause.ToD2dBitmap(d2dWindow.Target)
                                                          : Resources.Player_Play.ToD2dBitmap(d2dWindow.Target);
                                                  }
                                      },
                                      new LightButton
                                      {
                                          Name = "Next",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width/2 + Size.Width/8,
                                                        Y = Size.Height/2 - Size.Height/8,
                                                        Width = Size.Width/4,
                                                        Height = Size.Height/4,
                                                    },
                                          OnClick = p => Player.Next(),
                                          Image = () => Player.Playlist.End ? null : Resources.Player_Next.ToD2dBitmap(d2dWindow.Target)
                                      },
                                      new LightButton
                                      {
                                          Name = "Previous",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width/8,
                                                        Y = Size.Height/2 - Size.Height/8,
                                                        Width = Size.Width/4,
                                                        Height = Size.Height/4,
                                                    },
                                          OnClick = p => Player.Previous(),
                                          Image = () => Player.Playlist.Start ? null : Resources.Player_Previous.ToD2dBitmap(d2dWindow.Target)
                                      },
                                      new LightScroll()
                                      {
                                          Name = "Progress",
                                          Extents = new Rectangle
                                                    {
                                                        X = 0,
                                                        Y = Size.Height - 70,
                                                        Height = 20,
                                                        Width = Size.Width
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

                                                        var percentagePlayed = (float) (Player.Playlist.Current.Source.Position.TotalSeconds / Player.Playlist.Current.Source.Duration.TotalSeconds);

                                                        if (float.IsNaN(percentagePlayed))
                                                        {
                                                            percentagePlayed = 0.0f;
                                                        }

                                                        var image = new Bitmap(Size.Width, 30, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                                                        using (var g = Graphics.FromImage(image))
                                                        {
                                                            g.DrawLine(new Pen(Color.White, 1.0f), 0, image.Height - 1, image.Width, image.Height - 1);
                                                            g.DrawLine(new Pen(Color.White, 5f), new PointF(percentagePlayed * image.Width, 0.0f), new PointF(percentagePlayed * image.Width, image.Height));
                                                        }

                                                        return image.ToD2dBitmap(d2dWindow.Target);
                                                  }
                                      },
                                      new LightPlaylistItem()
                                      {
                                          Window = d2dWindow,
                                          Name="CurrentItem",
                                          AlwaysVisible = true,
                                          Background = () => Color.FromArgb(20, 50, 50, 50),
                                          Item = () => Player.Playlist.Current,
                                          Playlist = () => Player.Playlist,
                                          Extents = new Rectangle()
                                                    {
                                                        X = 0,
                                                        Y = Size.Height - 50,
                                                        Height = 50, 
                                                        Width = Size.Width
                                                    }
                                      },
                                      new LightMenu
                                      {
                                          Window = d2dWindow,
                                          Name = "Menu",
                                          Background = () => Color.FromArgb(20, 50, 50, 50),
                                          Extents = new Rectangle
                                                    { 
                                                        X = 0,
                                                        Y = 0,
                                                        Height = 21,
                                                        Width = Size.Width,
                                                    },
                                          Children = new List<DJPad.UI.GdiPlus.LightButton> {
                                                  new DJPad.UI.GdiPlus.LightButton
                                                  {
                                                      Name = "Exit",
                                                      Extents = new Rectangle { Y = 2, X = Size.Width - 18, Height = 16, Width = 16 },
                                                      OnClick = p => d2dWindow.Close(),
                                                      Image = () => Resources.Cancel
                                                  },
                                        }
                                     }
                   };
        }
    }
}