namespace DJPad.UI.D2D
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Linq;
    using DJPad.Core.Utils;

    public class LightMenu : LightButton, IDisposable
    {
        private SharpDX.Direct2D1.Bitmap cachedImage;

        public enum MenuOrientation
        {
            Horizontal,
            Vertical
        };

        public MenuOrientation Orientation { get; private set; }

        public LightMenu(MenuOrientation orientation = MenuOrientation.Horizontal) : base()
        {
            this.Image = () =>
            {
                if (this.cachedImage == null)
                {
                    var image = new Bitmap(this.Extents.Size.Width, this.Extents.Size.Height, PixelFormat.Format32bppPArgb);
                    using (var g = Graphics.FromImage(image))
                    {
                        g.FillRectangle(new SolidBrush(this.Background()), this.Extents);

                        foreach (var button in this.Children)
                        {
                            var buttonExtent = button.Extents;
                            buttonExtent.Offset(this.Extents.X, this.Extents.Y);
                            g.DrawImage(button.Image(), buttonExtent);
                        }
                    }

                    this.cachedImage = image.ToD2dBitmap(this.Window.Target);
                }

                return this.cachedImage;
            };

            this.OnClick = p =>
            {
                var button = this.Children.FirstOrDefault(b => {
                                                                   var buttonExtent = b.Extents;
                                                                   buttonExtent.Offset(this.Extents.X, this.Extents.Y);
                                                                   return buttonExtent.Contains(p);
                });

                if (button != null && button.OnClick != null)
                {
                    button.OnClick(p);
                    return;
                }
            };
        }

        public IList<GdiPlus.LightButton> Children { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && cachedImage != null)
            {
                cachedImage.Dispose();
                cachedImage = null;
            }
        }
    }
}