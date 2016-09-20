namespace DJPad.Core.Vis
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using DJPad.Core;
    using DJPad.Core.Utils;
    using DJPad.Types;

    public class FftBars : FftBasedVisualisation
    {
        float[] maxValues; 

        public FftBars() : base(0, false)
        {
        }

        //http://stackoverflow.com/questions/20408388/how-to-filter-fft-data-for-audio-visualisation
        protected override void DrawChannel(Graphics g, int width, int height, Sample.Channel channel, int zoom = 1, ColorPalette palette = null)
        {
            g.CompositingMode = CompositingMode.SourceCopy;
            
            float[] spect = this.fftTransform.calculateMagnitude(this.copiedSample.ToFftArray(channel));
            if (this.maxValues == null)
            {
                this.maxValues = new float[spect.Length];
                Array.Copy(spect, this.maxValues, spect.Length);
            }

            Array.Resize(ref spect, spect.Length/2);

            g.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, width, height);
            //var bars = new RectangleF[25];

            
            //for (int i = 0; i < spect.Length; i += 8)
            //{
            //    if (i * 2 < width)
            //    {
            //        var value = (spect[i] + spect[i + 1] + spect[i + 2] + spect[i + 3] + spect[i + 4] + spect[i + 5] + spect[i + 6] + spect[i + 7])/32;
            //        if (value <= 0)
            //        {
            //            continue;
            //        }

            //        bars[i / 8] = new RectangleF(
            //            (i * 2) - (i >= 0 ? 0 : 1) , 
            //            Math.Max(0, height - value), 
            //            15,
            //            value);
            //    }
            //}

            var bars = new RectangleF[50];


            for (int i = 0; i < spect.Length; i += 4)
            {
                if (i * 2 < width)
                {
                    this.maxValues[i] = Math.Max(spect[i], this.maxValues[i]);
                    this.maxValues[i + 1] = Math.Max(spect[i + 1], this.maxValues[i + 1]);
                    this.maxValues[i + 2] = Math.Max(spect[i + 2], this.maxValues[i + 2]);
                    this.maxValues[i + 3] = Math.Max(spect[i + 3], this.maxValues[i + 3]);

                    var value = (this.maxValues[i] + this.maxValues[i + 1] + this.maxValues[i + 2] + this.maxValues[i + 3]) / 16;
                    if (value <= 0)
                    {
                        continue;
                    }

                    bars[i / 4] = new RectangleF(
                        (i * 2) - (i >= 0 ? 0 : 1),
                        Math.Max(0, height - value),
                        7,
                        value);

                    var dropoff = 90;

                    this.maxValues[i]-= dropoff;
                    this.maxValues[i + 1]-= dropoff;
                    this.maxValues[i + 2]-= dropoff;
                    this.maxValues[i + 3]-= dropoff;
                }
            }

            foreach (var rect in bars)
            {
                var roundedRect = Rectangle.Round(rect);

                if (roundedRect.Height <= 0 || roundedRect.Width <= 0 || roundedRect.Right > width)
                {
                    continue;
                }

                try
                {
                    g.FillRectangle(new LinearGradientBrush(roundedRect, palette.Brightest.MakeTransparent(0.8f), palette.Darkest.MakeTransparent(0.2f), 90.0f), roundedRect);
                }
                catch(Exception)
                {
                }
            }
        }
    }
}