using DJPad.Core.Interfaces;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using DJPad.Core.Utils;

namespace DJPad.Core.Vis
{
    using DJPad.Types;

    public class AlbumText : BaseVisualisation
    {
        public IMetadata Metadata
        {
            get;
            set;
        }

        int position = 0;
        
        protected Font bigFont = new Font("Segoe UI", 26, FontStyle.Bold);
        protected Font smallFont = new Font("Segoe UI", 12, FontStyle.Bold);

        public override void Draw(System.Drawing.Graphics g, System.Drawing.Color backgroundColor, int width, int height, ColorPalette palette, TimeSpan? duration = null, bool playing = true)
        {
            g.CompositingMode = CompositingMode.SourceCopy;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.FillRectangle(new SolidBrush(Color.Transparent), new Rectangle(0, 0, width, height));

            if (this.Metadata == null)
            {
                return;
            }

            if (!playing)
            {
                position = 0;
            }

            var size = new Size(width, height);

            var path = new GraphicsPath();

            path.AddString(this.Metadata.Title, this.bigFont.FontFamily, (int)this.bigFont.Style, this.bigFont.Size, new Rectangle(new Point(0, position + 10), size), new StringFormat { Alignment = StringAlignment.Near});
            path.AddString(this.Metadata.Album, this.smallFont.FontFamily, (int)this.smallFont.Style , this.smallFont.Size, new Rectangle(new Point(2, position), size),  new StringFormat { Alignment = StringAlignment.Near});

            var pen = new Pen(palette.Darkest.MakeTransparent((float)position / (float)height), 4) { LineJoin = LineJoin.Bevel };

            g.DrawPath(pen, path);
            g.FillPath(new SolidBrush(palette.Brightest.MakeTransparent((float)position / (float)height)), path);

            position = (++position) % height;

            //g.DrawString(
                 
            //    new Font("Segoe UI", 20, FontStyle.Bold),
            //    new SolidBrush(backgroundColor), 
            //    position, position + 10);

            //g.DrawString(
            //    ,
            //    new Font("Segoe UI", 12, FontStyle.Bold),
            //    new SolidBrush(backgroundColor),
            //    -position, position);
        }
    }
}
