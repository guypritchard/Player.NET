namespace DJPad.Core.Utils
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    /// <summary>
    /// The enumeration of possible modifiers.
    /// </summary>
    public enum MultimediaKey
    {
        Unknown = 0,
        NextTrack = 11,
        PreviousTrack = 12,
        Stop = 13,
        PlayPause = 14,
    }

    public class MultimediaKeyListener
    {
        // Registers a hot key with Windows.
        [DllImport("user32", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        // Unregisters the hot key with Windows.
        [DllImport("user32", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern int RegisterShellHookWindow(IntPtr hWnd);

        [DllImport("user32", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint RegisterWindowMessage(string lpString);

        const int HSHELL_APPCOMMAND = 0xC;

        public const int APPCOMMAND_MEDIA_NEXTTRACK = 11;
        public const int APPCOMMAND_MEDIA_PREVIOUSTRACK = 12;
        public const int APPCOMMAND_MEDIA_STOP = 13;
        public const int APPCOMMAND_MEDIA_PLAY_PAUSE = 14;
        private const int WM_TIMER = 0x0113;

        /// <summary>
        /// Represents the window that is used internally to get the messages.
        /// </summary>
        private class Window : NativeWindow, IDisposable
        {
            private readonly uint notify;

            public Window(uint notify)
            {
                this.notify = notify;

                // create the handle for the window.
                this.CreateHandle(new CreateParams());
            }

            /// <summary>
            /// Overridden to get the notifications.
            /// </summary>
            /// <param name="m"></param>
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_TIMER)
                {
                    Debug.WriteLine("Timer firing.");
                }

                base.WndProc(ref m);

                // check if we got a hot key pressed.
                if (m.Msg == notify && m.WParam.ToInt32() == HSHELL_APPCOMMAND)
                {
                    var messageValue = GetInt(m.LParam) >> 16;
                    switch (messageValue)
                    {
                        case APPCOMMAND_MEDIA_PLAY_PAUSE:
                        case APPCOMMAND_MEDIA_NEXTTRACK:
                        case APPCOMMAND_MEDIA_PREVIOUSTRACK:
                        case APPCOMMAND_MEDIA_STOP:
                            m.Result = (IntPtr)1;
                            if (this.MultimediaKeyPressed != null)
                            {
                                this.MultimediaKeyPressed(this, new KeyPressedEventArgs((MultimediaKey)messageValue));
                            }
                            break;
                    }
                }
            }

            public event EventHandler<KeyPressedEventArgs> MultimediaKeyPressed;

            #region IDisposable Members

            public void Dispose()
            {
                this.DestroyHandle();
            }

            #endregion
        }

        private readonly Window hiddenWindow;

        public MultimediaKeyListener()
        {

             // Hook on to the shell
           var msgNotify = RegisterWindowMessage("SHELLHOOK");
           
            this.hiddenWindow = new Window(msgNotify);
            RegisterShellHookWindow(this.hiddenWindow.Handle);

            // register the event of the inner native window.
            hiddenWindow.MultimediaKeyPressed += delegate(object sender, KeyPressedEventArgs args)
            {
                if (KeyPressed != null)
                {
                    KeyPressed(this, args);
                }
            };
        }

        private static int GetInt(IntPtr ptr)
        {
            return IntPtr.Size == 8 ? unchecked((int)ptr.ToInt64()) : ptr.ToInt32();
        }

        /// <summary>
        /// A hot key has been pressed.
        /// </summary>
        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        public void Dispose()
        {
            // dispose the inner native window.
            hiddenWindow.Dispose();
        }

        /// <summary>
        /// Event arguments for the event that is fired after the hot key has been pressed.
        /// </summary>
        public class KeyPressedEventArgs : EventArgs
        {
            private MultimediaKey key;

            internal KeyPressedEventArgs(MultimediaKey key)
            {
                this.key = key;
            }

            public MultimediaKey Key
            {
                get { return this.key; }
            }
        }
    }
}
