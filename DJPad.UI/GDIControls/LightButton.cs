namespace DJPad.UI.GdiPlus
{
    using DJPad.UI.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    public class LightButton : LightControl<Bitmap>, IClickable
    {
        public LightButton()
        {
            this.Static = false;
        }

        public Action<Point> OnClick { get; set; }

        public Action<Point> OnDoubleClick { get; set; }
    }
}