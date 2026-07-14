namespace DJPad.UI.D2D
{
    using System;
    using System.Drawing;
    using DJPad.Core.Utils;
    using Vortice.Direct2D1;

    public class ImageCachingLightControl : LightControl<ID2D1Bitmap>
    {
        private readonly CachingBitmapProducer<ID2D1Bitmap> bitmapCache = new CachingBitmapProducer<ID2D1Bitmap>();

        public Func<string> CacheKey { get; set; }

        public virtual Func<LightControl<ID2D1Bitmap>, ID2D1Bitmap> SourceImage { get; set; }

        public ImageCachingLightControl()
        {
            this.bitmapCache.BitmapProducer = () => this.SourceImage(this);
            this.Image = () => this.bitmapCache.GetBitmap(this.CacheKey == null ? string.Empty : this.CacheKey());
        }
    }
}
