namespace DJPad.Core.Vis
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using DJPad.Types;

    public sealed class LiquidArtwork : ArtworkVisualisationBase
    {
        private float phase;
        private float lowEnergy;
        private float midEnergy;
        private float highEnergy;

        protected override void DrawArtwork(Graphics graphics, Size size, ColorPalette palette, bool playing)
        {
            if (this.Artwork == null)
            {
                return;
            }

            var samples = this.ReadMonoWindow(60);
            this.UpdateEnergy(samples);
            if (playing)
            {
                this.phase += 0.11f + (this.lowEnergy * 0.08f);
            }

            graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            const int stripHeight = 4;
            for (var y = 0; y < size.Height; y += stripHeight)
            {
                var sourceY = y * this.Artwork.Height / (float)size.Height;
                var sourceHeight = Math.Max(1, stripHeight * this.Artwork.Height / (float)size.Height);
                var lowWave = (float)Math.Sin((y * 0.045f) + this.phase) * this.lowEnergy * 24;
                var midWave = (float)Math.Sin((y * 0.12f) - (this.phase * 1.7f)) * this.midEnergy * 12;
                var highWave = (float)Math.Sin((y * 0.28f) + (this.phase * 2.4f)) * this.highEnergy * 4;
                var displacement = lowWave + midWave + highWave;
                graphics.DrawImage(this.Artwork,
                    new RectangleF(displacement - 3, y, size.Width + 6, stripHeight + 1),
                    new RectangleF(0, sourceY, this.Artwork.Width, sourceHeight), GraphicsUnit.Pixel);
            }
        }

        private void UpdateEnergy(float[] samples)
        {
            if (samples.Length == 0)
            {
                this.lowEnergy *= 0.9f;
                this.midEnergy *= 0.9f;
                this.highEnergy *= 0.9f;
                return;
            }

            var low = 0.0f;
            var mid = 0.0f;
            double lowTotal = 0;
            double midTotal = 0;
            double highTotal = 0;
            for (var index = 0; index < samples.Length; index++)
            {
                var value = samples[index];
                low += 0.025f * (value - low);
                mid += 0.18f * (value - mid);
                var lowBand = low;
                var midBand = mid - low;
                var highBand = value - mid;
                lowTotal += lowBand * lowBand;
                midTotal += midBand * midBand;
                highTotal += highBand * highBand;
            }

            this.lowEnergy = this.Smooth(this.lowEnergy, (float)Math.Sqrt(lowTotal / samples.Length) * 5);
            this.midEnergy = this.Smooth(this.midEnergy, (float)Math.Sqrt(midTotal / samples.Length) * 4);
            this.highEnergy = this.Smooth(this.highEnergy, (float)Math.Sqrt(highTotal / samples.Length) * 2);
        }

        private float Smooth(float previous, float current)
        {
            return Math.Min(1, (previous * 0.65f) + (current * 0.35f));
        }
    }
}
