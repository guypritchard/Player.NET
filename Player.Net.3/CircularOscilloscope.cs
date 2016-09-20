namespace Player.Net._2.UserInterface.Vis
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using System.Timers;
    using System.Collections.Generic;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Core.Utils;
    using DJPad.Types;
    using SharpDX.Direct2D1;
    using SharpDX.Mathematics.Interop;
    using System.Drawing;

    public class CircularOscilloscope
    {
        private TimeSpan Progress;
        private TimeSpan Total;
        private Sample sampleCopy;
        public event Action Redraw;
        public int samplesDrawn;
        private Timer drawTimer;

        private Cache<double> CosCache = new Cache<double>(Math.Cos);
        private Cache<double> SinCache = new Cache<double>(Math.Sin);

        private ColorPalette defaultColorPalette = new ColorPalette(new[] { Color.DarkOrange, Color.LightSkyBlue, Color.SlateGray });

        public CircularOscilloscope()
        {
            drawTimer = new Timer(30);
            drawTimer.Elapsed += (o, e) =>
            {
                if (this.Redraw != null)
                {
                    this.Redraw();
                }
            };

            drawTimer.Start();
        }

        public ISampleSource SampleSource { get; set; }

        #region Public Methods and Operators

        public FormatInformation GetFormat()
        {
            return this.SampleSource.GetFormat();
        }

        public void Draw(WindowRenderTarget target, Color backgroundColor, int width, int height, bool playing = true, TimeSpan? duration = null, ColorPalette palette = null)
        {
            if (palette == null)
            {
                palette = defaultColorPalette;
            }

            const int MinimumSamplesToDraw = 300;

            if (duration.HasValue)
            {
                this.Total = duration.Value;
            }

            target.FillRectangle(new RawRectangleF(0, 0, width, height), new SolidColorBrush(target, backgroundColor.ToRawColor4()));

            if (this.SampleSource == null)
            {
                return;
            }

            var currentSample = this.SampleSource.GetSample(this.SampleSource.GetFormat().SamplesPerSecond / 16);
            if (currentSample != null)
            {
                this.sampleCopy = currentSample;
            }

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
                var circleB = new List<RawVector2>();
                var circleL = new List<RawVector2>();
                var circleR = new List<RawVector2>();

                int j = 0;
                const float scale = 0.7f;
                float diameter = width/2.0f;

                // This looks weird, but its the only way I could get the countdown to appear clockwise.
                for (double i = 3 * Math.PI; i > Math.PI; i -= 0.021)
                {
                    if (leftgraph.Length <= 2 && rightgraph.Length <= 2)
                    {
                        circleL.Add(new RawVector2(diameter + (float)SinCache[i], diameter + (float)CosCache[i]));
                        circleR.Add(new RawVector2(diameter + (float)SinCache[i], diameter + (float)CosCache[i]));
                        circleB.Add(new RawVector2(diameter + (float)SinCache[i], diameter + (float)CosCache[i]));
                    }
                    else
                    {
                        // This looks weird, this is to ensure the circles appear within the window. 
                        circleL.Add(new RawVector2(diameter + (float)SinCache[i] * leftgraph[j].Y * scale,
                                                   diameter + (float)CosCache[i] * leftgraph[j].Y * scale));
                        circleR.Add(new RawVector2(diameter + (float)SinCache[i] * rightgraph[j].Y * scale,
                                                   diameter + (float)CosCache[i] * rightgraph[j].Y * scale));
                        circleB.Add(new RawVector2(diameter + (float)SinCache[i] * bothgraph[j].Y * scale,
                                                   diameter + (float)CosCache[i] * bothgraph[j].Y * scale));
                        j++;
                    }
                }

                using (var geometry = new PathGeometry(target.Factory))
                {
                    var sink = geometry.Open();
                    sink.BeginFigure(circleL.First(), FigureBegin.Filled);
                    sink.AddLines(circleL.ToArray());
                    sink.AddLines(circleR.ToArray());
                    sink.EndFigure(FigureEnd.Open);
                    sink.Close();

                    target.DrawGeometry(geometry, new SolidColorBrush(target, palette.Saturated.ToRawColor4()));
                }

                using (var geometry2 = new PathGeometry(target.Factory))
                {
                    var sink2 = geometry2.Open();
                    sink2.BeginFigure(circleB.Take(completeGraphLength >= 2 ? completeGraphLength : 2).First(), FigureBegin.Filled);
                    sink2.AddLines(circleB.Take(completeGraphLength >= 2 ? completeGraphLength : 2).ToArray());
                    sink2.EndFigure(FigureEnd.Open);
                    sink2.Close();

                    // Countdown
                    target.DrawGeometry(geometry2, new SolidColorBrush(target, palette.Brightest.ToRawColor4()), 8.0f);
                }

                if (circleB.Skip(completeGraphLength >= 2 ? completeGraphLength : 0).Count() > 2)
                {
                    using (var geometry3 = new PathGeometry(target.Factory))
                    {
                        var sink3 = geometry3.Open();
                        sink3.BeginFigure(circleB.Skip(completeGraphLength >= 2 ? completeGraphLength : 0).First(), FigureBegin.Filled);
                        sink3.AddLines(circleB.Skip(completeGraphLength >= 2 ? completeGraphLength : 0).ToArray());
                        sink3.EndFigure(FigureEnd.Open);
                        sink3.Close();

                        target.DrawGeometry(geometry3, new SolidColorBrush(target, palette.Darkest.ToRawColor4()), 8.0f);
                    }
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

        #endregion

        #region Methods

        private float ScaleSample(short sample, int height, int width)
        {
            return ((sample * height * 0.4f) / short.MaxValue) + (height / 2);
        }

        private List<RawPoint> Circle(int size)
        {
            var circle = new List<RawPoint>();
            for (double i = 0; i < 2 * Math.PI; i += 0.021)
            {
                circle.Add(new RawPoint((int)(size + Math.Sin(i) * size), (int)(size + Math.Cos(i) * size)));
            }

            circle.Add(circle.First());

            return circle;
        }

        #endregion
    }
}
