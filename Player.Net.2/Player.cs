namespace Player.Net._2
{
    using System;
    using System.Linq;
    using System.Windows.Forms;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Core.Utils;
    using DJPad.Player;
    using DJPad.UI;
    using DJPad.UI.GdiPlus;
    using UserInterface;
    using Resources;
    using System.Drawing;
    using DJPad.Db;
    using System.Diagnostics;

    public class Player : GdiPlusChromelessForm
    {
        private readonly PlayerState playerState;
        private readonly MultimediaKeyListener listener;
        private readonly WindowState windowState;
        readonly IUserInterface<Bitmap>[] userInterfaces;
        private Scanner scanner;
 
        public Player() : base(false)
        {
            this.Icon = Resources.Player;
            this.playerState = new PlayerState(IoCContainer.Get<IMediaCorpus>());
            this.listener = new MultimediaKeyListener();
            this.userInterfaces = new IUserInterface<Bitmap>[]
            {
                // new NormalUi(), 
                new SmallPlayerUi(), 
                new TinyPlayerUi()
            };

            this.listener.KeyPressed += (sender, args) =>
            {
                switch (args.Key)
                {
                    case MultimediaKey.PlayPause:
                        playerState.TogglePlay();
                        break;
                    case MultimediaKey.NextTrack:
                        playerState.Next();
                        break;
                    case MultimediaKey.PreviousTrack:
                        playerState.Previous();
                        break;
                }
            };

            this.DragEnter += (sender, args) =>
                            {
                                args.Effect = args.Data.GetDataPresent(DataFormats.FileDrop)
                                            ? DragDropEffects.Copy
                                            : DragDropEffects.None;
                            };

            this.DragDrop += (sender, args) =>
                            {
                                var fileNames = args.Data.GetData(DataFormats.FileDrop) as string[];
                                if (fileNames == null || !fileNames.Any())
                                {
                                    return;
                                }
                                if (fileNames.Length == 1)
                                {
                                    this.playerState.NewPlaylist(fileNames.Single(), this.playerState.IsPlaying);
                                }
                                else
                                {
                                    this.playerState.NewPlaylist(fileNames, this.playerState.IsPlaying);
                                }
                            };

            this.windowState = new WindowState
                            {
                                Close = () => this.Close(),
                                Repaint = () => this.Invalidate(),
                                Location = () => this.Location,
                                Form = this,
                                ChangeUi = () => this.ChangeUi(),
                            };

            this.playerState.Init();

            this.playerState.PlayStateChanged += 
                (i,s) => this.InvokeIfRequired(() =>
                {
                    var shell = new WindowsSpecificShell();
                    shell.SetOverlayIcon(this.Handle, i, s == Status.Playing);
                });

            this.Shown += 
                (o, e) => this.InvokeIfRequired(() =>
                {
                    var shell = new WindowsSpecificShell();
                    shell.SetOverlayIcon(this.Handle, this.playerState.Playlist.Current, this.playerState.IsPlaying);
                });

            if (!string.IsNullOrEmpty(this.playerState.Configuration.WatchDirectory))
            {
                this.scanner = new Scanner(
                    this.playerState.Configuration.WatchDirectory, 
                    SourceRegistry.GetSupportedFileTypes(),
                    directory =>
                    {
                         var corpus = IoCContainer.Get<IMediaCorpus>();
                         Debug.WriteLine(directory);
                         corpus.Initialize(directory, forceRecreate: true);
                    },
                    supportedFile =>
                    {
                        var corpus = IoCContainer.Get<IMediaCorpus>();
                        Debug.WriteLine(supportedFile);
                        var source = SourceRegistry.ResolveSource(supportedFile, true);
                        source.Load(supportedFile);
                        var metadata = source.GetMetadata();
                        corpus.AddOrUpdate(metadata, supportedFile);
                    });

                this.scanner.Scan();
            }

            this.Initialize();
        }

        protected void Initialize()
        {
            IUserInterface<Bitmap> userInterface = this.userInterfaces[this.playerState.Configuration.Mode];
            var layout = userInterface.GenerateUI(this.playerState, this.windowState);
            this.LightControlCollection.Clear();
            this.LightControlCollection.AddRange(layout);
            this.Text = userInterface.Name;
            this.MaximumSize = userInterface.Size;
            this.ClientSize = userInterface.Size;
            this.SetVolumePosition();
        }

        protected void ChangeUi()
        {
            this.playerState.Configuration.Mode = ++this.playerState.Configuration.Mode % this.userInterfaces.Count();
            this.playerState.Configuration.Save();
            this.Initialize();
        }

        protected override void OnMove(EventArgs e)
        {
            if (this.playerState.CanPlay)
            {
                var height = Screen.FromControl(this).Bounds.Bottom;
                var positionPercentage = 100 - (short)(this.Top * 100 / (height));
                this.playerState.Configuration.Volume = (short)positionPercentage > 100 ? (short)100 : (short)positionPercentage;
                this.playerState.Audio.Volume = this.playerState.Configuration.Volume;
            }

            base.OnMove(e);
        }

        private void SetVolumePosition()
        {
            var totalHeight = Screen.FromControl(this).Bounds.Bottom;
            var height = totalHeight - (this.playerState.Configuration.Volume*totalHeight/100);
            this.Location = new Point((Screen.FromControl(this).Bounds.Width - this.ClientSize.Width) / 2, height);
        }
    }
}