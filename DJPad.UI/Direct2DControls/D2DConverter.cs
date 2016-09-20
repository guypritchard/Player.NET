using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DJPad.UI.Direct2DControls
{
    using System.Drawing;
    using System.Drawing.Imaging;
    using SharpDX;
    using SharpDX.Direct2D1;
    using SharpDX.DXGI;

    public static class D2DConverter
    {
        public static SharpDX.Direct2D1.Bitmap ToD2DBitmap(this System.Drawing.Bitmap drawingBitmap, WindowRenderTarget renderTarget)
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
                renderTarget,
                new Size2(drawingBitmap.Width, drawingBitmap.Height),
                dataStream, drawingBitmapData.Stride,
                properties);

            //Unlock the gdi resource
            drawingBitmap.UnlockBits(drawingBitmapData);

            return result;
        } 
    }
}
