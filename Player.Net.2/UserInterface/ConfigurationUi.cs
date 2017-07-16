namespace Player.Net._2.UserInterface
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using DJPad.Core.Interfaces;
    using DJPad.Core.Utils;
    using DJPad.UI;
    using DJPad.Player;
    using System.Windows.Forms;
    using DJPad.UI.GdiPlus;

    public class ConfigurationUi : BaseUi
    {
        public override IList<LightControl<Bitmap>> GenerateUI(PlayerState state, WindowState window)
        {
            var corpus = IoCContainer.Get<IMediaCorpus>();
            this.Size = new Size(400, 400);
            var background = Color.Black;
            this.Window = window;

            Window.StartRendering();

            return new List<LightControl<Bitmap>>
                {
                    new LightTextPanel
                    {
                        Name = "AlbumsStored",
                        AlwaysVisible = true,
                        Extents = new Rectangle
                                {
                                    X = 60,
                                    Y = 60,
                                    Width = Size.Width - 120,
                                    Height = Size.Height - 120
                                },
                        Font = new Font("Segoe UI Light", this.Size.Height/4, FontStyle.Bold),
                        Justify = LightTextPanel.Justification.Center,
                        Color = () => Color.Gainsboro,
                        BorderColor = () => Color.SlateGray,
                        Text = () => corpus.AlbumCount().ToString()
                    },
                    new LightTextBox
                    {
                        Name = "Config",
                        AlwaysVisible = true,
                        Background = () => background,
                        Extents = new Rectangle(3, this.Size.Height - 30, this.Size.Width - 48, 25),
                        Text = state.Configuration.WatchDirectory,
                    },
                    new LightButton
                    {
                        Name = "Open",
                        Extents = new Rectangle
                                {
                                    X = this.Size.Width - 40,
                                    Y = this.Size.Height - 30,
                                    Width = 20,
                                    Height = 20,
                                },
                        OnClick = p =>
                                {
                                    var open = new FolderBrowserDialog()
                                    {
                                        ShowNewFolderButton = false,
                                        RootFolder = Environment.SpecialFolder.Desktop
                                    };

                                    if (open.ShowDialog() == DialogResult.OK)
                                    {
                                        if (!string.IsNullOrWhiteSpace(open.SelectedPath))
                                        {
                                            state.Configuration.WatchDirectory = open.SelectedPath;
                                            state.Configuration.Save();
                                        }
                                    }
                                },
                        Image = () => Resources.Resources.WatchFolder
                    },
                };
        }
    }
}