using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DJPad.UI.Direct2DControls
{
    using System.Drawing;
    using System.Drawing.Imaging;
    using Vortice.Direct2D1;
    using Vortice.DXGI;
    using Vortice.Mathematics;

    public static class D2DConverter
    {
        public static ID2D1Bitmap ToD2DBitmap(this System.Drawing.Bitmap drawingBitmap, ID2D1HwndRenderTarget renderTarget)
        {
            ID2D1Bitmap result = null;

            //Lock the gdi resource
            BitmapData drawingBitmapData = drawingBitmap.LockBits(
                new Rectangle(0, 0, drawingBitmap.Width, drawingBitmap.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            BitmapProperties properties = new BitmapProperties();
            properties.PixelFormat = new Vortice.DCommon.PixelFormat(
                Format.B8G8R8A8_UNorm,
                Vortice.DCommon.AlphaMode.Premultiplied);

            //Load the image from the gdi resource
            result = renderTarget.CreateBitmap(
                new SizeI(drawingBitmap.Width, drawingBitmap.Height),
                drawingBitmapData.Scan0,
                (uint)drawingBitmapData.Stride,
                properties);

            //Unlock the gdi resource
            drawingBitmap.UnlockBits(drawingBitmapData);

            return result;
        } 
    }
}
