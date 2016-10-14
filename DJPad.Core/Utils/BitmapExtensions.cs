

namespace DJPad.Core.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using System.Drawing;
    using System.IO;
    using DJPad.Types;
    using SharpDX;
    using SharpDX.DXGI;
    using System.Drawing.Imaging;
    using ColorPalette = DJPad.Types.ColorPalette;

    public static class BitmapExtensions
    {
        public static SharpDX.Direct2D1.Bitmap ToD2dBitmap(this Bitmap drawingBitmap, SharpDX.Direct2D1.WindowRenderTarget renderTarget2d)
        {
            SharpDX.Direct2D1.Bitmap result = null;

            //Lock the gdi resource
            BitmapData drawingBitmapData = drawingBitmap.LockBits(
                new Rectangle(0, 0, drawingBitmap.Width, drawingBitmap.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            //Prepare loading the image from gdi resource
            DataStream dataStream = new DataStream(
                drawingBitmapData.Scan0,
                drawingBitmapData.Stride * drawingBitmapData.Height,
                true,
                false);

            SharpDX.Direct2D1.BitmapProperties properties = new SharpDX.Direct2D1.BitmapProperties();
            properties.PixelFormat = new SharpDX.Direct2D1.PixelFormat(
                Format.B8G8R8A8_UNorm,
                SharpDX.Direct2D1.AlphaMode.Premultiplied);

            //Load the image from the gdi resource
            result = new SharpDX.Direct2D1.Bitmap(
                renderTarget2d,
                new Size2(drawingBitmap.Width, drawingBitmap.Height),
                dataStream, drawingBitmapData.Stride,
                properties);

            //Unlock the gdi resource
            drawingBitmap.UnlockBits(drawingBitmapData);

            return result;
        }      

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

        public static Bitmap Resize(this Bitmap bitmap, Size size)
        {
            var resizedImage = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
            using (var graphics = Graphics.FromImage(resizedImage))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.DrawImage(bitmap, 0, 0, size.Height, size.Width);
                return resizedImage;
            }
        }

        public static Icon ToIcon(this Bitmap image)
        {
            IntPtr Hicon = image.GetHicon();// Get an Hicon for myBitmap.
            Icon newIcon = Icon.FromHandle(Hicon);

            return newIcon;
        }

        public static Bitmap CreateImage(Size size, Color backgroundColor)
        {
            var cleanImage = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
            using (var background = Graphics.FromImage(cleanImage))
            {
                background.FillRectangle(new SolidBrush(backgroundColor), new Rectangle(new Point(0, 0), size));
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

        public static byte[] ToByteArray(this Image image)
        {
            if (image == null)
            {
                return null;
            }

            var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Jpeg);
            return ms.ToArray();
        }

        public static bool AverageColor(this Image image)
        {
            var p = image.Palette;

            var smallImage = (Bitmap)image.GetThumbnailImage(32, 32, null, IntPtr.Zero);

            List<Color> colors = new List<Color>();

            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    colors.Add(smallImage.GetPixel(i, j));
                }
            }

            var averageRed = (int)colors.Average(i => i.R);
            var averageBlue = (int)colors.Average(i => i.B);
            var averageGreen = (int)colors.Average(i => i.G);

            var color = Color.FromArgb(255, averageRed, averageGreen, averageBlue);

            var b = color.GetBrightness();
            
            return true;
        }
    }
}
