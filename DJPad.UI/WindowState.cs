namespace DJPad.UI
{
    using System;
    using System.Timers;
    using System.Drawing;
    using System.Windows.Forms;
    using System.Windows.Forms.VisualStyles;
    using SharpDX.Direct2D1;

    public class WindowState
    {
        public delegate void WindowArgs(object sender);

        public Action Close { get; set; }

        public Action Repaint { get; set; }

        public Func<Point> Location { get; set; }

        public Control Form { get; set; }

        public Action ChangeUi { get; set; }

        public Action Move { get; set; }

        public WindowState()
        {
            WindowRefresh.Elapsed += (o, e) =>
            {
                if (this.Repaint != null)
                {
                    this.Repaint();
                }
            };
        }

        public void Init()
        {
            this.ChangeUi();
            if (this.Repaint != null)
            {
                this.Repaint();
            }
        }

        private static readonly System.Timers.Timer WindowRefresh = new System.Timers.Timer(30);

        public void StartRendering()
        {
            WindowRefresh.Start();
        }

        public void StopRendering()
        {
            WindowRefresh.Stop();
        }
    }

    public class D2DWindowState : WindowState
    {
        public WindowRenderTarget Target { get; set; }
    }
}
