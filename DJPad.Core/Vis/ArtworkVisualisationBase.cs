namespace DJPad.Core.Vis
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using DJPad.Core.Interfaces;
    using ColorPalette = DJPad.Types.ColorPalette;

    public interface IArtworkVisualisation
    {
        Bitmap Artwork { get; set; }
    }

    public abstract class ArtworkVisualisationBase : IVisualisation, IArtworkVisualisation
    {
        private Bitmap image;

        public Bitmap Artwork { get; set; }

        public ISampleSource SampleSource { get; set; }

        public Bitmap Draw(Size size, Color backgroundColor, bool playing = true, TimeSpan? duration = null,
            ColorPalette palette = null)
        {
            if (this.image == null || this.image.Size != size)
            {
                this.image?.Dispose();
                this.image = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
            }

            using var graphics = Graphics.FromImage(this.image);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode = CompositingMode.SourceOver;
            this.DrawArtwork(graphics, size, palette, playing);
            return this.image;
        }

        protected abstract void DrawArtwork(Graphics graphics, Size size, ColorPalette palette, bool playing);

        protected float[] ReadMonoWindow(int milliseconds = 50)
        {
            if (this.SampleSource == null)
            {
                return Array.Empty<float>();
            }

            var format = this.SampleSource.GetFormat();
            if (format.BytesPerSample != 2)
            {
                return Array.Empty<float>();
            }

            var sample = this.SampleSource.GetSample(format.SamplesPerSecond * milliseconds / 1000);
            if (sample == null || sample.IsEmpty)
            {
                return Array.Empty<float>();
            }

            var channels = Math.Max(1, format.Channels);
            var frameSize = format.BytesPerSample * channels;
            var frames = sample.DataLength / frameSize;
            var values = new float[frames];
            for (var frame = 0; frame < frames; frame++)
            {
                var offset = frame * frameSize;
                var left = BitConverter.ToInt16(sample.Data, offset);
                var right = channels > 1 ? BitConverter.ToInt16(sample.Data, offset + format.BytesPerSample) : left;
                values[frame] = ((left + right) / 2.0f) / short.MaxValue;
            }
            return values;
        }
    }
}
