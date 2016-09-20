namespace DJPad.Core.Vis
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using DJPad.Core;
    using DJPad.Core.Utils;
    using DJPad.Types;

    public class BassZoom : FftBasedVisualisation
    {
        public BassZoom()
            : base(0, false)
        {
        }

        // https://en.wikipedia.org/wiki/Superformula
        // http://stackoverflow.com/questions/20408388/how-to-filter-fft-data-for-audio-visualisation
        protected override void DrawChannel(Graphics g, int width, int height, Sample.Channel channel, int zoom = 1, ColorPalette palette = null)
        {
            g.CompositingMode = CompositingMode.SourceCopy;

            g.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, width, height);

            float[] spect = this.fftTransform.calculateMagnitude(this.copiedSample.ToFftArray(channel));
            Array.Resize(ref spect, spect.Length / 2);

            var i = 0;
            var beat1 = Math.Min((spect[i] +
                                     spect[i + 4] +
                                     spect[i + 5] +
                                     spect[i + 6] +
                                     spect[i + 7]) / 16,
                                     height);

            var beat2 = Math.Min((spect[i + 9] +
                                  spect[i + 10] +
                                  spect[i + 11] +
                                  spect[i + 12]) / 16,
                                  height - 20);

            var beat3 = Math.Min((spect[i + 13] +
                                  spect[i + 14] +
                                  spect[i + 15] +
                                  spect[i + 16]) / 16,
                                  height - 40);


            g.FillEllipse(
                new SolidBrush(palette.Brightest.MakeTransparent(0.7f)), 
                (height / 2) - (beat1 / 2), 
                (height / 2) - (beat1 / 2), 
                beat1, 
                beat1);

            g.FillEllipse(
                new SolidBrush(palette.Darkest.MakeTransparent(0.7f)), 
                (height / 2) - (beat2 / 2), 
                (height / 2) - (beat2 / 2), 
                beat2, 
                beat2);

            g.FillEllipse(
               new SolidBrush(palette.Saturated.MakeTransparent(0.7f)),
               (height / 2) - (beat3 / 2),
               (height / 2) - (beat3 / 2),
               beat3,
               beat3);
        }
    }
}
