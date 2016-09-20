namespace DJPad.Sources
{
    using System;
    using System.Drawing;
    using DJPad.Core.Interfaces;

    public class AsyncMetadataSource : IMetadata
    {
        public string Album { get; set; }

        public string Artist { get; set; }

        public string Title { get; set; }

        public Bitmap AlbumArt { get; set; }

        public TimeSpan Duration { get; set; }
    }

    public class SimpleMetadataSource : IMetadata
    {
        public SimpleMetadataSource()
        { 
        }

        public SimpleMetadataSource(IMetadata data)
        {
            this.Album = data.Album;
            this.Artist = data.Artist;
            this.Title = data.Title;
            this.Duration = data.Duration;
            this.AlbumArt = data.AlbumArt;
        }

        public string Album { get; set; }

        public string Artist { get; set; }

        public string Title { get; set; }

        public Bitmap AlbumArt { get; set; }

        public TimeSpan Duration { get; set; }
    }
}