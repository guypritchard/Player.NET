namespace DJPad.Core.Vis
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.IO;
    using System.Linq;

    using System.Collections.Generic;
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
        public int samplesDrawn;

        private Cache<double> CosCache = new Cache<double>(Math.Cos);
        private Cache<double> SinCache = new Cache<double>(Math.Sin);

        private ColorPalette defaultColorPalette = new ColorPalette(new[] { Color.DarkOrange, Color.LightSkyBlue, Color.SlateGray });

        public ISampleSource SampleSource { get; set; }

        #region Public Methods and Operators

        public FormatInformation GetFormat()
        {
            return this.SampleSource.GetFormat();
        }

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

            g.FillRectangle(new SolidBrush(backgroundColor), new Rectangle(0, 0, width, height));

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
                leftgraph = new[] { new PointF { Y = height / 2.0f, X = 0 }, new PointF { Y = height / 2.0f, X = width } };
                rightgraph = leftgraph;
                bothgraph = leftgraph;
            }

            double percentage = this.Progress.TotalMilliseconds / this.Total.TotalMilliseconds;
            var completeGraphLength = (int)(bothgraph.Length * percentage);

            try
            {
                var circleB = new List<PointF>();
                var circleL = new List<PointF>();
                var circleR = new List<PointF>();

                int j = 0;
                const float scale = 0.7f;
                float diameter = width/2.0f;

                // This looks weird, but its the only way I could get the countdown to appear clockwise.
                for (double i = 3 * Math.PI; i > Math.PI; i -= 0.021)
                {
                    if (leftgraph.Length <= 2 && rightgraph.Length <= 2)
                    {
                        circleL.Add(new PointF(diameter + (float)SinCache[i], diameter + (float)CosCache[i]));
                        circleR.Add(new PointF(diameter + (float)SinCache[i], diameter + (float)CosCache[i]));
                        circleB.Add(new PointF(diameter + (float)SinCache[i], diameter + (float)CosCache[i]));
                    }
                    else
                    {
                        // This looks weird, this is to ensure the circles appear within the window. 
                        circleL.Add(new PointF(diameter + (float)SinCache[i] * leftgraph[j].Y * scale,
                                               diameter + (float)CosCache[i] * leftgraph[j].Y * scale));
                        circleR.Add(new PointF(diameter + (float)SinCache[i] * rightgraph[j].Y * scale,
                                               diameter + (float)CosCache[i] * rightgraph[j].Y * scale));
                        circleB.Add(new PointF(diameter + (float)SinCache[i] * bothgraph[j].Y * scale,
                                               diameter + (float)CosCache[i] * bothgraph[j].Y * scale));
                        j++;
                    }
                }

                g.DrawLines(new Pen(palette.Saturated.MakeTransparent(0.2f)), circleL.ToArray());
                g.DrawLines(new Pen(palette.Saturated.MakeTransparent(0.2f)), circleR.ToArray());

                // Countdown
                g.DrawLines(new Pen(palette.Brightest.MakeTransparent(0.2f), 8.0f), circleB.Take(completeGraphLength >= 2 ? completeGraphLength : 2).ToArray());

                if (circleB.Skip(completeGraphLength >= 2 ? completeGraphLength : 0).Count() > 2)
                {
                    g.DrawLines(new Pen(palette.Darkest.MakeTransparent(0.2f), 8.0f), circleB.Skip(completeGraphLength >= 2 ? completeGraphLength : 0).ToArray());
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        //public Bitmap Draw(Size size, Color backgroundColor, bool playing = true, TimeSpan? duration = null, ColorPalette palette = null)
        //{
        //    if (this.privateImage == null || this.privateImage.Height != size.Height || this.privateImage.Width != size.Width)
        //    {
        //        this.privateImage = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
        //    }

        //    this.Draw(Graphics.FromImage(this.privateImage), backgroundColor, size.Width, size.Height, playing, duration, palette);

        //    return this.privateImage;
        //}

        public Bitmap Draw(Size size, Color backgroundColor, bool playing = true, TimeSpan? duration = null, ColorPalette palette = null)
        {
            if (this.privateImage == null || this.privateImage.Height != size.Height || this.privateImage.Width != size.Width)
            {
                this.privateImage = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            }

            var shrink = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(shrink))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low; // or NearestNeighbour
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

                //ColorMatrix cm = new ColorMatrix();
                //cm.Matrix33 = 0.85f;
                //ImageAttributes ia = new ImageAttributes();
                //ia.SetColorMatrix(cm);

                g.CompositingMode = CompositingMode.SourceOver;
                
                g.DrawImage(this.privateImage, new Rectangle(10 , 10, size.Width-20, size.Height-20), 0, 0, shrink.Height, shrink.Width, GraphicsUnit.Pixel);
                
                this.Draw(
                    Graphics.FromImage(this.privateImage), 
                    backgroundColor, 
                    size.Width, 
                    size.Height, 
                    playing,
                    duration, 
                    palette);

                g.DrawImageUnscaled(this.privateImage, 0, 0);
            }

            Graphics.FromImage(this.privateImage).DrawImage(shrink, 0, 0);

            return this.privateImage;
        }

        #endregion

        #region Methods

        private float ScaleSample(short sample, int height, int width)
        {
            return ((sample * height * 0.4f) / short.MaxValue) + (height / 2);
        }

        private List<PointF> Circle(int size)
        {
            var circle = new List<PointF>();
            for (double i = 0; i < 2 * Math.PI; i += 0.021)
            {
                circle.Add(new PointF(size + (float)Math.Sin(i) * size, (float)size + (float)Math.Cos(i) * size));
            }

            circle.Add(circle.First());

            return circle;
        }

        #endregion
    }
}
