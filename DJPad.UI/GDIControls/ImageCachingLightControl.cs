namespace DJPad.UI.GdiPlus
{
    using System;
    using System.Drawing;
    using DJPad.Core.Utils;

    public class ImageCachingLightControl : LightControl<Bitmap>
    {
        private readonly CachingBitmapProducer<Bitmap> bitmapCache = new CachingBitmapProducer<Bitmap>();

        public Func<string> CacheKey { get; set; }

        public virtual Func<LightControl<Bitmap>, Bitmap> SourceImage { get; set; }

        public ImageCachingLightControl()
        {
            this.bitmapCache.BitmapProducer = () => this.SourceImage(this).Resize(this.Extents.Size);
            this.Image = () => this.bitmapCache.GetBitmap(this.CacheKey == null ? string.Empty : this.CacheKey());
        }
    }
}