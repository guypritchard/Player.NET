namespace DJPad.UI.GdiPlus
{
    using Core.Utils;
    using DJPad.UI.Interfaces;
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Windows.Forms;

    public class LightTextBox : LightControl<Bitmap>, IAcceptKeys, IClickable
    {
        private readonly CachingBitmapProducer<Bitmap> bitmapCache = new CachingBitmapProducer<Bitmap>();

        public LightTextBox()
        {
            this._text = string.Empty;
            this.bitmapCache.BitmapProducer = () =>
                {
                    var image = new Bitmap(this.Extents.Size.Width, this.Extents.Size.Height, PixelFormat.Format32bppPArgb);
                    using (var g = Graphics.FromImage(image))
                    {
                        g.FillRectangle(new SolidBrush(Color.LightGray), new Rectangle(0, 0, this.Extents.Size.Width, this.Extents.Size.Height));

                        var numberText = new MetadataTextImageProducer
                        {
                            StringFormat = new StringFormat
                            {
                                Alignment = StringAlignment.Near,
                                LineAlignment = StringAlignment.Near,
                                FormatFlags = StringFormatFlags.NoWrap,
                            }
                        }.DrawText(
                            this.Text, 
                            Color.Black, 
                            Color.White,
                            Color.SlateGray,
                            new Font("Segoe UI Light", 16, FontStyle.Bold),
                            this.Extents.Size);

                      g.DrawImage(numberText, 0, 0, numberText.Width, numberText.Height);
                    }

                    return image;
                };


            this.Image = () => this.bitmapCache.GetBitmap(this.Text);

            this.OnKey = (k, s, c) =>
            {
                if (k >= System.Windows.Forms.Keys.A && k <= System.Windows.Forms.Keys.Z)
                {
                    this.Text += s || c ? k.ToString() : k.ToString().ToLower();
                    return;
                }

                if (k == System.Windows.Forms.Keys.OemBackslash || k == System.Windows.Forms.Keys.Oem5)
                {
                    this.Text += "\\";
                }

                if (k == System.Windows.Forms.Keys.OemPeriod)
                {
                    this.Text += ".";
                }

                if (k == System.Windows.Forms.Keys.OemQuestion && s)
                {
                    this.Text += "?";
                }
                
                if (k == System.Windows.Forms.Keys.Back)
                {
                    if (!string.IsNullOrEmpty(this.Text))
                    {
                        this.Text = this.Text.Remove(this.Text.Length - 1);
                    }
                }

                if (k == System.Windows.Forms.Keys.Space)
                {
                    this.Text += " ";
                }
            };

            this.Text = string.Empty;

            this.OnClick = (p) =>
            {
                this.Visible = false;
            };
        }

        public Action<Point> OnClick
        {
            get; set;
        }

        public Action<Point> OnDoubleClick
        {
            get;
            set;
        }

        public Action<Keys, bool, bool> OnKey
        {
            get; set;
        }

        private string _text;

        public string Text
        {
            get { return this._text; }
            set
            {
                if (value == null)
                {
                    this._text = string.Empty;
                }

                this._text = value;
            }
        }
    }
}
