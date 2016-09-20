namespace DJPad.Core.Vis
{
    using System.Drawing.Imaging;
    using DJPad.Core.Interfaces;
    using System;
    using System.Drawing;
    using ColorPalette = DJPad.Types.ColorPalette;

    public abstract class BaseVisualisation : IVisualisation
    {
        protected Bitmap privateImage;
        
        public ISampleSource SampleSource { get; set; }
        
        public abstract void Draw(Graphics g, Color backgroundColor, int width, int height, ColorPalette palette, TimeSpan? duration = null, bool playing = true);

        public Bitmap Draw(Size size, Color backgroundColor, bool playing = true, TimeSpan? duration = null, ColorPalette palette = null)
        {
            if (this.privateImage == null || this.privateImage.Height != size.Height || this.privateImage.Width != size.Width || backgroundColor == Color.Transparent)
            {
                this.privateImage = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
            }

            this.Draw(Graphics.FromImage(this.privateImage), backgroundColor, size.Width, size.Height, palette, duration, playing);

            return this.privateImage;
        }
    }
}
