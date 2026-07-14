namespace DJPad.Core.Vis
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using DJPad.Types;

    public sealed class PixelMosaic : ArtworkVisualisationBase
    {
        private const int Columns = 10;
        private const int Rows = 10;
        private readonly float[] energy = new float[Columns * Rows];
        private float phase;

        protected override void DrawArtwork(Graphics graphics, Size size, ColorPalette palette, bool playing)
        {
            if (this.Artwork == null)
            {
                return;
            }

            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            var samples = this.ReadMonoWindow();
            var tileWidth = size.Width / (float)Columns;
            var tileHeight = size.Height / (float)Rows;
            var gridColor = palette?.Brightest ?? Color.White;
            using var gridPen = new Pen(Color.FromArgb(65, gridColor), 1);
            if (playing)
            {
                this.phase += 0.16f;
            }

            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    var index = (row * Columns) + column;
                    var first = samples.Length == 0 ? 0 : index * samples.Length / this.energy.Length;
                    var last = samples.Length == 0 ? 0 : Math.Max(first + 1, (index + 1) * samples.Length / this.energy.Length);
                    double sum = 0;
                    for (var sample = first; sample < last; sample++)
                    {
                        sum += samples[sample] * samples[sample];
                    }
                    var current = last > first ? (float)Math.Sqrt(sum / (last - first)) : 0;
                    this.energy[index] = (this.energy[index] * 0.45f) + (Math.Min(1, current * 6) * 0.55f);

                    var source = new RectangleF(column * this.Artwork.Width / (float)Columns,
                        row * this.Artwork.Height / (float)Rows,
                        this.Artwork.Width / (float)Columns,
                        this.Artwork.Height / (float)Rows);
                    var centerX = (column + 0.5f) * tileWidth;
                    var centerY = (row + 0.5f) * tileHeight;
                    var directionX = (centerX - (size.Width / 2.0f)) / size.Width;
                    var directionY = (centerY - (size.Height / 2.0f)) / size.Height;
                    var movement = this.energy[index] * 38;
                    var expansion = this.energy[index] * 10;
                    var wobble = (float)Math.Sin(this.phase + (index * 0.7f)) * this.energy[index] * 12;
                    var destination = new RectangleF(column * tileWidth + (directionX * movement) - (directionY * wobble) - expansion,
                        row * tileHeight + (directionY * movement) + (directionX * wobble) - expansion,
                        tileWidth + (expansion * 2), tileHeight + (expansion * 2));
                    graphics.DrawImage(this.Artwork, destination, source, GraphicsUnit.Pixel);
                    graphics.DrawRectangle(gridPen, destination.X, destination.Y, destination.Width, destination.Height);
                }
            }
        }
    }
}
