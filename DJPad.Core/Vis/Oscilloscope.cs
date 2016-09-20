namespace DJPad.Core.Vis
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Linq;

    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Types;

    public class Oscilloscope : IVisualisation
    {
        #region Fields

        private TimeSpan Progress;
        private TimeSpan Total;
        private Sample sampleCopy;
        private Bitmap privateImage;
        public int samplesDrawn;

        private ColorPalette defaultColorPalette = new ColorPalette(new[] { Color.DarkOrange, Color.LightSkyBlue, Color.SlateGray });

        #endregion

        public ISampleSource SampleSource { get; set; }

        #region Public Methods and Operators

        public FormatInformation GetFormat()
        {
            return this.SampleSource.GetFormat();
        }

        public void Draw(Graphics g, Color backgroundColor, int width, int height, bool playing = true, TimeSpan? duration = null, ColorPalette palette = null)
        {
            const int MinimumSamplesToDraw = 300;
            
            if (duration.HasValue)
            {
                this.Total = duration.Value;
            }

            g.FillRectangle(new SolidBrush(backgroundColor), new Rectangle(0, 0, width, height));

            if (this.SampleSource == null)
            {
                return;
            }

            var currentSample = this.SampleSource.GetSample(this.SampleSource.GetFormat().SamplesPerSecond / 32);
            if (currentSample != null)
            {
                this.sampleCopy = currentSample;
            }
            
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            
            PointF[] leftgraph;
            PointF[] rightgraph;
            PointF[] bothgraph;

            if (this.sampleCopy != null && this.sampleCopy.DataLength > MinimumSamplesToDraw * 2)
            {
                this.Progress = sampleCopy.PresentationTime;

                var br = new BinaryReader(new MemoryStream(this.sampleCopy.Data));
                br.BaseStream.Position = this.samplesDrawn;

                leftgraph = new PointF[MinimumSamplesToDraw];
                rightgraph = new PointF[MinimumSamplesToDraw];
                bothgraph = new PointF[MinimumSamplesToDraw];

                for (int i = 0; i < MinimumSamplesToDraw; i++)
                {
                    try
                    {
                        short leftSample = br.ReadInt16();
                        short rightSample = br.ReadInt16();

                        leftgraph[i].X = (i * width) / MinimumSamplesToDraw;
                        leftgraph[i].Y = this.ScaleSample(leftSample, height, width);

                        rightgraph[i].X = leftgraph[i].X;
                        rightgraph[i].Y = this.ScaleSample(rightSample, height, width);

                        bothgraph[i].X = leftgraph[i].X;
                        bothgraph[i].Y = this.ScaleSample((short)((rightSample + leftSample) / 4), height, width);

                        // We already read 4 bytes at this point we just need to skip ahead to the next point to read a sample.
                        int samplesToSkip = (this.sampleCopy.DataLength / (MinimumSamplesToDraw * 2)) - 4;

                        // Make sure we're always on an even number boundary to be sure we read our left/right samples correctly.
                        samplesToSkip = (samplesToSkip % 2) == 0 ? samplesToSkip : samplesToSkip - 1;

                        br.BaseStream.Position += samplesToSkip > 0 ? samplesToSkip : 0;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }
            }
            else
            {
                leftgraph = new[] { new PointF { Y = height / 2, X = 0 }, new PointF { Y = height / 2, X = width } };
                rightgraph = leftgraph;
                bothgraph = leftgraph;
            }

            double percentage = this.Progress.TotalMilliseconds / this.Total.TotalMilliseconds;
            var completeGraphLength = (int)(bothgraph.Length * percentage);

            try
            {
                g.DrawLines(new Pen(palette.Saturated), leftgraph);
                g.DrawLines(new Pen(palette.Saturated), rightgraph);
                g.DrawLines(new Pen(Color.FromArgb(200, palette.Brightest), 4.0f), bothgraph.Take(completeGraphLength >= 2 ? completeGraphLength : 2).ToArray());
                g.DrawLines(new Pen(Color.FromArgb(200, palette.Darkest), 4.0f), bothgraph.Skip(completeGraphLength >= 2 ? completeGraphLength : 0).ToArray());
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public Bitmap Draw(Size size, Color backgroundColor, bool playing = true, TimeSpan? duration = null, ColorPalette palette = null)
        {
            if (this.privateImage == null || this.privateImage.Height != size.Height || this.privateImage.Width != size.Width || backgroundColor == Color.Transparent)
            {
                this.privateImage = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            }

            this.Draw(Graphics.FromImage(this.privateImage), backgroundColor, size.Width, size.Height, playing, duration, palette);

            return this.privateImage;
        }

        public void SetMetadata(IMetadata metadata)
        {
            
        }

        #endregion

        #region Methods

        private float ScaleSample(short sample, int height, int width)
        {
            return ((sample * height * 0.4f) / short.MaxValue) + (height / 2);
        }

        #endregion
    }
}