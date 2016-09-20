

namespace DJPad.Core.Utils
{
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Threading;

    public class WaveformImageProducer
    {
        private short[] samples;
        private readonly IFileSource source;
        private const int MinSamples = 1000;

        public bool Complete { get; private set; }

        public WaveformImageProducer(string filename)
        {
            var loadSource = SourceRegistry.ResolveSource(filename, true);
            loadSource.Load(filename);
            this.source = loadSource;
            this.Init();
        }

        public Bitmap DrawWaveform(int width, int height, double completedPercentage)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.FillRectangle(Brushes.Transparent, new Rectangle(new Point(), new Size(width, height)));
                
                var fracPart = this.samples.Length / (this.samples.Length % width);

                if (this.samples != null && this.samples.Length > 0)
                {
                    lock (this.samples)
                    {
                        var p = new Pen(Brushes.SlateGray);
                        float magnitude = (float)height / (float)short.MaxValue;
                        var samplesTaken = 0;
                        for (int i = 0; i < width; i++)
                        {
                            if (i % fracPart == 0)
                            {
                                samplesTaken++;
                            }

                            var length = magnitude * (Math.Abs((int)samples[samplesTaken++]) * 0.9f);
                            graphics.DrawLine(p, new Point(i, (height / 2) + (int)length), new Point(i, (height / 2) - (int)length));
                        }
                    }
                }

                graphics.DrawLine(new Pen(Brushes.DarkOrange, 5f), new Point((int)(completedPercentage * width), 0), new Point((int)(completedPercentage * width), height));

                return bitmap;
            }
        }

        public void Cancel()
        {
            this.Complete = true;
        }

        private void Init()
        {
            this.samples = new short[MinSamples];
            this.Complete = false;
            ThreadPool.QueueUserWorkItem((b) => this.ProduceWaveformImage());
        }

        private void ProduceWaveformImage()
        {
            const int seconds = 5;

            var totalNumberOfSamples = source.GetMetadata().Duration.TotalSeconds * source.GetFormat().ToWaveFormat().SamplesPerSec;
            var chunk = source.GetFormat().ToWaveFormat().SamplesPerSec * seconds;
            var skip = (int) totalNumberOfSamples/MinSamples;
            int samplesTaken = 0;

            if (!source.GetMetadata().Duration.TotalSeconds.Equals(0.0d))
            {
                while (samplesTaken < samples.Length)
                {
                    var sample = source.GetSample(chunk);

                    if (sample.IsEmpty || this.Complete)
                    {
                        break;
                    }

                    lock (this.samples)
                    {
                        var localSamples = sample.DataLength / skip;

                        for (int i = 0; i < localSamples; i++)
                        {
                            // We could overflow - occasionally total length values can be approximate.
                            if (samplesTaken < samples.Length)
                            {
                                samples[samplesTaken++] = BitConverter.ToInt16(sample.Data, i * skip);
                            }
                        }
                    }
                }
            }

            this.Complete = true;
        }
    }
}
