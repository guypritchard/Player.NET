namespace DJPad.UI.GdiPlus
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using DJPad.Core.Utils;
    using DJPad.Player;
    using DJPad.Core;
    using Resources;
    using System.Collections.Concurrent;

    public class LightPlaylistItem : LightButton
    {
        private readonly CachingBitmapProducer<Bitmap> bitmapCache = new CachingBitmapProducer<Bitmap>();

        private static ConcurrentDictionary<string, Bitmap> albumArtCache = new ConcurrentDictionary<string, Bitmap>(); 

        public enum DrawMode { Banner, Playlist, Playlist_Small }

        public bool HideTime { get; set; }

        public Func<IPlaylistItem> Item { get; set; }

        public Func<Playlist> Playlist { get; set; } 

        public LightPlaylistItem(DrawMode mode = DrawMode.Banner)
        {
            this.bitmapCache.BitmapProducer = () =>
            {
                var playlistItem = Item();
                if (playlistItem == null)
                {
                    return null;
                }

                switch (mode)
                {
                    case DrawMode.Banner:
                        return this.DrawBanner();
                    case DrawMode.Playlist:
                        return this.DrawPlaylist();
                    case DrawMode.Playlist_Small:
                        return this.DrawPlaylist();
                    default:
                        throw new InvalidOperationException();
                }
            };

            this.Image = () =>
            {
                var item = Item();
                if (item != null)
                {
                    var cacheKey = item + 
                                   (Playlist != null ? Playlist().CurrentIndex.ToString() : string.Empty) +
                                   Playlist().Loaded + "_" + item.HasLoadedMetadata;

                    return this.bitmapCache.GetBitmap(cacheKey);
                }
                return null;
            };
        }

        /// <summary>
        /// Add an event handler which can do something once the metadata has loaded.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="refresh"></param>
        public LightPlaylistItem(IPlaylistItem item, Action onMetadataLoaded, DrawMode mode = DrawMode.Playlist) : this(DrawMode.Playlist)
        {
            this.Item = () => item;

            if (!item.HasLoadedMetadata)
            {
                item.MetadataLoaded += (sender, args) => onMetadataLoaded();
            }
        }

        private Bitmap DrawBanner()
        {
            var bitmap = new Bitmap(this.Extents.Width, this.Extents.Height, PixelFormat.Format32bppPArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                if (this.Background() != Color.Transparent)
                {
                    graphics.FillRectangle(new SolidBrush(this.Background()), new Rectangle()
                    {
                        Width = this.Extents.Width,
                        Height = this.Extents.Height
                    });
                }

                if (Item() != null)
                {
                    graphics.DrawImage(Item().Metadata.AlbumArt ?? Resources.Unknown, 0, 0, this.Extents.Height, this.Extents.Height);

                    var text = new MetadataTextImageProducer
                    {
                        StringFormat = new StringFormat
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Near,
                            FormatFlags = StringFormatFlags.NoWrap,
                            Trimming = StringTrimming.EllipsisCharacter,
                        }
                    }.DrawText(
                        string.Format(
                            "{0}\r\n{1}\r\n{2}",
                            Item().Metadata.Title,
                            Item().Metadata.Album,
                            Item().Metadata.Artist),
                        Color.White,
                        Color.Transparent,
                        Color.DimGray,
                        new Font("Segoe UI Light", 13, FontStyle.Bold),
                        new Size(this.Extents.Width - this.Extents.Height - 20, this.Extents.Height + 2));

                    graphics.DrawImage(text, this.Extents.Height, -2,
                        this.Extents.Width - this.Extents.Height + 2 - 20, this.Extents.Height);
                }

                if (Playlist != null && Playlist() != null)
                {
                    var position = new MetadataTextImageProducer
                    {
                        StringFormat = new StringFormat
                        {
                            Alignment = StringAlignment.Far,
                            LineAlignment = StringAlignment.Far,
                            FormatFlags = StringFormatFlags.NoWrap,
                            Trimming = StringTrimming.EllipsisCharacter,
                        }
                    }.DrawText(
                        string.Format(
                            "{0}\r\n{1} of {2}",
                            !this.HideTime ? Item().Metadata.Duration.Detailed() : string.Empty,
                            Item().Index + 1,
                            Playlist().Count),
                        Color.White,
                        Color.Transparent,
                        Color.SlateGray,
                        new Font("Segoe UI", 13, FontStyle.Regular),
                        new Size(this.Extents.Width - this.Extents.Height, this.Extents.Height));

                    graphics.DrawImage(
                        position,
                        this.Extents.Height,
                        0,
                        this.Extents.Width - this.Extents.Height,
                        this.Extents.Height);
                }
            }

            return bitmap;
        }

        private Bitmap DrawPlaylist()
        {
            var bitmap = new Bitmap(this.Extents.Width, this.Extents.Height, PixelFormat.Format32bppPArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                if (this.Background() != Color.Transparent)
                {
                    graphics.FillRectangle(new SolidBrush(this.Background()), new Rectangle()
                    {
                        Width = this.Extents.Width,
                        Height = this.Extents.Height
                    });
                }

                if (Item() != null)
                {
                    var item = Item();

                    // This call can be expensive...
                    // Some caching/cleverness may be required.
                    if (!item.HasLoadedMetadata)
                    {
                        graphics.DrawImage(Resources.Unknown, 3, 18, 30, 30); 
                    }
                    else if (!albumArtCache.ContainsKey(item.Metadata.Album))
                    {
                        albumArtCache.TryAdd(item.Metadata.Album, item.Metadata.AlbumArt.Resize(new Size(30, 30)));
                        graphics.DrawImage(albumArtCache[item.Metadata.Album] ?? Resources.Unknown, 3, 18, 30, 30);
                    }
                    else
                    {
                        graphics.DrawImage(albumArtCache[item.Metadata.Album] ?? Resources.Unknown, 3, 18, 30, 30);
                    }

                    var numberText = new MetadataTextImageProducer
                    {
                        StringFormat = new StringFormat
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Near,
                            FormatFlags = StringFormatFlags.NoWrap,
                        }
                    }.DrawText(
                        string.Format(
                            "{0}.",
                            item.Index + 1),
                        Color.White,
                        Color.Transparent,
                        Color.SlateGray,
                        new Font("Segoe UI Light", 13, FontStyle.Bold),
                        new Size(33, 33));

                    graphics.DrawImage(numberText, 3, 0, numberText.Width, numberText.Height);

                    var text = new MetadataTextImageProducer
                    {
                        StringFormat = new StringFormat
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Near,
                            FormatFlags = StringFormatFlags.NoWrap,
                            Trimming = StringTrimming.EllipsisCharacter,
                        }
                    }.DrawText(item.Metadata.Title,
                        Color.White,
                        Color.Transparent,
                        Color.SlateGray,
                        new Font("Segoe UI Light", 13, FontStyle.Bold),
                        new Size(this.Extents.Width - 80, this.Extents.Height));

                    graphics.DrawImage(text, 35, 0, text.Width, text.Height);
                    
                    text = new MetadataTextImageProducer
                    {
                        StringFormat = new StringFormat
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Near,
                            FormatFlags = StringFormatFlags.NoWrap,
                            Trimming = StringTrimming.EllipsisCharacter,
                        }
                    }.DrawText(
                        string.Format(
                            "{0}\r\n{1}",
                            item.Metadata.Album,
                            item.Metadata.Artist),
                        Color.Azure,
                        Color.Transparent,
                        Color.SlateGray,
                        new Font("Segoe UI Light", 11, FontStyle.Italic),
                        new Size(this.Extents.Width - 36, this.Extents.Height));

                    graphics.DrawImage(text, 36, 17, text.Width, text.Height);


                    text = new MetadataTextImageProducer
                    {
                        StringFormat = new StringFormat
                        {
                            Alignment = StringAlignment.Far,
                            LineAlignment = StringAlignment.Near,
                            FormatFlags = StringFormatFlags.NoWrap,
                        }
                    }.DrawText(
                       string.Format("{0}", Item().Source.GetFileType().TrimStart(new[] {'.'})),
                       Color.Azure,
                       Color.Transparent,
                       Color.SlateGray,
                       new Font("Segoe UI Light", 11, FontStyle.Italic),
                       new Size(this.Extents.Width - 36, this.Extents.Height));

                    graphics.DrawImage(text, 36, 17, text.Width, text.Height);
                }

                if (Playlist != null && Playlist() != null)
                {
                    var position = new MetadataTextImageProducer
                    {
                        StringFormat = new StringFormat
                        {
                            Alignment = StringAlignment.Far,
                            LineAlignment = StringAlignment.Near,
                            FormatFlags = StringFormatFlags.NoWrap,
                        }
                    }.DrawText(Item().Metadata.Duration.Detailed(),
                        Color.White,
                        Color.Transparent,
                        Color.SlateGray,
                        new Font("Segoe UI", 13, FontStyle.Bold),
                        new Size(this.Extents.Width - this.Extents.Height, this.Extents.Height));

                    graphics.DrawImage(
                        position,
                        this.Extents.Height,
                        0,
                        this.Extents.Width - this.Extents.Height,
                        this.Extents.Height);
                }
            }

            return bitmap;
        }
    }
}