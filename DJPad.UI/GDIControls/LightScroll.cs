namespace DJPad.UI.GdiPlus
{
    using System;
    using System.Drawing;

    public class LightScroll : LightButton
    {
        public enum ScrollOrientation
        {
            Horizontal,
            Vertical
        };

        public Action<int> OnScroll { get; set; }

        public ScrollOrientation Orientation { get; private set; }

        public LightScroll(ScrollOrientation orientation = ScrollOrientation.Horizontal)
        {
            this.Orientation = orientation;
            this.OnClick = p =>
            {
                if (this.OnScroll != null)
                {
                    this.OnScroll(Position(p));
                }
            };
        }

        private int Position(Point p)
        {
            if (this.Orientation == ScrollOrientation.Horizontal)
            {
                return 100 * (p.X - this.Extents.X) /this.Extents.Width;
            }

            if (this.Orientation == ScrollOrientation.Vertical)
            {
                return 100 * p.Y / this.Extents.Height;
            }

            return 0;
        }
    }
}