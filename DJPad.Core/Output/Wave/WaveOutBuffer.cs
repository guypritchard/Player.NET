namespace DJPad.Output.Wave
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using DJPad.Core;

    public delegate void BufferFillEventHandler(IntPtr data, int size);

    public class WaveOutBuffer : IDisposable
    {
        #region Fields

        private readonly byte[] HeaderData;

        private readonly AutoResetEvent PlayEvent = new AutoResetEvent(false);

        private readonly IntPtr WaveOutHandle;

        private WaveNative.NativeWaveHeader Header;

        private GCHandle HeaderDataHandle;

        private GCHandle HeaderHandle;

        #endregion

        #region Constructors and Destructors

        public WaveOutBuffer(WaveFormat format, IntPtr waveOutHandle, int size)
        {
            this.Format = format;

            this.WaveOutHandle = waveOutHandle;

            this.HeaderHandle = GCHandle.Alloc(this.Header, GCHandleType.Pinned);
            this.Header.dwUser = (IntPtr)GCHandle.Alloc(this);
            this.HeaderData = new byte[size];
            this.HeaderDataHandle = GCHandle.Alloc(this.HeaderData, GCHandleType.Pinned);
            this.Header.lpData = this.HeaderDataHandle.AddrOfPinnedObject();
            this.Header.dwBufferLength = size;
            WaveOutHelper.Try(
                WaveNative.waveOutPrepareHeader(this.WaveOutHandle, ref this.Header, Marshal.SizeOf(this.Header)));
        }

        #endregion

        #region Public Properties

        public IntPtr Data
        {
            get
            {
                return this.Header.lpData;
            }
        }

        public WaveFormat Format { get; set; }

        public double LengthInSeconds
        {
            get
            {
                return this.Size / (double)this.Format.SamplesPerSec;
            }
        }

        public int Size
        {
            get
            {
                return this.Header.dwBufferLength;
            }
        }

        #endregion

        #region Properties

        private bool Playing { get; set; }

        #endregion

        #region Public Methods and Operators

        public void Dispose()
        {
            if (this.Header.lpData != IntPtr.Zero)
            {
                WaveNative.waveOutUnprepareHeader(this.WaveOutHandle, ref this.Header, Marshal.SizeOf(this.Header));
                this.HeaderHandle.Free();
                this.Header.lpData = IntPtr.Zero;
            }

            this.PlayEvent.Close();

            if (this.HeaderDataHandle.IsAllocated)
            {
                this.HeaderDataHandle.Free();
            }

            GC.SuppressFinalize(this);
        }

        public void OnCompleted()
        {
            this.PlayEvent.Set();
            this.Playing = false;
        }

        public bool Play()
        {
            lock (this)
            {
                this.PlayEvent.Reset();
                this.Playing = WaveNative.waveOutWrite(this.WaveOutHandle, ref this.Header, Marshal.SizeOf(this.Header))
                               == WaveNative.MMSYSERR_NOERROR;
                return this.Playing;
            }
        }

        public void Silence()
        {
            // zero out buffer
            byte v = this.Format.BitsPerSample == 8 ? (byte)128 : (byte)0;
            ;
            var b = new byte[this.Size];

            for (int i = 0; i < b.Length; i++)
            {
                b[i] = v;
            }

            Marshal.Copy(b, 0, this.Data, b.Length);
        }

        public void WaitFor()
        {
            if (this.Playing)
            {
                this.Playing = this.PlayEvent.WaitOne();
            }
            else
            {
                Thread.Sleep(0);
            }
        }

        #endregion

        #region Methods

        internal static void WaveOutProc(
            IntPtr hdrvr, int uMsg, int dwUser, ref WaveNative.NativeWaveHeader wavhdr, int dwParam2)
        {
            if (uMsg == WaveNative.MM_WOM_DONE)
            {
                try
                {
                    var h = (GCHandle)wavhdr.dwUser;
                    var buf = (WaveOutBuffer)h.Target;
                    buf.OnCompleted();
                }
                catch
                {
                }
            }
        }

        #endregion
    }
}