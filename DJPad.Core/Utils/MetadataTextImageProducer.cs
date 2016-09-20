
namespace DJPad.Core.Utils
{
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Drawing.Text;

    public class MetadataTextImageProducer
    {
        public Color Color { get; set; }
        public Font Font { get; set; }
        public StringFormat StringFormat { get; set; }
        public SizeF Size { get; set; }

        public MetadataTextImageProducer()
        {
            this.StringFormat = new StringFormat
                                {
                                    Alignment = StringAlignment.Far,
                                    LineAlignment = StringAlignment.Far,
                                    FormatFlags = StringFormatFlags.NoWrap,
                                    Trimming = StringTrimming.EllipsisCharacter
                                };
        }

        /// <summary>
        /// http://www.codeproject.com/Articles/42529/Outline-Text
        /// </summary>
        /// <param name="text"></param>
        /// <param name="borderColor"></param>
        /// <param name="font"></param>
        /// <param name="size"></param>
        /// <param name="textColor"></param>
        /// <returns></returns>
        public Bitmap DrawText(string text, Color textColor, Color backgroundColor, Color borderColor, Font font, Size size)
        {
            var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                var path = new GraphicsPath();

                path.AddString(text, font.FontFamily, (int)font.Style, font.Size, new RectangleF(new PointF(), size), this.StringFormat);

                var pen = new Pen(borderColor, 2) { LineJoin = LineJoin.Bevel };

                graphics.DrawPath(pen, path);
                graphics.FillRectangle(new SolidBrush(backgroundColor), new Rectangle(new Point(), size));
                graphics.FillPath(new SolidBrush(textColor), path);
            }

            return bitmap;
        }
    }
}
