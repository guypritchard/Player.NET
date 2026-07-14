namespace DJPad.UI.D2D
{
    using DJPad.UI.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Vortice.Direct2D1;

    public class LightButton : LightControl<ID2D1Bitmap>, IClickable
    {
        public Dictionary<string, Bitmap> ButtonStates { get; set; } 

        public LightButton()
        {
            this.Static = false;
        }

        public Action<Point> OnClick { get; set; }

        public Action<Point> OnDoubleClick { get; set; }

        public D2DWindowState Window { get; set; }

        public override Func<ID2D1Bitmap> Image { get; set; }
    }
}
