using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingImageLockMode = System.Drawing.Imaging.ImageLockMode;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace DJPad.Experimental;

internal sealed class BitmapBuffer : IDisposable
{
    private WriteableBitmap? image;
    private byte[] row = Array.Empty<byte>();

    public WriteableBitmap Update(DrawingBitmap source)
    {
        if (image == null || image.PixelSize.Width != source.Width || image.PixelSize.Height != source.Height)
        {
            image?.Dispose();
            image = new WriteableBitmap(new PixelSize(source.Width, source.Height), new Vector(96, 96),
                PixelFormat.Bgra8888, AlphaFormat.Premul);
            row = new byte[source.Width * 4];
        }

        var data = source.LockBits(new System.Drawing.Rectangle(0, 0, source.Width, source.Height),
            DrawingImageLockMode.ReadOnly, DrawingPixelFormat.Format32bppPArgb);
        try
        {
            using var target = image.Lock();
            var sourceStride = Math.Abs(data.Stride);
            for (var y = 0; y < source.Height; y++)
            {
                var sourceY = data.Stride >= 0 ? y : source.Height - y - 1;
                Marshal.Copy(IntPtr.Add(data.Scan0, sourceY * sourceStride), row, 0, row.Length);
                Marshal.Copy(row, 0, IntPtr.Add(target.Address, y * target.RowBytes), row.Length);
            }
        }
        finally
        {
            source.UnlockBits(data);
        }

        return image;
    }

    public void Dispose()
    {
        image?.Dispose();
        image = null;
    }
}
