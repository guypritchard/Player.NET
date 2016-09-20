namespace DJPad.Core.Vis
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Text;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Lib.FFT;
    using DJPad.Types;

    public class FFTGraph : IVisualisation
    {
        private const int DEFAULT_SPECTRUM_ANALYSER_BAND_COUNT = 60;
        private const float DEFAULT_SPECTRUM_ANALYSER_DECAY = 0.05F;
        private const int DEFAULT_SPECTRUM_ANALYSER_PEAK_DELAY = 20;

        private readonly IFFT fft;

        private Sample copiedSample;

        private Bitmap privateImage;

        public FFTGraph()
        {
            this.fft = new KJFFT(1024);
        }

        public ISampleSource SampleSource { get; set; }

        public Sample GetSample(int dataRequested)
        {
            Sample s = this.SampleSource.GetSample(dataRequested);
            this.copiedSample = s.Clone();
            return s;
        }

        public FormatInformation GetFormat()
        {
            return this.SampleSource.GetFormat();
        }

        public void Draw(Graphics g, Color background, int width, int height, bool playing = true, TimeSpan? duration = null, ColorPalette palette = null)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            //g.FillRectangle(new SolidBrush(background), new Rectangle(0, 0, width, height));

            var currentSample = this.SampleSource.GetSample(this.SampleSource.GetFormat().SamplesPerSecond/16);
            if (currentSample != null)
            {
                this.copiedSample = currentSample;
            }

            if (this.copiedSample != null)
            {
                DrawChannel(g, width, height, Sample.Channel.Both);
                // DrawChannel(g, height, Sample.Channel.Right);
            }
        }

        //http://stackoverflow.com/questions/20408388/how-to-filter-fft-data-for-audio-visualisation
        private void DrawChannel(Graphics g, int width, int height, Sample.Channel channel)
        {
            float[] spect = this.fft.calculateMagnitude(this.copiedSample.ToFftArray(channel));
            var points = new PointF[spect.Length];

            points[0].Y = height;
            points[1].Y = height;

            for (int i = 2; i < points.Length; i++)
            {
                points[i].X = i;
                points[i].Y = height - (spect[i] / 20);
            }

            var widthScale = ((float)width)/(float)(points.Length/2);

            g.ScaleTransform(widthScale, 0.9f);

            g.DrawLines(new Pen(Brushes.SlateGray), points);
        }

        public Bitmap Draw(Size size, Color backgroundColor, bool playing = true, TimeSpan? duration = null, ColorPalette palette = null)
        {
            if (this.privateImage == null || this.privateImage.Height != size.Height || this.privateImage.Width != size.Width || backgroundColor == Color.Transparent)
            {
                this.privateImage = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            }

            this.Draw(Graphics.FromImage(this.privateImage), backgroundColor, size.Width, size.Height, playing, duration);

            return this.privateImage;
        }

        public void SetMetadata(IMetadata metadata)
        {
        }
    }
}