namespace Player.Net._2.UserInterface
{
    using DJPad.Core;
    using DJPad.Types;
    using Resources;
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    using DJPad.Core.Interfaces;
    using DJPad.Core.Utils;
    using DJPad.Core.Vis;
    using DJPad.Player;
    using DJPad.UI;
    using DJPad.UI.GdiPlus;

    public class SmallPlayerUi : BaseUi
    {
        private ColorPalette Palette;
        private ChildWindow playList;

        private readonly IVisualisation[] allBackgroundVisualisations =
        {
            new Oscilloscope(),
            new FftBars(),
            new Sonagram(),
            new BassZoom(),
            new CircularOscilloscope(),

            // No visualisation.
            null,
        };
        private int backgroundVisIndex;
        
        private bool UnderlayVis
        {
            get
            {
                return this.CurrentVisualisation != null;
            }
        }

        private IVisualisation CurrentVisualisation
        {
            get { return this.allBackgroundVisualisations[backgroundVisIndex % this.allBackgroundVisualisations.Length]; }
        }

        public override IList<LightControl<Bitmap>> GenerateUI(PlayerState state, WindowState window)
        {
            this.backgroundVisIndex = state.Configuration.Visualisation;
            this.Size = new Size(400, 400);
            this.Player = state;
            this.Window = window;
            this.Palette = this.Palette ?? Resources.Unknown.GetPalette();
            this.Name = Resources.Player_Name;
           
            InitVisualisations(window.Repaint);
            
            this.Player.PlayStateChanged += (i, s) => window.Form.InvokeIfRequired(() =>
            {
                this.Palette = Player.Playlist.Empty || Player.Playlist.Current.Metadata.AlbumArt == null
                    ? Resources.Unknown.GetPalette()
                    : this.Player.Playlist.Current.SynchronousArt.GetPalette();

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
                                      new ImageCachingLightControl
                                      {
                                          Name = "AlbumArt",
                                          AlwaysVisible = true,
                                          Extents = new Rectangle(new Point(), this.Size),
                                          SourceImage = c => Player.Playlist.Empty || Player.Playlist.Current.SynchronousArt == null
                                                  ? Resources.Unknown
                                                  : Player.Playlist.Current.SynchronousArt,
                                          CacheKey = () => !Player.Playlist.Empty ? Player.Playlist.Current + "_" + Player.Playlist.Current.HasLoadedMetadata : string.Empty
                                      },
                                      new LightPanel
                                      {
                                          Name = "Visualisation",
                                          AlwaysVisible = true,
                                          Extents = new Rectangle(new Point(), this.Size),
                                          Image = () => Player.CanPlay && this.UnderlayVis
                                              ? this.CurrentVisualisation.Draw(this.Size, Color.Transparent, Player.IsPlaying, Player.Playlist.Current.Source.Duration, this.Palette) 
                                              : null
                                      },
                                      new LightTextPanel()
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
                                          Justify = LightTextPanel.Justification.Center,
                                          Color = () => this.UnderlayVis ? Palette.Brightest : Color.Gainsboro,
                                          BorderColor = () => this.UnderlayVis ? Palette.Darkest : Color.SlateGray,
                                          Text = () => Player.Playlist.Empty
                                                      ? string.Empty
                                                      : Player.Playlist.Current.Source.Position.Consise()
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
                                          SourceImage = c => BitmapExtensions.CreateImage(new Size(Size.Width, Size.Height), Color.FromArgb(150, 50, 50, 50))
                                      },
                                      // Not ready yet...
                                      //new LightButton
                                      //{
                                      //    Name = "Settings",
                                      //    Extents = new Rectangle
                                      //              {
                                      //                  X = Size.Width/2 - Size.Width/4,
                                      //                  Y = Size.Height/2 + Size.Height/8,
                                      //                  Width = Size.Width/16,
                                      //                  Height = Size.Height/16,
                                      //              },
                                      //    OnClick = p =>
                                      //              {
                                      //                  this.configuration = this.configuration ?? new ChildWindow(new ConfigurationUi(), RelativePosition.Left, state, window);

                                      //                  if (this.configuration.Visible)
                                      //                  {
                                      //                      this.configuration.Hide();
                                      //                  }
                                      //                  else
                                      //                  {
                                      //                      this.configuration.Location = new Point(window.Location().X - Size.Width, window.Location().Y);
                                      //                      this.configuration.Show(this.Window.Form);
                                      //                  }
                                      //              },
                                      //    Image = () => Resources.Settings
                                      //},
                                      new LightButton
                                      {
                                          Name = "Open",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width/2 - Size.Width/32,
                                                        Y = Size.Height/2 - Size.Width/6,
                                                        Width = Size.Width/16,
                                                        Height = Size.Height/16,
                                                    },
                                          OnClick = p => 
                                                    {
                                                        Player.Open();
                                                    },
                                          Image = () => System.Windows.Forms.Control.ModifierKeys == System.Windows.Forms.Keys.Control 
                                                              ? Resources.Player_Folder_Open 
                                                              : Resources.Player_Folder 
                                      },
                                      new LightButton
                                      {
                                          Name = "Playlist",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width/2 - Size.Width/8,
                                                        Y = Size.Height/2 - Size.Width/12,
                                                        Width = Size.Width/20,
                                                        Height = Size.Height/20,
                                                    },
                                          OnClick = p => 
                                                    {
                                                        this.playList = this.playList ?? new ChildWindow(new SmallPlaylistUi(), RelativePosition.Right, state, window);

                                                        if (this.playList.Visible)
                                                        {
                                                            this.playList.Hide();
                                                            state.Configuration.PlaylistVisible = false;
                                                        }
                                                        else
                                                        {
                                                            this.playList.Location = new Point(window.Location().X + Size.Width, window.Location().Y);
                                                            this.playList.Show(this.Window.Form);
                                                            state.Configuration.PlaylistVisible = true;
                                                        }
                                                    },
                                          Image = () => this.playList != null && this.playList.Visible 
                                                                      ? Resources.Player_List
                                                                      : Resources.Player_ListDisabled
                                      },                
                                      new LightButton
                                      {
                                          Name = "Visualisation",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width/2 - Size.Width/8,
                                                        Y = Size.Height/2 + Size.Width/32,
                                                        Width = Size.Width/20,
                                                        Height = Size.Height/20,
                                                    },
                                          OnClick = p =>
                                          {
                                                        state.Configuration.Visualisation = ++this.backgroundVisIndex;
                                                        state.Configuration.Save();
                                                    },
                                          Image = () => Resources.VisualisationSettings
                                      },                     
                                      new LightButton
                                      {
                                          Name = "Random",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width/2 + Size.Width/12,
                                                        Y = Size.Height/2 + Size.Width/32,
                                                        Width = Size.Width/20,
                                                        Height = Size.Height/20,
                                                    },
                                          OnClick = p => {
                                                            this.Player.Playlist.Random = !this.Player.Playlist.Random;
                                                            this.Player.Configuration.Randomise =this.Player.Playlist.Random;
                                                         },
                                          Image = () => this.Player.Playlist.Random ? Resources.Random : Resources.RandomDisabled
                                      },
                                      new LightButton
                                      {
                                          Name = "Repeat",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width/2 + Size.Width/12,
                                                        Y = Size.Height/2 - Size.Width/12,
                                                        Width = Size.Width/20,
                                                        Height = Size.Height/20,
                                                    },
                                          OnClick = p => { this.Player.Playlist.Repeat = !this.Player.Playlist.Repeat; },
                                          Image = () => this.Player.Playlist.Repeat ? Resources.Repeat : Resources.RepeatDisabled
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
                                                            InitVisualisations(window.Repaint);
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
                                                          ? Resources.Player_Pause
                                                          : Resources.Player_Play;
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
                                          Image = () => Player.Playlist.End ? null : Resources.Player_Next
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
                                          Image = () => Player.Playlist.Start ? null : Resources.Player_Previous
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

                                                        return image;
                                                  }
                                      },
                                      new LightPlaylistItem()
                                      {
                                          Name="CurrentItem",
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
                                          Name = "Menu",
                                          Background = () => Color.FromArgb(20, 50, 50, 50),
                                          Extents = new Rectangle
                                                    { 
                                                        X = 0,
                                                        Y = 0,
                                                        Height = 21,
                                                        Width = Size.Width,
                                                    },
                                          Children = new List<LightButton> {
                                                  new LightButton
                                                  {
                                                      Name = "Exit",
                                                      Extents = new Rectangle { Y = 2, X = Size.Width - 18, Height = 16, Width = 16 },
                                                      OnClick = p => Window.Close(),
                                                      Image = () => Resources.Cancel
                                                  },
                                              }
                                    }
                   };
        }

        private void InitVisualisations(Action repaint)
        {
            foreach (var vis in this.allBackgroundVisualisations)
            {
                if (vis == null) continue;
                vis.SampleSource = Player.Audio;
            }
        }
    }
}