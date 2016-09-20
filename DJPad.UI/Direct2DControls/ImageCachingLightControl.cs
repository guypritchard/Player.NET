namespace DJPad.UI.D2D
{
    using System;
    using System.Drawing;
    using DJPad.Core.Utils;

    public class ImageCachingLightControl : LightControl<SharpDX.Direct2D1.Bitmap>
    {
        private readonly CachingBitmapProducer<SharpDX.Direct2D1.Bitmap> bitmapCache = new CachingBitmapProducer<SharpDX.Direct2D1.Bitmap>();

        public Func<string> CacheKey { get; set; }

        public virtual Func<LightControl<SharpDX.Direct2D1.Bitmap>, SharpDX.Direct2D1.Bitmap> SourceImage { get; set; }

        public ImageCachingLightControl()
        {
            this.bitmapCache.BitmapProducer = () => this.SourceImage(this);
            this.Image = () => this.bitmapCache.GetBitmap(this.CacheKey == null ? string.Empty : this.CacheKey());
        }
    }
}