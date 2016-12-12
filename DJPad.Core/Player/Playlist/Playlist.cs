namespace DJPad.Player
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using DJPad.Core;
    using DJPad.Core.Player.Playlist;
    using DJPad.Core.Interfaces;

    public class Playlist
    {
        private int currentIndex;

        private int nextIndex;

        private int previousIndex;

        private bool internalRandom;

        private bool useRandom;

        private List<IPlaylistItem> RandomItems;

        private readonly Corpus<IPlaylistItem, IMetadata> corpus = new Corpus<IPlaylistItem, IMetadata>();

        public Playlist(IList<IPlaylistItem> files = null)
        {
            this.Items = new List<IPlaylistItem>();
            this.RandomItems = new List<IPlaylistItem>();

            this.previousIndex = -1;
            this.currentIndex = 0;
            this.nextIndex = 1;
            this.Loaded = DateTime.UtcNow;

            if (files != null)
            {
                foreach (var file in files)
                {
                    file.MetadataLoaded += (sender, metadata) => corpus.Add((IPlaylistItem)sender, metadata);
                }

                this.Items.AddRange(files);
                this.RandomItems.AddRange(this.Items.OrderBy(f => f.RandomIndex));
            }
        }

        public bool Repeat
        {
            get;set;
        }

        public bool Random {
            get
            {
                return this.internalRandom;
            }
            set
            {
                if (value)
                {
                    this.currentIndex = this.RandomItems.IndexOf(this.Current);
                    this.nextIndex = this.currentIndex + 1;
                    this.previousIndex = this.currentIndex - 1;
                }
                else
                {
                    this.currentIndex = this.Items.IndexOf(this.Current);
                    this.nextIndex = this.currentIndex + 1;
                    this.previousIndex = this.currentIndex - 1;
                }

                this.internalRandom = value;

            }
        }

        public IPlaylistItem Current
        {
            get
            {
                if (this.Random)
                {
                    return this.RandomItems.ElementAtOrDefault(this.currentIndex);
                }

               return this.Items.ElementAtOrDefault(this.currentIndex);
            }
            set
            {
                int index = this.Random 
                            ? this.RandomItems.IndexOf(value)
                            : this.Items.IndexOf(value); 

                if (index > -1)
                {
                    this.currentIndex = index;
                    this.previousIndex = this.currentIndex - 1;
                    this.nextIndex = this.currentIndex + 1;
                }
            }
        }

        public List<IPlaylistItem> Items { get; private set; }

        public IPlaylistItem Next
        {
            get
            {
                if (this.useRandom)
                {
                    return this.RandomItems.ElementAtOrDefault(this.nextIndex);
                }

                return this.Items.ElementAtOrDefault(this.nextIndex);
            }
        }

        public IPlaylistItem Previous
        {
            get
            {
                if (this.useRandom)
                {
                    return this.RandomItems.ElementAtOrDefault(this.previousIndex);
                }

                return this.Items.ElementAtOrDefault(this.previousIndex);
            }
        }

        public bool Empty
        {
            get
            {
                return !this.Items.Any(); 
            }
        }

        public bool Start
        {
            get { return !this.Empty && this.Previous == null; }
        }

        public bool End
        {
            get { return !this.Empty && this.Next == null; }
        }

        public int CurrentIndex
        {
            get
            {
                if (this.Random)
                {
                    return this.RandomItems.IndexOf(this.Current) + 1;
                }

                return this.Items.IndexOf(this.Current) + 1;
            }
        }

        public int Count
        {
            get { return this.Items.Count; }
        }


        public IEnumerable<IPlaylistItem> Search(string search)
        {
            return this.corpus.Find(search);
        }

        public void MoveForward(int skip)
        {
            this.currentIndex = this.Items.IndexOf(this.Current);
            this.nextIndex = this.currentIndex + skip;
            this.previousIndex = this.currentIndex - skip;

            this.currentIndex = this.nextIndex;
            this.nextIndex++;
            this.previousIndex++;
        }
        
        public void MoveBackward(int skip)
        {
            this.currentIndex = this.Items.IndexOf(this.Current);
            this.nextIndex = this.currentIndex + skip;
            this.previousIndex = this.currentIndex - skip;
            
            this.currentIndex = this.previousIndex;
            this.nextIndex--;
            this.previousIndex--;
        }
        
        public void MoveNext()
        {
            this.useRandom = this.Random;
            if (this.Random)
            {
                this.currentIndex = this.RandomItems.IndexOf(this.Current);
                this.nextIndex = this.currentIndex + 1;
                this.previousIndex = this.currentIndex - 1;
            }
            else
            {
                this.currentIndex = this.Items.IndexOf(this.Current);
                this.nextIndex = this.currentIndex + 1;
                this.previousIndex = this.currentIndex - 1;
            }

            this.currentIndex = this.nextIndex;
            this.nextIndex++;
            this.previousIndex++;
        }

        public void MovePrevious()
        {
            this.useRandom = this.Random;
            if (this.Random)
            {
                this.currentIndex = this.RandomItems.IndexOf(this.Current);
                this.nextIndex = this.currentIndex + 1;
                this.previousIndex = this.currentIndex - 1;
            }
            else
            {
                this.currentIndex = this.Items.IndexOf(this.Current);
                this.nextIndex = this.currentIndex + 1;
                this.previousIndex = this.currentIndex - 1;
            }
            
            this.currentIndex = this.previousIndex;
            this.nextIndex--;
            this.previousIndex--;
        }

        public void MoveRandom()
        {
            this.Items.OrderBy(i => i.RandomIndex).ElementAt(this.currentIndex);
        }

        public void MoveToItem(string filename)
        {
            foreach (var t in this.Items)
            {
                if (string.Compare(filename, t.FullFileName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    this.Current = t;
                    return;
                }
            }
        }

        public void Save(string location, string name)
        {
            var fileName = Path.Combine(location, name);
            using (var fs = File.Open(fileName, FileMode.Create))
            using (var streamWrite = new StreamWriter(fs))
            {
                foreach(var item in this.Items)
                {
                    streamWrite.WriteLine(item);
                }
            }

            this.FileName = fileName;
            this.Loaded = DateTime.Now;
        }

        public string FileName { get; set; }

        public DateTime Loaded { get; set; }

        public override string ToString()
        {
            return currentIndex.ToString(CultureInfo.InvariantCulture);
        }
    }
}