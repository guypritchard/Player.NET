namespace DJPad.Core.Vis
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Core.Utils;
    using DJPad.Types;

    public class CircularOscilloscope : IVisualisation
    {
        private TimeSpan Progress;
        private TimeSpan Total;
        private Sample sampleCopy;
        private Bitmap privateImage;
        private ColorPalette defaultColorPalette = new ColorPalette(new[] { Color.DarkOrange, Color.LightSkyBlue, Color.SlateGray });

        public ISampleSource SampleSource { get; set; }

        #region Public Methods and Operators

        public void Draw(Graphics g, Color backgroundColor, int width, int height, bool playing = true, TimeSpan? duration = null, ColorPalette palette = null)
        {
            if (palette == null)
            {
                palette = defaultColorPalette;
            }

            g.CompositingMode = CompositingMode.SourceCopy;

            const int MinimumSamplesToDraw = 300;

            if (duration.HasValue)
            {
                this.Total = duration.Value;
            }

            using (var background = new SolidBrush(backgroundColor))
            {
                g.FillRectangle(background, new Rectangle(0, 0, width, height));
            }

            if (this.SampleSource == null)
            {
                return;
            }

            var currentSample = this.SampleSource.GetSample(this.SampleSource.GetFormat().SamplesPerSecond / 16);
            if (currentSample != null)
            {
                this.sampleCopy = currentSample;
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var lowBand = new float[MinimumSamplesToDraw];
            var midBand = new float[MinimumSamplesToDraw];
            var highBand = new float[MinimumSamplesToDraw];
            if (this.sampleCopy != null && this.sampleCopy.DataLength > MinimumSamplesToDraw * 2)
            {
                this.Progress = sampleCopy.PresentationTime;
                var format = this.sampleCopy.Format ?? this.SampleSource.GetFormat();
                this.SplitFrequencyBands(this.sampleCopy, format, lowBand, midBand, highBand);
            }

            var percentage = this.Total == TimeSpan.Zero ? 0 : this.Progress.TotalMilliseconds / this.Total.TotalMilliseconds;
            var completeGraphLength = Math.Clamp((int)(MinimumSamplesToDraw * percentage), 2, MinimumSamplesToDraw);

            try
            {
                var lowWave = new PointF[MinimumSamplesToDraw];
                var midWave = new PointF[MinimumSamplesToDraw];
                var highWave = new PointF[MinimumSamplesToDraw];
                var size = Math.Min(width, height);
                var centerX = width / 2.0f;
                var centerY = height / 2.0f;

                for (var point = 0; point < MinimumSamplesToDraw; point++)
                {
                    var angle = (3 * Math.PI) - (point * 2 * Math.PI / (MinimumSamplesToDraw - 1));
                    lowWave[point] = this.RadialPoint(centerX, centerY, angle,
                        (size * 0.29f) + (lowBand[point] * size * 0.16f));
                    midWave[point] = this.RadialPoint(centerX, centerY, angle,
                        (size * 0.36f) + (midBand[point] * size * 0.10f));
                    highWave[point] = this.RadialPoint(centerX, centerY, angle,
                        (size * 0.43f) + (highBand[point] * size * 0.025f));
                }

                using var lowPen = new Pen(palette.Darkest.MakeTransparent(0.85f), 2);
                using var midPen = new Pen(palette.Saturated.MakeTransparent(0.9f), 2);
                using var playedPen = new Pen(palette.Brightest.MakeTransparent(0.8f), 5);
                using var remainingPen = new Pen(palette.Saturated.MakeTransparent(0.75f), 5);
                g.DrawLines(lowPen, lowWave);
                g.DrawLines(midPen, midWave);
                g.DrawLines(playedPen, highWave.Take(completeGraphLength).ToArray());
                g.DrawLines(remainingPen, highWave.Skip(completeGraphLength - 1).ToArray());
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public Bitmap Draw(Size size, Color backgroundColor, bool playing = true, TimeSpan? duration = null, ColorPalette palette = null)
        {
            if (this.privateImage == null || this.privateImage.Height != size.Height || this.privateImage.Width != size.Width)
            {
                this.privateImage = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            }

            using var graphics = Graphics.FromImage(this.privateImage);
            this.Draw(graphics, backgroundColor, size.Width, size.Height, playing, duration, palette);

            return this.privateImage;
        }

        #endregion

        #region Methods

        private void SplitFrequencyBands(Sample sample, FormatInformation format, float[] low, float[] mid, float[] high)
        {
            var channels = Math.Max(1, format.Channels);
            var frameSize = format.BytesPerSample * channels;
            var frameCount = sample.DataLength / frameSize;
            var lowPassAlpha = 1.0f - (float)Math.Exp(-2 * Math.PI * 250 / format.SampleRate);
            var midPassAlpha = 1.0f - (float)Math.Exp(-2 * Math.PI * 2000 / format.SampleRate);
            var lowPass = 0.0f;
            var midPass = 0.0f;
            var lowFrames = new float[frameCount];
            var midFrames = new float[frameCount];
            var highFrames = new float[frameCount];

            for (var frame = 0; frame < frameCount; frame++)
            {
                var offset = frame * frameSize;
                var left = BitConverter.ToInt16(sample.Data, offset);
                var right = channels > 1 ? BitConverter.ToInt16(sample.Data, offset + format.BytesPerSample) : left;
                var value = ((left + right) / 2.0f) / short.MaxValue;
                lowPass += lowPassAlpha * (value - lowPass);
                midPass += midPassAlpha * (value - midPass);
                lowFrames[frame] = lowPass;
                midFrames[frame] = midPass - lowPass;
                highFrames[frame] = value - midPass;
            }

            for (var point = 0; point < low.Length; point++)
            {
                var firstFrame = point * frameCount / low.Length;
                var lastFrame = Math.Max(firstFrame + 1, (point + 1) * frameCount / low.Length);
                var lowTotal = 0.0f;
                var midTotal = 0.0f;
                var highTotal = 0.0f;
                for (var frame = firstFrame; frame < lastFrame; frame++)
                {
                    lowTotal += lowFrames[frame];
                    midTotal += midFrames[frame];
                    highTotal += highFrames[frame];
                }

                var count = lastFrame - firstFrame;
                low[point] = Math.Clamp(lowTotal / count * 3.0f, -1, 1);
                mid[point] = Math.Clamp(midTotal / count * 2.0f, -1, 1);
                high[point] = Math.Clamp(highTotal / count * 0.8f, -1, 1);
            }
        }

        private PointF RadialPoint(float centerX, float centerY, double angle, float radius)
        {
            return new PointF(centerX + ((float)Math.Sin(angle) * radius), centerY + ((float)Math.Cos(angle) * radius));
        }

        #endregion
    }
}
