using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DJPad.Core.Utils
{
    using DJPad.Types;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;

    public class PaletteExtractor
    {
        public static DJPad.Types.ColorPalette ExtractPalette(Bitmap bitmap)
        {
            var resized = ResizeImage(bitmap, 8, 8);
            
            BitmapData bd = resized.LockBits(new Rectangle(0, 0, resized.Width, resized.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var arr = new int[bd.Width * bd.Height - 1];
            Marshal.Copy(bd.Scan0, arr, 0, arr.Length);
            resized.UnlockBits(bd);

            return new DJPad.Types.ColorPalette(arr.Distinct().Select(p =>
            {
                var c = Color.FromArgb(p);
                return Color.FromArgb(0xFF, c.R, c.G, c.B);
            }));
        }

        /// <summary>
        /// </summary>
        /// Resize the image to the specified width and height.
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height, PixelFormat.Format32bppPArgb);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.Low;
                graphics.SmoothingMode = SmoothingMode.None;
                graphics.PixelOffsetMode = PixelOffsetMode.None;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
