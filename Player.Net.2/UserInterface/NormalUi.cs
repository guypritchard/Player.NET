namespace Player.Net._2.UserInterface
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using DJPad.Core.Interfaces;
    using DJPad.Core.Utils;
    using DJPad.Core.Vis;
    using DJPad.Player;
    using DJPad.UI;
    using Resources;
    using DJPad.UI.GdiPlus;

    public class NormalUi : BaseUi
    {
        public IVisualisation CreateVisualisation(IAudioOutput audio)
        {
            return new FFTGraph() { SampleSource = audio };
        }

        public override IList<LightControl<Bitmap>> GenerateUI(PlayerState state, WindowState window)
        {
            this.Player = state;
            this.Window = window;
            this.Size = new Size(600, 400);
            var background = Color.Black;

            state.PlaylistChanged += () => window.Repaint();

            return new List<LightControl<Bitmap>>
                                  {
                                     
                                      new LightButton
                                      {
                                          Name = "Previous",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width/2 - Size.Width/10 - Size.Height/28,
                                                        Y = Size.Height - Size.Height/8 - Size.Height/60,
                                                        Height = Size.Height/8,
                                                        Width = Size.Width/14
                                                    },
                                          OnClick = p => Player.Previous(),
                                          Image = () => Player.Playlist.Start ? null : Resources.Player_Previous
                                      },
                                      new LightPlaylist
                                        {
                                            Name = "Playlist",
                                            AlwaysVisible = true,
                                            Background = () => background,
                                            Extents = new Rectangle(new Point(), this.Size),
                                            Redraw = window.Repaint,
                                            List = () => state.Playlist,
                                            //OnItemClicked = i =>
                                            //{
                                            //    if (i != state.Playlist.Current)
                                            //    {
                                            //        state.Playlist.MoveToItem(i.FullFileName);
                                            //        state.Play(i, state.IsPlaying);
                                            //    }
                                            //},
                                            OnItemDoubleClicked = i =>
                                            {
                                                state.Playlist.MoveToItem(i.FullFileName);
                                                state.Play(i, true);
                                            }
                                        },
                                         new LightButton
                                      {
                                          Name = "ChangeUi",
                                          OnDoubleClick = p => { window.ChangeUi(); }
                                      },
                                      new LightButton
                                      {
                                          Name = "Next",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width/2 + Size.Width/20,
                                                        Y = Size.Height - Size.Height/8 - Size.Height/60,
                                                        Height = Size.Height/8,
                                                        Width = Size.Width/14
                                                    },
                                          OnClick = p => Player.Next(),
                                          Image = () => Player.Playlist.End ? null : Resources.Player_Next
                                      },
                                      new LightTextPanel
                                      {
                                          AlwaysVisible = true,
                                          Name = "Duration",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width - 100,
                                                        Y = Size.Height - 100,
                                                        Height = 200,
                                                        Width = 210
                                                    },
                                          Font = new Font("Segoe UI Light", 14, FontStyle.Regular),
                                          Color = () => Color.White,
                                          Justify = LightTextPanel.Justification.Center,
                                          Text = () => Player.Playlist.Empty
                                                      ? string.Empty
                                                      : Player.Playlist.Current.Source.Duration.Consise()
                                      },
                                      new LightTextPanel
                                      {
                                          AlwaysVisible = true,
                                          Name = "Played",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width - 350,
                                                        Y = 100,
                                                        Height = 200,
                                                        Width = 350
                                                    },
                                          Font = new Font("Segoe UI Light", 75, FontStyle.Bold),
                                          Color = () => Color.White,
                                          Justify = LightTextPanel.Justification.Right,
                                          Text = () => Player.Playlist.Empty
                                                      ? string.Empty
                                                      :  Player.Playlist.Current.Source.Position.Consise()
                                      },
                                      new LightButton
                                      {
                                          Name = "Play/Pause",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width/2 - Size.Width/20,
                                                        Y = Size.Height - Size.Height/6,
                                                        Width = Size.Width/10,
                                                        Height = Size.Height/6,
                                                    },
                                          OnClick = p =>
                                                    {
                                                        if (Player.CanPlay)
                                                        {
                                                            Window.StartRendering();
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
                                          Name = "Exit",
                                          Extents = new Rectangle
                                                    {
                                                        X = Size.Width - 30,
                                                        Height = 30,
                                                        Width = 30
                                                    },
                                          OnClick = p => this.Window.Close(),
                                          Image = () => Resources.Cancel
                                      },
                                      new LightScroll
                                      {
                                          Name = "Progress",
                                          Extents = new Rectangle
                                                    {
                                                        X = 0,
                                                        Y = Size.Height - Size.Height/4,
                                                        Height = 30,
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

                                                        var image = new Bitmap(Size.Width, 30, PixelFormat.Format32bppPArgb);
                                                        using (var g = Graphics.FromImage(image))
                                                        {
                                                            g.DrawLine(new Pen(Color.White, 1.0f), 0, image.Height - 1, image.Width, image.Height - 1);
                                                            g.DrawLine(new Pen(Color.White, 5f), new PointF(percentagePlayed * image.Width, 0.0f), new PointF(percentagePlayed * image.Width, image.Height));
                                                        }

                                                        return image;
                                                  }
                                      }
                   };
        }
    }
}