namespace DJPad.Core.Vis
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using DJPad.Core;
    using DJPad.Types;

    public class Sonagram : FftBasedVisualisation
    {
        private readonly ColorPalette defaultColorPalette = new ColorPalette(
            new[] { Color.DarkOrange, Color.LightSkyBlue, Color.SlateGray });
        private Bitmap scrollBuffer;

        protected override void DrawChannel(Graphics g, int width, int height, Sample.Channel channel, int zoom = 4,
            ColorPalette palette = null)
        {
            palette ??= this.defaultColorPalette;
            g.CompositingMode = CompositingMode.SourceCopy;

            var spectrum = this.fftTransform.calculateMagnitude(this.copiedSample.ToFftArray(channel));
            var maximumMagnitude = 0.0f;
            for (var index = 1; index < spectrum.Length; index++)
            {
                maximumMagnitude = Math.Max(maximumMagnitude, spectrum[index]);
            }
            var noiseFloor = maximumMagnitude * 0.003f;

            var cells = Math.Max(1, width / zoom);
            var rowHeight = Math.Min(zoom * 2, height);
            if (this.scrollBuffer == null || this.scrollBuffer.Width != width || this.scrollBuffer.Height != height)
            {
                this.scrollBuffer?.Dispose();
                this.scrollBuffer = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            }
            using (var bufferGraphics = Graphics.FromImage(this.scrollBuffer))
            {
                bufferGraphics.CompositingMode = CompositingMode.SourceCopy;
                bufferGraphics.DrawImageUnscaled(this.privateImage, 0, 0);
            }
            g.DrawImageUnscaled(this.scrollBuffer, 0, rowHeight);
            g.FillRectangle(Brushes.Black, 0, 0, width, rowHeight);

            for (var cell = 0; cell < cells; cell++)
            {
                var firstBin = 1 + (cell * (spectrum.Length - 1) / cells);
                var lastBin = 1 + ((cell + 1) * (spectrum.Length - 1) / cells);
                var magnitude = 0.0f;
                for (var bin = firstBin; bin < lastBin && bin < spectrum.Length; bin++)
                {
                    magnitude = Math.Max(magnitude, spectrum[bin]);
                }

                var intensity = magnitude <= noiseFloor || maximumMagnitude <= noiseFloor
                    ? 0
                    : Math.Pow((magnitude - noiseFloor) / (maximumMagnitude - noiseFloor), 0.4);
                var visibility = intensity == 0 ? 0 : 0.65 + (0.35 * intensity);
                var saturated = palette.Saturated;
                var brightest = palette.Brightest;
                var red = (int)(((saturated.R * (1 - intensity)) + (brightest.R * intensity)) * visibility);
                var green = (int)(((saturated.G * (1 - intensity)) + (brightest.G * intensity)) * visibility);
                var blue = (int)(((saturated.B * (1 - intensity)) + (brightest.B * intensity)) * visibility);

                using var brush = new SolidBrush(Color.FromArgb(255, red, green, blue));
                g.FillRectangle(brush, cell * zoom, 0, zoom, rowHeight);
            }
        }
    }
}
