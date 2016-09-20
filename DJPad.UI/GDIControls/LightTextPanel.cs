namespace DJPad.UI.GdiPlus
{
    using System;
    using System.Drawing;
    using DJPad.Core.Utils;

    public sealed class LightTextPanel : LightControl<Bitmap>
    {
        private readonly CachingBitmapProducer<Bitmap> bitmapCache = new CachingBitmapProducer<Bitmap>();

        public LightTextPanel()
        {
            this.Color = () => System.Drawing.Color.White;
            this.BorderColor = () => System.Drawing.Color.SlateGray;

            bitmapCache.BitmapProducer = () => new MetadataTextImageProducer { StringFormat = this.GetStringFormat() }
                                               .DrawText(
                                                    this.Text(),
                                                    this.Color(),
                                                    this.Background(),
                                                    this.BorderColor(),
                                                    this.Font,
                                                    this.Extents.Size);

            this.Image = () => this.bitmapCache.GetBitmap(Text() + Color());
        }

        public enum Justification { Left, Right, Center }
        public Func<Color> Color { get; set; }
        public Func<Color> BorderColor { get; set; } 
        public Font Font { get; set; }
        public SizeF Size { get; set; }
        public Justification Justify { get; set; }
        public Func<string> Text { get; set; }

        private StringFormat GetStringFormat()
        {
            switch(this.Justify){
                case Justification.Right:
                    return new StringFormat
                    {
                        Alignment = StringAlignment.Far,
                        LineAlignment = StringAlignment.Far,
                        FormatFlags = StringFormatFlags.NoWrap
                    };
                case Justification.Left: 
                    return new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Near,
                        FormatFlags = StringFormatFlags.NoWrap
                    };
                default:
                    return new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap
                    };
            }
        }

    }
}