

namespace DJPad.Core.Utils
{
    using System;
    using System.Drawing;
    using ColorPalette = DJPad.Types.ColorPalette;

    public static class BitmapExtensions
    {
        public static Bitmap Overlay(this Bitmap bitmap, Bitmap imageToOverlay, Rectangle? position = null)
        {
            if (bitmap == null)
            {
                return imageToOverlay;
            }

            if (position == null)
            {
                position = new Rectangle(new Point(0, 0), new Size(imageToOverlay.Width, imageToOverlay.Height));
            }

            var cleanImage = new Bitmap(bitmap);
            using (var background = Graphics.FromImage(cleanImage))
            {
                background.DrawImage(imageToOverlay, position.Value);
                return cleanImage;
            }
        }

        public static ColorPalette GetPalette(this Bitmap bitmap)
        {
            if (bitmap == null)
            {
                return null;
            }

            return PaletteExtractor.ExtractPalette(bitmap);
        }

    }
}
