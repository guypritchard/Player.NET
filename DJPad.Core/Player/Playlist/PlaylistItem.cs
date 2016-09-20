namespace DJPad.Player
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Core.Player.Playlist;
    using DJPad.Core.Utils;
    using DJPad.Sources;

    public class PlaylistItem : IIndexable, IDisposable, IPlaylistItem
    {
        private static readonly Random Random = new Random();

        public int RandomIndex { get; private set; }

        public int Index { get; set; }

        public string FileName { get; private set; }

        public string FullFileName { get; private set; }

        public IFileSource Source
        {
            get { return this.LazySource.Value.Result; }
        }

        public IMetadata Metadata
        {
            get
            {
                if (!this.HasLoadedMetadata)
                {
                    return new AsyncMetadataSource
                    {
                        Title = Path.GetFileName(this.FileName),
                    };
                }

                return this.LazyMetadata.Value.Result;
            }
        }

        public Bitmap SynchronousArt
        {
            get { return this.LazyMetadata.Value.Result.AlbumArt; }
        }

        public PlaylistItemState State { get; private set; }

        private AsyncLazy<IFileSource> LazySource;

        private AsyncLazy<IMetadata> LazyMetadata; 

        public bool HasLoadedMetadata
        {
            get { return this.LazyMetadata.Value.IsCompleted; }
        }

        public event EventHandler<IMetadata> MetadataLoaded;

        public PlaylistItem(string fullFileName, int index = 0)
        {
            this.FullFileName = fullFileName;
            this.FileName = Path.GetFileNameWithoutExtension(this.FullFileName);
            this.RandomIndex = Random.Next();
            this.Index = index;
            this.Init();
        }

        public bool IsPlayable()
        {
            return File.Exists(this.FileName) && this.State == PlaylistItemState.Loaded;
        }

        public void Reset()
        {
            try
            {
                this.State = PlaylistItemState.Loading;
                this.Source.Load(this.FullFileName);
                this.Source.Position = TimeSpan.Zero;
                this.State = PlaylistItemState.Loaded;
            }
            catch (FileNotFoundException)
            {
                this.State = PlaylistItemState.ItemNotFound;
            }
            catch (IOException)
            {
                this.State = PlaylistItemState.UnplayableItem;
            }
            catch (Exception)
            {
                this.State = PlaylistItemState.UnplayableItem;
            }
        }

        /// <summary>
        /// Overridden string for debugging purposes.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.FullFileName;
        }

        public string[] GetStrings()
        {
            return null;
        }

        public void Dispose()
        {
            this.Unload();
        }

        private void Unload()
        {
            this.Source.Close();
            this.Init();
            this.State = PlaylistItemState.Unloaded;
        }

        private void Init()
        {
            this.LazySource = new AsyncLazy<IFileSource>(() =>
            {
                IFileSource source = new NullSource() { FileName = this.FullFileName };

                try
                {
                    this.State = PlaylistItemState.Loading;
                    var fileSource = SourceRegistry.ResolveSource(this.FullFileName, true);
                    fileSource.Load(this.FullFileName);
                    fileSource.GetMetadata();
                    this.State = PlaylistItemState.Loaded;
                    source = fileSource;
                }
                catch (FileNotFoundException)
                {
                    this.State = PlaylistItemState.ItemNotFound;
                }
                catch (IOException)
                {
                    this.State = PlaylistItemState.UnplayableItem;
                }
                catch (Exception)
                {
                    this.State = PlaylistItemState.UnplayableItem;
                }

                return source;
            });
               
            this.LazyMetadata = new AsyncLazy<IMetadata>(() =>
            {
                var metadata = new NullSource() { FileName = this.FullFileName }.GetMetadata();

                try
                {
                    metadata = this.Source.GetMetadata();
                    if (this.MetadataLoaded != null)
                    {
                        this.MetadataLoaded(this, metadata);
                    }
                }
                catch (FileNotFoundException)
                {
                    Debug.WriteLine("{0} not found.", this.FullFileName);
                }
                catch (IOException)
                {
                    Debug.WriteLine("Unable to load {0}", this.FullFileName);
                }
                catch (Exception)
                {
                    Debug.WriteLine("Unexpected error loading {0}", this.FullFileName);
                }

                return metadata;
            });
        }
        
    }
}