namespace DJPad.UI
{
    using System;
    using System.Drawing;

    public abstract class LightControl<T>
    {
        protected LightControl()
        {
            this.Background = () => Color.Transparent;
            this.Static = true;
        }
       
        public string Name { get; set; }

        public Rectangle Extents { get; set; }

        public virtual Func<T> Image { get; set; }

        /// <summary>
        /// Should the item render.
        /// </summary>
        public bool Display { get; set; }

        /// <summary>
        /// Should the item always render.
        /// </summary>
        public bool AlwaysVisible { get; set; }

        public bool Visible { get; set; }

        public Func<Color> Background { get; set; }

        public bool Static { get; protected set; }
    }
}
