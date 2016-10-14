namespace DJPad.Player
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Core.Utils;
    using DJPad.Output.DirectSound;
    using System.Windows.Forms;

    public class PlayerState
    {
        public event Action<IPlaylistItem, Status> PlayStateChanged;
        public event Action<IPlaylistItem> ItemLoadStateChanged;
        public event Action PlaylistChanged;

        public IAudioOutput Audio { get; set; }
        public Playlist Playlist { get; set; }
        public PlayerConfiguration Configuration { get; set; }

        private bool playRequested = false;
        
        public PlayerState(IMediaCorpus corpus = null)
        {
            this.Configuration = ApplicationConfiguration.Load<PlayerConfiguration>(PlayerConfiguration.DefaultLocation);
            this.Playlist = new Playlist();
        }

        public void Init()
        {
            if (!string.IsNullOrEmpty(this.Configuration.PlaylistName) && File.Exists(this.Configuration.PlaylistName))
            {
                this.NewPlaylist(this.Configuration.PlaylistName);
            }
            else if (File.Exists(this.Configuration.LastFile))
            {
                this.NewPlaylist(this.Configuration.LastFile);
            }

            this.Playlist.Random = this.Configuration.Randomise;
        }

        public void NewPlaylist(string startingFile, bool autoPlay = false)
        {
            if (String.Compare(Path.GetExtension(startingFile), ".ppl", StringComparison.OrdinalIgnoreCase) == 0)
            {
                Playlist = PlaylistGenerator.FromPlaylistFile(startingFile);

                if (File.Exists(this.Configuration.LastFile))
                {
                    Playlist.MoveToItem(this.Configuration.LastFile);
                }
            }
            else if (Directory.Exists(startingFile))
            {
                Playlist = PlaylistGenerator.FromDirectory(startingFile, true);
                this.FirePlaylistChanged();
                Playlist.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), PlayerConfiguration.ApplicationName), "Temp.ppl");
            }
            else
            {
                Playlist = PlaylistGenerator.FromDirectory(Path.GetDirectoryName(startingFile), false);
                this.FirePlaylistChanged();
                Playlist.MoveToItem(startingFile);
            }

            Play(this.Playlist.Current, autoPlay);
        }

        public void NewPlaylist(string[] paths, bool autoPlay = false)
        {
            Playlist = PlaylistGenerator.FromMultiplePaths(paths);
            this.FirePlaylistChanged();

            Playlist.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), PlayerConfiguration.ApplicationName), "Temp.ppl");
            Play(this.Playlist.Current, autoPlay);
        }
        
        public void TogglePlay()
        {
            if (!Playlist.Empty)
            {
                // Toggle playback.
                this.playRequested = !this.playRequested;

                if (this.IsPlaying)
                {
                    this.Audio.Stop();
                }
                else if (this.CanPlay)
                {
                    if (this.Playlist.Current.Source.EndOfFile)
                    {
                        this.Playlist.Current.Source.Position = TimeSpan.Zero;
                    }

                    this.Audio.Play();
                }

                this.FirePlayStateChanged(this.Playlist.Current, this.Audio.State);
            }
        }

        public void Next()
        {
            if (!Playlist.End)
            {
                var finished = Playlist.Current;

                Playlist.MoveNext();
                this.Play(Playlist.Current, this.KeepPlaying);

                if (finished != null)
                {
                    finished.Dispose();
                }
            }
        }
        
        public void Previous()
        {
            if (!Playlist.Start)
            {
                var finished = Playlist.Current;

                Playlist.MovePrevious();
                this.Play(Playlist.Current, this.KeepPlaying);

                if (finished != null)
                {
                    finished.Dispose();
                }
            }
        }

        public void Play(IPlaylistItem item, bool autoPlay = false)
        {
            if (item == null)
            {
                return;
            }

            if (this.Audio != null)
            {
                this.Audio.Stop();
            }
            
            CreatePlayGraph(item);
            this.FireLoadStateChanged(item);
            
            if (item.State == PlaylistItemState.Loaded)
            {
                if (this.Playlist.Current.Source.EndOfFile)
                {
                    this.Playlist.Current.Source.Position = TimeSpan.Zero;
                }

                if (autoPlay)
                {
                    this.playRequested = true;
                    this.Audio.Play();
                }

                this.Configuration.PlaylistName = Playlist.FileName;
                this.Configuration.LastFile = item.FullFileName;
                this.Configuration.Save();
            }
            else
            {
                this.playRequested = false;
                this.Audio.Stop();
            }

            this.FirePlayStateChanged(item, this.Audio.State);
        }

        public bool CanPlay
        {
            get
            {
                return !this.Playlist.Empty 
                    && this.Audio != null 
                    && this.Playlist.Current.State != PlaylistItemState.ItemNotFound 
                    && this.Playlist.Current.State != PlaylistItemState.UnplayableItem;
            }
        }

        public bool IsPlaying
        {
            get { return this.CanPlay && (this.Audio.State == Status.Buffering || this.Audio.State == Status.Playing || this.Audio.State == Status.Stopping); }
        }

        public bool KeepPlaying
        {
            get { return this.CanPlay && this.playRequested; }
        }

        private void CreatePlayGraph(IPlaylistItem item)
        {
            if (this.Audio == null)
            {
                this.Audio = new DirectSoundOut();
                this.Audio.FinishedEvent += (obj, args) =>
                {
                    if (Playlist.Repeat)
                    {
                        Debug.WriteLine("Stop caught, repeat required.");
                        Playlist.Current.Source.Position = TimeSpan.Zero;
                        this.Play(Playlist.Current, this.KeepPlaying);
                        return;
                    }

                    Debug.WriteLine("Stop caught.");
                    if (this.Playlist.End)
                    {
                        this.Stop();
                        return;
                    }

                    this.Next();
                };
            }

            if (item.Source != null && item.State == PlaylistItemState.Loaded)
            {
                this.Audio.SampleSource = item.Source;
                this.Audio.Volume = this.Configuration.Volume;
            }
        }

        public void Stop()
        {
            if (this.Audio != null)
            {
                this.Audio.Stop();
            }
            
            this.playRequested = false;
            this.FirePlayStateChanged(this.Playlist.Current, this.Audio == null ? Status.Stopped : this.Audio.State);
        }

        public void Open()
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                FolderBrowserDialog open = new FolderBrowserDialog()
                {
                    ShowNewFolderButton = false,
                    RootFolder = Environment.SpecialFolder.Desktop
                };

                if (open.ShowDialog() == DialogResult.OK)
                {
                    if (!string.IsNullOrWhiteSpace(open.SelectedPath))
                    {
                        this.NewPlaylist(open.SelectedPath, true);
                    }
                }
            }
            else
            {
                var open = new OpenFileDialog
                {
                    Filter = "Playable files|*.mp3;*.wma;*.wav;*.cda;*.ppl|MP3 files (*.mp3)|*.mp3|WMA files (*.wma)|*.wma|Wave files (*.wav)|*.wav|Playlists (*.ppl)|*.ppl|All files (*.*)|*.*",
                    AutoUpgradeEnabled = true,
                    InitialDirectory = this.Playlist.Empty
                        ? Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                        : Path.GetDirectoryName(this.Playlist.Current.FullFileName),
                    RestoreDirectory = false
                };

                if (open.ShowDialog() == DialogResult.OK)
                {
                    this.NewPlaylist(open.FileName, true);
                }
            }
        }

        private void FirePlayStateChanged(IPlaylistItem item, Status state)
        {
            if (this.PlayStateChanged != null)
            {
                this.PlayStateChanged(item, state);
            }
        }

        private void FireLoadStateChanged(IPlaylistItem item)
        {
            if (this.ItemLoadStateChanged != null)
            {
                this.ItemLoadStateChanged(item);
            }
        }

        private void FirePlaylistChanged()
        {
            if (this.PlaylistChanged != null)
            {
                this.PlaylistChanged();
                Debug.WriteLine("Playlist changed.");
            }
        }

    }
}