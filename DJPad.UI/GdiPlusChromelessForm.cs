namespace DJPad.UI.GdiPlus
{
    using DJPad.UI.GdiPlus;
    using DJPad.UI.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public abstract class GdiPlusChromelessForm : Form
    {
        protected List<LightControl<Bitmap>> LightControlCollection = new List<LightControl<Bitmap>>();

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_NCLBUTTONDBLCLK = 0x00A3; //double click on a title bar a.k.a. non-client area of the form
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_NCMOUSEMOVE = 0x00A0;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int WM_MOUSEWHEEL = 0x20A;
        private const int HTNOWHERE = 0x0;
        private const int HTCLIENT = 0x1;
        private const int HTCAPTION = 0x2;
        private const int HTTRANSPARENT = -1;
        private const int WM_TIMER = 0x0113;
        private const int CSDROPSHADOW = 0x00020000;

        private readonly System.Timers.Timer displayControlTimer;
        private DateTime previousMove;

        protected GdiPlusChromelessForm(bool child)
        {
            this.ShowInTaskbar = !child;
            this.StartPosition = FormStartPosition.Manual;
            this.AllowDrop = true;
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.MouseMove += MouseMoveHandler;
            this.MouseEnter += MouseMoveHandler;
            this.MouseWheel += MouseWheelHandler;
            this.FormClosing += (o, e) => this.displayControlTimer.Enabled = false;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque | ControlStyles.UserPaint, true);

            this.displayControlTimer = new System.Timers.Timer { Interval = 1000, Enabled = false };
            this.displayControlTimer.Elapsed += 
                (s, e) => this.InvokeIfRequired(() =>
                {
                    if (this.IsHoveringOverControls(this.PointToClient(Cursor.Position)))
                    {
                        return;
                    }

                    this.LightControlCollection.ForEach(c =>
                    {
                        if (!c.AlwaysVisible)
                        {
                            c.Display = !c.Display;
                        }
                    });

                    Debug.WriteLine("Canceling Timer.");
                    this.displayControlTimer.Enabled = false;
                    this.Invalidate();
                });
        }

        void LightOnKeyPress(int wParam, int lParam)
        {
            Debug.WriteLine("KeyPress");

            foreach(var control in this.LightControlCollection.Where(c => c is IAcceptKeys))
            {
                var acceptsKeys = control as IAcceptKeys;

                if (acceptsKeys != null && acceptsKeys.OnKey != null)
                {
                    acceptsKeys.OnKey(GetKeyCode(wParam), (Control.ModifierKeys & Keys.Shift) == Keys.Shift, Control.IsKeyLocked(Keys.CapsLock));
                }
            }

            this.Invalidate();
        }

        private Keys GetKeyCode(int wParam)
        {
            return (Keys)wParam;
            //switch (wParam)
            //{
            //    case 0x0D:
            //        return Keys.Enter;
            //    case 0x21:
            //        return Keys.PageUp;
            //    case 0x22:
            //        return Keys.PageDown;
            //    case 0x23:
            //        return Keys.End;
            //    case 0x24:
            //        return Keys.Home;
            //    case 0x26:
            //        return Keys.Up;
            //    case 0x28:
            //        return Keys.Down;

            //    default:
            //        if (wParam >= 0x41 && wParam <= 0x5A)
            //        {
            //            return (Keys)wParam; 
            //        }

            //        return Keys.Space;
        }


        private void MouseWheelHandler(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("Scroll");

            foreach (var control in this.LightControlCollection.Where(c => c is IScrollable))
            {
                if (control.Extents.Contains(e.Location))
                {
                    var scrollableControl = control as LightPlaylist;

                    if (scrollableControl != null)
                    {
                        if (scrollableControl.OnScroll == null)
                        {
                            Debug.WriteLine(scrollableControl.Name + " has no OnScroll handler.");

                            // Look for other controls at this position that can accept this click.
                            continue;
                        }

                        scrollableControl.OnScroll(e.Delta);
                    }
                }

                this.Invalidate();
            }
        }

        void MouseMoveHandler(object sender, EventArgs e)
        {
            this.DisplayControls();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CSDROPSHADOW;
                return cp;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            this.previousMove = DateTime.MinValue;
            this.DisplayControls();
            base.OnResize(e);
        }

        protected bool LightScroll(Point location)
        {
            Debug.WriteLine("Scrolling");

            return false;
        }

        protected bool LightOnMouseClick(Point location)
        {
            foreach (var control in this.LightControlCollection.Where(c => c is IClickable))
            {
                if (control.Extents.Contains(location))
                {
                    var clickableControl = control as IClickable;

                    if (clickableControl != null)
                    {
                        if (clickableControl.OnClick == null)
                        {
                            // Debug.WriteLine(clickableControl.Name + " has no OnClick handler.");

                            // Look for other controls at this position that can accept this click.
                            continue;
                        }

                        this.InvokeIfRequired(() => clickableControl.OnClick(location));
                        this.Invalidate();
                        return true;
                    }
                }
            }

            return false;
        }

        protected bool LightOnMouseDoubleClick(Point location)
        {
            foreach (var control in this.LightControlCollection.Where(c => c is IClickable))
            {
                if (control.Extents == Rectangle.Empty || control.Extents.Contains(location))
                {
                    var clickableControl = control as IClickable;

                    if (clickableControl != null)
                    {
                        if (clickableControl.OnDoubleClick != null)
                        {
                            clickableControl.OnDoubleClick(location);
                            this.Invalidate();
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONDBLCLK)
            {
                if (this.LightOnMouseClick(GetPosition(m.LParam)))
                {
                    return;
                }
            }

            if (m.Msg == WM_LBUTTONDBLCLK)
            {
                if (this.LightOnMouseDoubleClick(GetPosition(m.LParam)))
                {
                    return;
                }
            }

            if (m.Msg == WM_NCLBUTTONDBLCLK)
            {
                var pos = GetPosition(m.LParam);
                Debug.WriteLine(string.Format("{0}:{1}", pos.X, pos.Y));

                if (this.LightOnMouseDoubleClick(this.PointToClient(pos)))
                {
                    return;
                }
            }

            if (m.Msg == WM_KEYDOWN)
            {
                Debug.WriteLine(string.Format("{0}:{1}:{2}", m.HWnd, (int)m.LParam, (int)m.WParam));
                this.LightOnKeyPress((int)m.WParam, (int)m.LParam);
                return;
            }

            if (m.Msg == WM_KEYUP)
            {
                this.Invalidate();
                return;
            }

            if (m.Msg == WM_NCHITTEST)
            {
                var point = this.PointToClient(GetPosition(m.LParam));

                Debug.WriteLine(string.Format("Point [{0},{1}]", point.X, point.Y));

                // Always set focus when mouse over the client area. 
                if (this.ClientRectangle.Contains(point))
                {
                    if (this.IsHoveringOverControls(point))
                    {
                        m.Result = (IntPtr)HTCLIENT;
                    }
                    else
                    {
                        m.Result = (IntPtr)HTCAPTION;
                    }

                    this.DisplayControls();
                    return;
                }
            }

            base.WndProc(ref m);
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            foreach (var control in this.LightControlCollection)
            {
                if (control.Display && control.Image != null)
                {
                    var image = control.Image();
                    if (image != null)
                    {
                        if (control.Static)
                        {
                            e.Graphics.DrawImageUnscaled(image, control.Extents);
                        }
                        else
                        {
                            e.Graphics.DrawImage(image, control.Extents);
                        }
                    }
                }
            }

            
#if DEBUG
            // For debugging.
            e.Graphics.DrawRectangles(new Pen(Brushes.Red), this.LightControlCollection.Select(c => c.Extents).ToArray());
#endif

            base.OnPaint(e);
        }

        private static Point GetPosition(IntPtr position)
        {
            return new Point(IntPtr.Size == 8 ? unchecked((int)position.ToInt64()) : position.ToInt32());

            //uint xy = unchecked(IntPtr.Size == 8 ? (uint)position.ToInt64() : (uint)position.ToInt32());
            //int x = unchecked((short)xy);
            //int y = unchecked((short)(xy >> 16));
            //return new Point(x, y);
        }

        private void DisplayControls()
        {
            if (DateTime.UtcNow > previousMove.AddSeconds(1.0))
            {
                previousMove = DateTime.UtcNow;
                // cancel previous timer.
                this.displayControlTimer.Enabled = false;
                this.LightControlCollection.ForEach(c => c.Display = true);
                this.displayControlTimer.Enabled = true;
                this.Focus();
                this.Invalidate();
                Debug.WriteLine("Starting timer.");
            }
        }

        private bool IsHoveringOverControls(Point point)
        {
            return this.LightControlCollection.Any(c =>
                                                   {
                                                       // We want to be able to drag the window from the areas where the menu exists, but doesn't have any buttons.
                                                       if (c is LightMenu)
                                                       {
                                                           var menu = c as LightMenu;
                                                           return menu.Children.Any(mb => c is LightButton && mb.Extents.Contains(point));
                                                       }

                                                       return c is IClickable && ((IClickable)c).OnClick != null && c.Extents.Contains(point);
                                                   });
        }
    }
}