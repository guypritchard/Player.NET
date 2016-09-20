using System;
using System.Drawing;

namespace DJPad.Core.Utils
{
    using System.Diagnostics;

    public class CachingBitmapProducer<T>
    {
        private string cacheKey;
        private T cachedBitmap;

        public Func<T> BitmapProducer { get; set; }

        public T GetBitmap(string key)
        {
            if (String.CompareOrdinal(key, this.cacheKey) != 0)
            {
                Debug.WriteLine("Redrawing cached image.");
                this.cachedBitmap = this.BitmapProducer();
                this.cacheKey = key;
            }

            return cachedBitmap;
        }
    }
}
