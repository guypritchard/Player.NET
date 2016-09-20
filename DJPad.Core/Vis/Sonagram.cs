namespace DJPad.Core.Vis
{
    using System.Drawing.Drawing2D;
    using DJPad.Core;
    using System;
    using System.Drawing;
    using DJPad.Types;

    public class Sonagram : FftBasedVisualisation
    {
        /** colour map used in the spectrogram */
	    static readonly Color[] RedColors = {
           Color.FromArgb(255, 11,  0,  0),
           Color.FromArgb(255, 21,  0,  0),
           Color.FromArgb(255, 32,  0,  0),
           Color.FromArgb(255, 43,  0,  0),
           Color.FromArgb(255, 53,  0,  0),
           Color.FromArgb(255, 64,  0,  0),
           Color.FromArgb(255, 74,  0,  0),
           Color.FromArgb(255, 85,  0,  0),
           Color.FromArgb(255, 96,  0,  0),
           Color.FromArgb(255,106,  0,  0),
           Color.FromArgb(255,117,  0,  0),
           Color.FromArgb(255,128,  0,  0),
           Color.FromArgb(255,138,  0,  0),
           Color.FromArgb(255,149,  0,  0),
           Color.FromArgb(255,159,  0,  0),
           Color.FromArgb(255,170,  0,  0),
           Color.FromArgb(255,181,  0,  0),
           Color.FromArgb(255,191,  0,  0),
           Color.FromArgb(255,202,  0,  0),
           Color.FromArgb(255,212,  0,  0),
           Color.FromArgb(255,223,  0,  0),
           Color.FromArgb(255,234,  0,  0),
           Color.FromArgb(255,244,  0,  0),
           Color.FromArgb(255,255,  0,  0),
           Color.FromArgb(255,255, 11,  0),
           Color.FromArgb(255,255, 21,  0),
           Color.FromArgb(255,255, 32,  0),
           Color.FromArgb(255,255, 43,  0),
           Color.FromArgb(255,255, 53,  0),
           Color.FromArgb(255,255, 64,  0),
           Color.FromArgb(255,255, 74,  0),
           Color.FromArgb(255,255, 85,  0),
           Color.FromArgb(255,255, 96,  0),
           Color.FromArgb(255,255,106,  0),
           Color.FromArgb(255,255,117,  0),
           Color.FromArgb(255,255,128,  0),
           Color.FromArgb(255,255,138,  0),
           Color.FromArgb(255,255,149,  0),
           Color.FromArgb(255,255,159,  0),
           Color.FromArgb(255,255,170,  0),
           Color.FromArgb(255,255,181,  0),
           Color.FromArgb(255,255,191,  0),
           Color.FromArgb(255,255,202,  0),
           Color.FromArgb(255,255,212,  0),
           Color.FromArgb(255,255,223,  0),
           Color.FromArgb(255,255,234,  0),
           Color.FromArgb(255,255,244,  0),
           Color.FromArgb(255,255,255,  0),
           Color.FromArgb(255,255,255, 16),
           Color.FromArgb(255,255,255, 32),
           Color.FromArgb(255,255,255, 48),
           Color.FromArgb(255,255,255, 64),
           Color.FromArgb(255,255,255, 80),
           Color.FromArgb(255,255,255, 96),
           Color.FromArgb(255,255,255,112),
           Color.FromArgb(255,255,255,128),
           Color.FromArgb(255,255,255,143),
           Color.FromArgb(255,255,255,159),
           Color.FromArgb(255,255,255,175),
           Color.FromArgb(255,255,255,191),
           Color.FromArgb(255,255,255,207),
           Color.FromArgb(255,255,255,223),
           Color.FromArgb(255,255,255,239),
           Color.FromArgb(255,255,255,255)};

        static readonly Color[] BlueColors = {
           Color.FromArgb(255, 0,  0,  11),
           Color.FromArgb(255, 0,  0,  21),
           Color.FromArgb(255, 0,  0,  32),
           Color.FromArgb(255, 0,  0,  43),
           Color.FromArgb(255, 0,  0,  53),
           Color.FromArgb(255, 0,  0,  64),
           Color.FromArgb(255, 0,  0,  74),
           Color.FromArgb(255, 0,  0,  85),
           Color.FromArgb(255, 0,  0,  96),
           Color.FromArgb(255,0,  0,  106),
           Color.FromArgb(255,0,  0,  117),
           Color.FromArgb(255,0,  0,  128),
           Color.FromArgb(255,0,  0,  138),
           Color.FromArgb(255,0,  0,  149),
           Color.FromArgb(255,0,  0,  159),
           Color.FromArgb(255,0,  0,  170),
           Color.FromArgb(255,0,  0,  181),
           Color.FromArgb(255,0,  0,  191),
           Color.FromArgb(255,0,  0,  202),
           Color.FromArgb(255,0,  0,  212),
           Color.FromArgb(255,0,  0,  223),
           Color.FromArgb(255,0,  0,  234),
           Color.FromArgb(255,0,  0,  244),
           Color.FromArgb(255, 0,  0, 255),
           Color.FromArgb(255, 0, 11, 255),
           Color.FromArgb(255, 0, 21, 255),
           Color.FromArgb(255, 0, 32, 255),
           Color.FromArgb(255, 0, 43, 255),
           Color.FromArgb(255, 0, 53, 255),
           Color.FromArgb(255, 0, 64, 255),
           Color.FromArgb(255, 0, 74, 255),
           Color.FromArgb(255, 0, 85, 255),
           //Color.FromArgb(255, 0, 96, 255),
           //Color.FromArgb(255, 0,106, 255),
           //Color.FromArgb(255, 0,117, 255),
           //Color.FromArgb(255, 0,128, 255),
           //Color.FromArgb(255, 0,138, 255),
           //Color.FromArgb(255, 0,149, 255),
           //Color.FromArgb(255, 0,159, 255),
           //Color.FromArgb(255, 0,170, 255),
           //Color.FromArgb(255, 0,181, 255),
           //Color.FromArgb(255, 0,191, 255),
           //Color.FromArgb(255, 0,202, 255),
           //Color.FromArgb(255, 0,212, 255),
           //Color.FromArgb(255, 0,223, 255),
           //Color.FromArgb(255,0,234,  255),
           //Color.FromArgb(255,0,244,  255),
           //Color.FromArgb(255,0,255,  255),
           //Color.FromArgb(255,16,255, 255),
           //Color.FromArgb(255,32,255, 255),
           //Color.FromArgb(255,48,255, 255),
           //Color.FromArgb(255,64,255, 255),
           //Color.FromArgb(255,80,255, 255),
           //Color.FromArgb(255,96,255, 255),
           //Color.FromArgb(255,112,255,255),
           //Color.FromArgb(255,128,255,255),
           //Color.FromArgb(255,143,255,255),
           //Color.FromArgb(255,159,255,255),
           //Color.FromArgb(255,175,255,255),
           //Color.FromArgb(255,191,255,255),
           //Color.FromArgb(255,207,255,255),
           //Color.FromArgb(255,223,255,255),
           //Color.FromArgb(255,239,255,255),
           //Color.FromArgb(255,255,255,255)
                                             
                                             };

       
        private int position;
        private ColorPalette defaultColorPalette = new ColorPalette(new[] { Color.DarkOrange, Color.LightSkyBlue, Color.SlateGray });

        public Sonagram() : base(0, false)
        {
        }

        //http://stackoverflow.com/questions/20408388/how-to-filter-fft-data-for-audio-visualisation
        protected override void DrawChannel(Graphics g, int width, int height, Sample.Channel channel, int zoom = 4, ColorPalette palette = null)
        {
            if (palette == null)
            {
                palette = this.defaultColorPalette;
            }
            var color = palette.Brightest;

            g.CompositingMode = CompositingMode.SourceCopy;
            
            var drawPosition = this.position++;
            float[] spect = this.fftTransform.calculateMagnitude(this.copiedSample.ToFftArray(channel));


            var verticalPosition = 0;
            for (var i = height; i != 0; i--)
            {
                var logSpect = spect[i] != 0 ? (int)Math.Sqrt(spect[i]) * 6 : 0;
                logSpect = logSpect > 255 ? 255 : logSpect;
                var percentage = (float) logSpect / 255;

                g.FillRectangle(new SolidBrush(
                    Color.FromArgb(
                    255,
                    (int)(color.R * percentage),
                    (int)(color.G * percentage),
                    (int)(color.B * percentage))), drawPosition * zoom % width, verticalPosition++ * zoom, zoom, zoom);


               //  g.FillRectangle(new SolidBrush(this.GetColor(spect[i])), drawPosition * zoom % width, i * zoom, zoom, zoom);
 
                //var logSpect = spect[i]= 0 ? (int)Math.Sqrt(spect[i]) * 4 : 0;
                //logSpect = logSpect > 255 ? 255 : logSpect;
                // g.FillRectangle(new SolidBrush(Color.FromArgb(255, logSpect / 4, logSpect, logSpect / 4)), drawPosition * zoom % width, i * zoom, zoom, zoom);
            }
        }

        Color GetColor(double c)
        {
            double loThreshold = 5.0;
	        double hiThreshold = 775.0;

            int max = BlueColors.Length - 1;
            int index = (int)(max - (c - loThreshold) * max / (hiThreshold - loThreshold));
            return BlueColors[max - (index < 0 ? 0 : (index > max ? max : index))];
        }
    }
}
