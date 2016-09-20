namespace DJPad.Core.Utils
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using DJPad.Core.Interfaces;

    public partial class VisualisationHost : Form
    {
        private readonly IVisualisation visualisationSource;

        public bool DisplayFPS { get; set; }

        private DateTime LastPaint { get; set; }

        public VisualisationHost(IVisualisation vis)
        {
            this.visualisationSource = vis;

            this.LastPaint = DateTime.UtcNow;

            this.InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.DrawImage(this.visualisationSource.Draw(this.ClientSize, Color.FromArgb(0, 0, 0, 0)), new Point());

            if (this.DisplayFPS)
            {
                var timeTaken = DateTime.UtcNow - this.LastPaint;

                e.Graphics.DrawString(string.Format("FPS:{0:0}", (1000 / timeTaken.TotalMilliseconds)), new Font("Arial", 18.0f, FontStyle.Bold), Brushes.Black, 0, 0);

                this.LastPaint = DateTime.UtcNow;
            }

            base.OnPaint(e);
        }
    }
}