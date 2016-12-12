namespace DJPad.UI.GdiPlus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Linq;
    using System.Windows.Forms;

    using DJPad.Core.Utils;
    using DJPad.Player;
    using DJPad.UI.Interfaces;
    using DJPad.Core;
    using Resources;

    public sealed class LightPlaylist : LightButton, IScrollable, IAcceptKeys
    {
        public enum DrawMode
        {
            Playlist,
            Playlist_Small,
            Hierarchical,
        };

        private readonly List<LightPlaylistItem> VisibleItems = new List<LightPlaylistItem>();
        private readonly CachingBitmapProducer<Bitmap> bitmapCache = new CachingBitmapProducer<Bitmap>();
        private const int visibleItemCount = 8;
        private Func<Playlist> playlist;
        private int scrollPosition;
        private int currentPosition;
        private DateTime playlistVersion;

        public Func<int, bool> OnScroll { get; set; }

        public Func<Playlist> List
        {
            get { return this.playlist; }
            set
            {
                this.playlist = value;
                if (this.playlist != null)
                {
                    RefreshPlaylistIfChanged();
                }
            }
        }
        
        public Action<IPlaylistItem> OnItemClicked { get; set; }

        public Action<IPlaylistItem> OnItemDoubleClicked { get; set; }

        public Action<Keys, bool, bool> OnKey { get; set; }

        public Action Redraw { get; set; }

        public Func<bool> IsPlaying { get; set; }

        public DrawMode Mode
        {
            get
            {
                return this.mode;
            }

            set
            {
                this.mode = value;
            }
        }

        private DrawMode mode;

        public LightPlaylist()
        {
            bitmapCache.BitmapProducer = () =>
            {
                RefreshPlaylistIfChanged();

                var images = this.VisibleItems
                                 .OrderBy(i => i.Item().Index)
                                 .Select(i =>
                                    {
                                        Bitmap image = i.Image();
                                        if (i.Item() == List().Current)
                                        {
                                            image = image.Overlay(BitmapExtensions.CreateImage(image.Size, Color.FromArgb(70, 128, 128, 128)))
                                                         .Overlay(BitmapExtensions.CreateImage(
                                                             new Size(3, image.Height), 
                                                             i.Item().SynchronousArt != null 
                                                             ? i.Item().SynchronousArt.GetPalette().Brightest
                                                             : Resources.Unknown.GetPalette().Brightest))
                                                         .Overlay(this.IsPlaying() ? Resources.Player_Play_Small : Resources.Player_Pause_Small, new Rectangle(5, 20, 25, 25));
                                        }

                                        // Experimental scroll position drawing.
                                        //if (i.Item().Index == this.scrollPosition)
                                        //{
                                        //    image = image.Overlay(BitmapExtensions.CreateImage(image.Size, Color.FromArgb(70, 128, 128, 128)));
                                        //}

                                        return image;
                                    }).ToList();

                var newImage = new Bitmap(this.Extents.Width, this.Extents.Height, PixelFormat.Format32bppPArgb);
                
                var yPos = 0;
                using (var graphics = Graphics.FromImage(newImage))
                {
                    graphics.FillRectangle(new SolidBrush(Background()), 0, 0, newImage.Width, newImage.Height);
                    foreach (var image in images)
                    {
                        if (yPos >= this.Extents.Height) break;

                        graphics.DrawImage(image, 0, yPos, this.Extents.Width - 3, image.Height);
                        yPos += image.Height;
                    }

                    var positionColor = List().Current.SynchronousArt != null
                        ? List().Current.SynchronousArt.GetPalette().Brightest
                        : Resources.Unknown.GetPalette().Brightest;
                    
                    var positionOfCurrent = (float)List().Current.Index/List().Count;
                    var percentageScrolled = this.scrollPosition <= 0 ? 0.0f : (float)this.scrollPosition/List().Count;

                    var scrollPos = (int)this.Extents.Height * percentageScrolled;
                    var playPos = (int)this.Extents.Height*positionOfCurrent;

                    var top = (int)Math.Min(scrollPos, playPos);
                    var bottom = (int)Math.Max(scrollPos, playPos);

                    //if (Math.Abs(top - bottom) > 0)
                    //{
                    //    graphics.FillRectangle(
                    //        new LinearGradientBrush(
                    //            new Point(0, 0),
                    //            new Point(0, Math.Abs(top - bottom)),
                    //            Color.White,
                    //            positionColor),
                    //        this.Extents.Width - 5,
                    //        (int)(this.Extents.Height * Math.Min(percentageScrolled, positionOfCurrent)),
                    //        5,
                    //        bottom - top);
                    //}

                    graphics.FillRectangle(
                       new SolidBrush(Color.DarkGray),
                       this.Extents.Width - 3,
                       (int)(this.Extents.Height * Math.Min(percentageScrolled, positionOfCurrent)),
                       1,
                       bottom - top);

                    graphics.FillRectangle(new SolidBrush(Color.White), this.Extents.Width - 5, (int)(this.Extents.Height * percentageScrolled), 10, 10);
                    graphics.FillRectangle(new SolidBrush(positionColor), this.Extents.Width - 5, (int)(this.Extents.Height * positionOfCurrent), 10, 10);
                }

                return newImage;
            };
            
            this.Image = () =>
            {
                var list = List();
                if (list != null && !list.Empty)
                {
                    //// The currently playing item has changed.
                    if (this.currentPosition != list.Current.Index)
                    {
                        this.scrollPosition = list.Current.Index;
                        var newPosition = list.Current.Index;

                        if (this.VisibleItems.All(i => i.Item().Index != newPosition))
                        {
                            this.TendVisibleItems();
                        }

                        this.currentPosition = newPosition;
                    }

                    var cacheKey = 
                        string.Join("_", 
                        this.playlistVersion,
                        this.playlist().Current,
                        this.scrollPosition,
                        this.VisibleItems.Count(i => i.Item().HasLoadedMetadata),
                        this.IsPlaying());

                    Debug.WriteLine(cacheKey);
                    return this.bitmapCache.GetBitmap(cacheKey);
                }

                return null;
            };

            if (this.OnItemClicked != null)
            {
                this.OnClick = p =>
                {
                    if (List() != null)
                    {
                        var selectedItem = this.VisibleItems.OrderBy(i => i.Item().Index).ElementAtOrDefault(p.Y / 50);
                        if (selectedItem != null)
                        {
                            if (this.OnItemClicked != null)
                            {
                                this.OnItemClicked(selectedItem.Item());
                            }
                        }
                    }
                };
            }

            this.OnDoubleClick = p =>
            {
                if (List() != null)
                {
                    var selectedItem = this.VisibleItems.OrderBy(i => i.Item().Index).ElementAtOrDefault(p.Y / 50);
                    if (selectedItem != null)
                    {
                        if (this.OnItemDoubleClicked != null)
                        {
                            this.OnItemDoubleClicked(selectedItem.Item());
                        }
                    }
                }
            };

            this.OnScroll = d =>
            {
                Debug.WriteLine("Scrolling");
                var newScrollPosition = this.scrollPosition + (d > 0 ? -1 : 1);
                if (newScrollPosition < List().Count && newScrollPosition >= 0)
                {
                    this.scrollPosition = newScrollPosition;
                }

                this.TendVisibleItems();
              
                return true;
            };

            this.OnKey = (k,s,c) =>
                {
                    switch (k)
                    {
                        case Keys.Enter:
                            var selected = this.VisibleItems.FirstOrDefault(i => i.Item().Index == this.scrollPosition);
                            if (selected != null && this.OnItemDoubleClicked != null)
                            {
                                this.OnItemDoubleClicked(selected.Item());
                            }
                            
                            break;
                        case Keys.Up:
                            this.OnScroll(1);
                            break;
                        case Keys.Down:
                            this.OnScroll(-1);
                            break;
                        case Keys.Home:
                            this.scrollPosition = 0;
                            break;
                        case Keys.End:
                            this.scrollPosition = List().Count - 1;
                            break;
                        case Keys.PageUp:
                            this.scrollPosition = this.scrollPosition < visibleItemCount
                                ? 0
                                : this.scrollPosition - visibleItemCount;
                            break;
                        case Keys.PageDown:
                            this.scrollPosition = this.scrollPosition + visibleItemCount > List().Count - 1
                                ? List().Count - 1
                                : this.scrollPosition + visibleItemCount;
                            break;
                    }

                    this.TendVisibleItems();
                };
        }

        private void RefreshPlaylistIfChanged()
        {
            // Did the playlist change underneath us?
            if (this.playlistVersion != this.playlist().Loaded)
            {
                if (playlist().Empty)
                {
                    this.currentPosition = 0;
                    this.scrollPosition = 0;
                }
                else
                {
                    this.currentPosition = playlist().Current.Index;
                    this.scrollPosition = this.currentPosition;
                }

                this.TendVisibleItems();

                this.playlistVersion = this.playlist().Loaded;
            }
        }

        private void RefreshIfCurrentChanged()
        {
            if (this.scrollPosition != this.playlist().CurrentIndex)
            {
                this.scrollPosition = this.currentPosition;
            }
        }

        private void TendVisibleItems()
        {
            // The number of items it takes to centre the currently playing item.
            var offset = visibleItemCount / 2;

            var playlistItems = List().Items.Where(i => i.Index >= this.scrollPosition - visibleItemCount + offset).Take(visibleItemCount);

            if (playlistItems.Any())
            {
                Debug.WriteLine(playlistItems.First().FullFileName);
            }

            // Should dispose these and cancel pending metadata retreival operations.
            this.VisibleItems.RemoveAll(i => !playlistItems.Contains(i.Item()));

            var visibleItems = playlistItems.Where(i => !this.VisibleItems.Select(j => j.Item()).Contains(i))
                                            .Select(i => new LightPlaylistItem(i, this.Redraw)
                                            {
                                                Extents = new Rectangle
                                                {
                                                    Width = this.Extents.Width,
                                                    Height = this.mode == DrawMode.Playlist_Small ? 25 : 50,
                                                },
                                                Playlist = () => List()
                                            });

            this.VisibleItems.AddRange(visibleItems);
        }              
    }
}