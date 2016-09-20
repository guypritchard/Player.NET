namespace DJPad.Output.Wave
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Lib;
    using DJPad.Output.Wave;

    public class WaveOut : BaseOutput, IAudioOutput, IDisposable, IStereoVolume
    {
        #region Constants

        private const int AudioBufferCount = 4;

        private const int AudioBufferLength = 8*1024;

        #endregion

        #region Fields

        private readonly WaveNative.WaveDelegate BufferProc = WaveOutBuffer.WaveOutProc;

        private readonly ConcurrentQueue<WaveOutBuffer> BufferQueue = new ConcurrentQueue<WaveOutBuffer>();

        private BufferFillEventHandler FillBufferEventHandler;

        private bool Finished;

        private Thread PlaybackThread;

        private double SecondsPlayed;

        private IntPtr WaveOutHandle;

        #endregion

        #region Public Properties

        public static int DeviceCount
        {
            get { return WaveNative.waveOutGetNumDevs(); }
        }


        public Status State
        {
            get { return this.Finished ? Status.Stopped : Status.Playing; }
        }

        #endregion

        #region Public Methods and Operators

        public void Play()
        {
            Initialize(
                -1,
                SampleSource.GetFormat().ToWaveFormat(),
                AudioBufferLength,
                AudioBufferCount,
                WaveOutFiller);

            Finished = false;
            PlaybackThread = new Thread(ThreadProc) {Name = "WaveOut Buffer Filler Thread"};
            PlaybackThread.Start();
        }

        public void Stop()
        {
            Finished = true;
            PlaybackThread.Join();
            DeInitialize();
            FireStoppedEvent(null);
        }

        public void Dispose()
        {
            if (PlaybackThread != null)
            {
                try
                {
                    Finished = true;
                    if (WaveOutHandle != IntPtr.Zero)
                    {
                        WaveNative.waveOutReset(WaveOutHandle);
                    }

                    PlaybackThread.Join();
                    FillBufferEventHandler = null;
                    FreeBufferQueue();

                    if (WaveOutHandle != IntPtr.Zero)
                    {
                        WaveNative.waveOutClose(WaveOutHandle);
                    }
                }
                finally
                {
                    PlaybackThread = null;
                    WaveOutHandle = IntPtr.Zero;
                }
            }

            GC.SuppressFinalize(this);
        }

        public long GetLengthPlayed()
        {
            return SamplesPlayed;
        }

        public TimeSpan GetSecondsPlayed()
        {
            return TimeSpan.FromSeconds(SecondsPlayed);
        }

        public IVisualisation Visualization { get; set; }

        public int Volume
        {
            get
            {
                throw new NotImplementedException();
            }

            set 
            {
                if (value > 100)
                {
                    value = 100;
                }

                var singleChannelVolume = (short)((float)ushort.MaxValue * (value / 100));
                int volume = singleChannelVolume;
                volume = volume << 16;
                volume += singleChannelVolume;

                WaveNative.waveOutSetVolume(WaveOutHandle, volume);
            }
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }
        
        public void DeInitialize()
        {
            WaveNative.waveOutClose(WaveOutHandle);
        }

        public void Initialize(
            int device, WaveFormat format, int bufferSize, int bufferCount, BufferFillEventHandler fillProc)
        {
            FillBufferEventHandler = fillProc;
            WaveOutHelper.Try(
                WaveNative.waveOutOpen(
                    out WaveOutHandle, device, format, BufferProc, 0, WaveNative.CALLBACK_FUNCTION));
            AllocateBufferQueue(format, bufferSize, bufferCount);
        }

        public void WaveOutFiller(IntPtr data, int size)
        {
            Sample s = SampleSource.GetSample(size);

            if (s.IsEmpty)
            {
                Debug.WriteLine("No more data.  Stopping...");
                Finished = true;
                ThreadPool.QueueUserWorkItem(FireFinishedEvent);
            }
            else
            {
                // Copy into the unmanaged buffer.
                Marshal.Copy(s.Data, 0, data, s.DataLength);
            }
        }

        #endregion

        #region Methods

        private void AllocateBufferQueue(WaveFormat format, int bufferSize, int bufferCount)
        {
            FreeBufferQueue();

            if (bufferCount > 0)
            {
                for (int i = 0; i < bufferCount; i++)
                {
                    var buffer = new WaveOutBuffer(format, WaveOutHandle, bufferSize);
                    BufferQueue.Enqueue(buffer);
                }
            }
        }

        private void FreeBufferQueue()
        {
            while (!BufferQueue.IsEmpty)
            {
                WaveOutBuffer buffer;
                if (BufferQueue.TryDequeue(out buffer))
                {
                    buffer.Dispose();
                }
            }
        }

        private void ThreadProc()
        {
            Clock.StartClock();

            while (!Finished)
            {
                WaveOutBuffer currentBuffer;

                BufferQueue.TryDequeue(out currentBuffer);
                currentBuffer.WaitFor();

                if (FillBufferEventHandler != null && !Finished)
                {
                    FillBufferEventHandler(currentBuffer.Data, currentBuffer.Size);
                }
                else
                {
                    currentBuffer.Silence();
                }

                SecondsPlayed += currentBuffer.LengthInSeconds;
                SamplesPlayed += currentBuffer.Size;

                currentBuffer.Play();

                BufferQueue.Enqueue(currentBuffer);
            }

            WaitForAllBuffers();
        }

        private void WaitForAllBuffers()
        {
            WaveOutBuffer currentBuffer;
            if (BufferQueue.TryDequeue(out currentBuffer))
            {
                currentBuffer.WaitFor();
                BufferQueue.Enqueue(currentBuffer);
            }
        }

        #endregion

        public FormatInformation GetFormat()
        {
            throw new NotImplementedException();
        }

        public Sample GetSample(int dataRequested)
        {
            throw new NotImplementedException();
        }
    }
}