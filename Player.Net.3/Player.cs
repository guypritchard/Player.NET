using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;


namespace Player.Net._3
{
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Linq;
    using DJPad.Core.Utils;
    using DJPad.Player;
    using DJPad.UI;
    using global::Player.Net._2.UserInterface;
    using SharpDX;
   //sing SharpDX.DirectWrite;
    using SharpDX.Windows;
    using SharpDX.Direct2D1;

    using System.Windows.Forms;
    using Bitmap = SharpDX.Direct2D1.Bitmap;
    using PixelFormat = SharpDX.Direct2D1.PixelFormat;
    using DJPad.UI.Interfaces;
    using System;
    using System.Diagnostics;
    using DJPad.UI.D2D;

    public partial class Player : RenderForm
    {
        private const int WM_KEYDOWN = 0x0100;
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

        private List<LightControl<SharpDX.Direct2D1.Bitmap>> LightControlCollection = new List<LightControl<SharpDX.Direct2D1.Bitmap>>();
        private readonly D2DWindowState windowState;

        private readonly PlayerState playerState;
        private readonly MultimediaKeyListener listener;
        private readonly System.Timers.Timer displayControlTimer;
        private DateTime previousMove;

        IUserInterface<SharpDX.Direct2D1.Bitmap> userInterface = new NormalUi();

        WindowRenderTarget renderTarget2d;
        SharpDX.Direct2D1.Factory factory2d = new SharpDX.Direct2D1.Factory(FactoryType.SingleThreaded, DebugLevel.Information);
        SharpDX.DirectWrite.Factory factoryText = new SharpDX.DirectWrite.Factory();

        public Player()
        {
            this.AllowDrop = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Icon = Resources.Player;
            
            this.playerState = new PlayerState();
            this.listener = new MultimediaKeyListener();

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
                });

            this.listener.KeyPressed += (sender, args) =>
            {
                switch (args.Key)
                {
                    case MultimediaKey.PlayPause:
                        playerState.TogglePlay();
                        break;
                    case MultimediaKey.NextTrack:
                        playerState.Next();
                        break;
                    case MultimediaKey.PreviousTrack:
                        playerState.Previous();
                        break;
                }
            };
            
            this.DragEnter += (sender, args) =>
                            {
                                args.Effect = args.Data.GetDataPresent(DataFormats.FileDrop)
                                            ? DragDropEffects.Copy
                                            : DragDropEffects.None;
                            };

            this.DragDrop += (sender, args) =>
                            {
                                var fileNames = args.Data.GetData(DataFormats.FileDrop) as string[];
                                if (fileNames == null || !fileNames.Any())
                                {
                                    return;
                                }
                                if (fileNames.Length == 1)
                                {
                                    this.playerState.NewPlaylist(fileNames.Single(), true);
                                }
                                else
                                {
                                    this.playerState.NewPlaylist(fileNames, true);
                                }
                            };

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            this.playerState.Init();

            this.windowState = new D2DWindowState()
            {
                Close = () => this.Close(),
                Repaint = () => this.Invalidate(),
                Location = () => this.Location,
                Form = this,
                ChangeUi = () => this.ChangeUi(),
            };

            this.Initialize();
            
            this.MouseClick += (sender, args) =>
            {
                var location = args.Location;
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

                            clickableControl.OnClick(location);
                            // return true;
                        }
                    }
                }

                // return false;
            };

            this.Closing += (sender, args) =>
            {
                this.displayControlTimer.Enabled = false;
            };

            this.Resize += (sender, args) => this.DisplayControls();
            this.MouseMove += (sender, args) => this.DisplayControls();
        }

        protected void Initialize()
        {
            this.Text = userInterface.Name;
            this.MaximumSize = userInterface.Size;
            this.ClientSize = userInterface.Size;

            var targetProp = new HwndRenderTargetProperties
            {
                Hwnd = this.Handle,
                PixelSize = new SharpDX.Size2(this.Size.Width, this.Size.Height),
                PresentOptions = PresentOptions.None
            };

            var renderProp = new RenderTargetProperties(new PixelFormat(Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied));

            this.renderTarget2d = new WindowRenderTarget(
                this.factory2d,
                renderProp,
                targetProp) { AntialiasMode = AntialiasMode.PerPrimitive };

            this.windowState.Target = this.renderTarget2d;
            this.windowState.Close = this.Close;

            var layout = userInterface.GenerateUI(this.playerState, this.windowState);
            this.LightControlCollection.Clear();
            this.LightControlCollection.AddRange(layout);
            this.LightControlCollection.ForEach(c => c.Display = true);

            RenderLoop.Run(
                this,
                    () =>
                    {
                        this.renderTarget2d.BeginDraw();

                        // D2D debug layer?
                        foreach (var control in this.LightControlCollection)
                        {
                            if (control.Display && control.Image != null)
                            {
                                var image = control.Image();
                                if (image != null)
                                {
                                    this.renderTarget2d.DrawBitmap(
                                        image,
                                        new RawRectangleF(
                                            control.Extents.Left,
                                            control.Extents.Top,
                                            control.Extents.Right,
                                            control.Extents.Bottom),
                                        1.0f,
                                        BitmapInterpolationMode.Linear);
                                }
                            }
                        }

                        try
                        {
                            this.renderTarget2d.EndDraw();
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                        }                        
                    }, 
                    true);
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
                Debug.WriteLine("Starting timer.");
            }
        }

        private bool IsHoveringOverControls(Point point)
        {
            return this.LightControlCollection.Any(c =>
            {
                //// We want to be able to drag the window from the areas where the menu exists, but doesn't have any buttons.
                if (c is LightMenu)
                {
                    var menu = c as LightMenu;
                    return menu.Children.Any(mb => c is LightButton && mb.Extents.Contains(point));
                }

                return c is LightButton && ((LightButton)c).OnClick != null && c.Extents.Contains(point);
            });
        }

        private static Point GetPosition(IntPtr position)
        {
            return new Point(IntPtr.Size == 8 ? unchecked((int)position.ToInt64()) : position.ToInt32());

            //uint xy = unchecked(IntPtr.Size == 8 ? (uint)position.ToInt64() : (uint)position.ToInt32());
            //int x = unchecked((short)xy);
            //int y = unchecked((short)(xy >> 16));
            //return new Point(x, y);
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

                        clickableControl.OnClick(location);
                        return true;
                    }
                }
            }

            return false;
        }

        protected void ChangeUi()
        {
            
        }
    }
}
