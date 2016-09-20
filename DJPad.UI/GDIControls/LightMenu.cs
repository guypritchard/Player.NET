namespace DJPad.UI.GdiPlus
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Linq;

    public class LightMenu : LightButton, IDisposable
    {
        private Bitmap cachedImage;

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
                    this.cachedImage = new Bitmap(this.Extents.Size.Width, this.Extents.Size.Height, PixelFormat.Format32bppPArgb);
                    using (var g = Graphics.FromImage(this.cachedImage))
                    {
                        g.FillRectangle(new SolidBrush(this.Background()), this.Extents);

                        foreach (var button in this.Children)
                        {
                            var buttonExtent = button.Extents;
                            buttonExtent.Offset(this.Extents.X, this.Extents.Y);
                            g.DrawImage(button.Image(), buttonExtent);
                        }
                    }
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

        public IList<LightButton> Children { get; set; }

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