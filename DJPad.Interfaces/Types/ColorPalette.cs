namespace DJPad.Types
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    public class ColorPalette
    {
        public ColorPalette(IEnumerable<Color> colors)
        {
            this.Range = new List<Color>(colors).OrderBy(c => c.GetBrightness()).ToList();
        }

        IList<Color> Range { get; set; }

        public Color Darkest
        {
            get
            {
                if (this.Range.Any())
                {
                    return this.Range.OrderBy(c => c.GetBrightness()).First();
                }

                return Color.Empty;
            }
        }

        public Color Saturated
        {
            get
            {
                if (this.Range.Any())
                {
                    return this.Range.OrderBy(c => c.GetSaturation()).First();
                }

                return Color.Empty;
            }
        }

        public Color Brightest
        {
            get
            {
                if (this.Range.Any())
                {
                    return this.Range.OrderBy(c => c.GetBrightness()).Last();
                }

                return Color.Empty;
            }
        }
    }
}
