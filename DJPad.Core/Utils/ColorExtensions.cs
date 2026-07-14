
namespace DJPad.Core.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Drawing;
    using Color4 = Vortice.Mathematics.Color4;

    public static class ColorExtensions
    {
        public static Color Lighten(this Color color)
        {
            float correctionFactor = 0.25f;
            float red = (255 - color.R) * correctionFactor + color.R;
            float green = (255 - color.G) * correctionFactor + color.G;
            float blue = (255 - color.B) * correctionFactor + color.B;
            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }

        public static Color MakeTransparent(this Color color, float opacity)
        {
            if (opacity < 0.0f)
            {
                opacity = 0.0f;
            }

            if (opacity > 1.0f)
            {
                opacity = 1.0f;
            }

            return Color.FromArgb((int)(255 * opacity), color.R, color.G, color.B);
        }

        public static Color4 ToColor4(this Color color)
        {
            return new Color4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
        }
    }
}
