namespace DJPad.Core.Vis
{
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Timers;
    using DJPad.Core;
    using DJPad.Lib.FFT;
    using DJPad.Core.Interfaces;
    using DJPad.Types;
    using System.Drawing.Imaging;
    using System.Drawing.Drawing2D;

    public abstract class FftBasedVisualisation : IVisualisation
    {
        protected Timer drawTimer;

        protected IFFT fftTransform;

        private Bitmap privateImage;

        public event Action Redraw;

        private readonly int[] FPSdata = Enumerable.Repeat(0, 20).ToArray();
        private long drawCount;

        protected Sample copiedSample;

        protected abstract void DrawChannel(Graphics g, int width, int height, Sample.Channel channel, int zoom = 1, DJPad.Types.ColorPalette palette = null);

        protected FftBasedVisualisation(int redrawInterval, bool displayFps = false)
        {
            this.fftTransform = new KJFFT(2048);

            if (redrawInterval > 0)
            {
                drawTimer = new Timer(redrawInterval);
                drawTimer.Elapsed += (o, e) =>
                {
                    if (this.Redraw != null)
                    {
                        this.Redraw();
                    }
                };

                drawTimer.Start();
            }
        }

        public Bitmap Draw(Size size, Color backgroundColor, bool playing = true, TimeSpan? duration = null, DJPad.Types.ColorPalette palette = null)
        {
            if (this.privateImage == null || this.privateImage.Height != size.Height || this.privateImage.Width != size.Width || backgroundColor != Color.Transparent)
            {
                this.privateImage = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
            }

            if (this.SampleSource != null)
            {
                var graphics = Graphics.FromImage(this.privateImage);
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                this.Draw(graphics, backgroundColor, size.Width, size.Height, playing, duration, palette);
            }

            return this.privateImage;
        }

        public void Draw(Graphics g, Color background, int width, int height, bool playing = true, TimeSpan? duration = null, DJPad.Types.ColorPalette palette = null)
        {
            var currentSample = this.SampleSource.GetSample(this.SampleSource.GetFormat().SamplesPerSecond / 5);
            if (currentSample != null)
            {
                this.copiedSample = currentSample;
            }

            if (this.copiedSample != null && playing)
            {
                DrawChannel(g, width, height, Sample.Channel.Both, 1, palette);
            }
        }
        
        public ISampleSource SampleSource { get; set; }
    }
}
