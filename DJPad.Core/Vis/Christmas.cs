using System;

namespace DJPad.Core.Vis
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using System.Timers;
    using DJPad.Core.Interfaces;
    using DJPad.Core.Utils;
    using DJPad.Types;

    public class Christmas : IVisualisation
    {
        public class SnowFlake
        {
            public Point Point { get; set; }
            public int Size { get; set; }
        }

        public event Action Redraw;

        public ISampleSource SampleSource
        {
            get;
            set;
        }

        private Bitmap privateImage;
        private List<SnowFlake> snowFlakes = new List<SnowFlake>();
        private Timer drawTimer;

        public Christmas()
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

        public Bitmap Draw(Size size, Color backgroundColor, bool playing = true, TimeSpan? duration = null, ColorPalette palette = null)
        {
            if (this.privateImage == null || this.privateImage.Height != size.Height || this.privateImage.Width != size.Width)
            {
                this.snowFlakes.Clear();
                this.snowFlakes.AddRange(Enumerable.Range(1, 10).Select(i =>
                    new SnowFlake
                    {
                        Point = new Point(StaticRandom.Next(size.Width), StaticRandom.Next(size.Height)), 
                        Size = StaticRandom.Next(10) + 1
                    }));
                this.privateImage = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            }

            this.Draw(Graphics.FromImage(this.privateImage), backgroundColor, size.Width, size.Height, playing, duration, palette);
            this.Move();

            return this.privateImage;
        }

        public void Draw(Graphics g, Color backgroundColor, int width, int height, bool playing = true, TimeSpan? duration = null, ColorPalette palette = null)
        {
            g.CompositingMode = CompositingMode.SourceCopy;
            g.FillRectangle(new SolidBrush(backgroundColor), new Rectangle(0, 0, width, height));

            foreach (var point in this.snowFlakes)
            {
                g.FillPolygon(Brushes.LightBlue, CalculateVertices(6, point.Size, 0, point.Point));
            }
        }

        private void Move()
        {
            foreach (var flake in this.snowFlakes)
            {
                var point = flake.Point;
                point.X = point.X + 1;
                flake.Point = point;
            }
        }

        private Point[] CalculateVertices(int sides, int radius, int startingAngle, Point center)
        {
            if (sides < 3)
                throw new ArgumentException("Polygon must have 3 sides or more.");

            List<Point> points = new List<Point>();
            float step = 360.0f / sides;

            float angle = startingAngle; //starting angle
            for (double i = startingAngle; i < startingAngle + 360.0; i += step) //go in a full circle
            {
                points.Add(DegreesToXY(angle, radius, center)); //code snippet from above
                angle += step;
            }

            return points.ToArray();
        }

        private Point DegreesToXY(float degrees, float radius, Point origin)
        {
            Point xy = new Point();
            double radians = degrees * Math.PI / 180.0;

            xy.X = (int)(Math.Cos(radians) * radius + origin.X);
            xy.Y = (int)(Math.Sin(-radians) * radius + origin.Y);

            return xy;
        }
    }
}
