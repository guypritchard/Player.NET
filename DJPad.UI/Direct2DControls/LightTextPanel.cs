namespace DJPad.UI.D2D
{
    using System;
    using System.Drawing;
    using DJPad.Core.Utils;
    using SharpDX.Direct2D1;

    public sealed class D2DLightTextPanel : LightControl<SharpDX.Direct2D1.Bitmap>
    {
        private readonly CachingBitmapProducer<SharpDX.Direct2D1.Bitmap> bitmapCache = new CachingBitmapProducer<SharpDX.Direct2D1.Bitmap>();

        public D2DLightTextPanel()
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
                                                    this.Extents.Size).ToD2dBitmap(this.Target);

            this.Image = () => this.bitmapCache.GetBitmap(Text() + Color());
        }
        public WindowRenderTarget Target { get; set; }

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