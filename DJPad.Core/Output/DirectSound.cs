namespace DJPad.Output.DirectSound
{
    using System;
    using System.Threading;
    using System.Timers;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Formats.Wave;
    using DJPad.Lib;
    using SharpDX.DirectSound;
    using Timer = System.Timers.Timer;
    using WaveFormat = SharpDX.Multimedia.WaveFormat;
    using System.Diagnostics;

    public class DirectSoundOut : BaseOutput, IAudioOutput
    {
        private bool Enabled { get; set; }
        private double Interval { get; set; }

        private Thread fillThread;
        
        private const int MaxVolume = 10000;
        private int currentVolume;

        private readonly IntPtr formWindow;

        private readonly TimeSpan playbackBufferLength = TimeSpan.FromMilliseconds(1000);

        private byte[] visualisationBuffer;

        private SecondarySoundBuffer buffer;

        private int bufferSize;

        private ManualResetEvent waitUntilFinished;

        private FormatInformation upstreamFormat { get; set; }

        public long SourceOffset { get; set; }
        public long SourceTotalLength { get; set; }

        private NotificationPosition[] notifications;

        private bool firstWrite = true;

        public DirectSoundOut()
            : this(WindowsNative.GetDesktopWindow())
        {
        }

        public DirectSoundOut(IntPtr playerHwnd)
        {
            formWindow = playerHwnd;
            this.fillThread = new Thread(this.ThreadFunc);
            this.fillThread.SetApartmentState(ApartmentState.STA);
            this.fillThread.IsBackground = true;
            this.fillThread.Start();
            this.Interval = playbackBufferLength.TotalMilliseconds / 4;
            this.State = Status.Stopped;
        }

        private int NextWritePosition { get; set; }

        public Status State
        {
            get; private set;
        }

        public void Play()
        {
            if (this.SampleSource == null)
            {
                throw new InvalidOperationException("No audio source defined.");
            }

            this.firstWrite = true;
            this.Init();
            this.Enabled = true;
            this.buffer.Play(0, PlayFlags.Looping);
            this.State = Status.Playing;
        }

        public void Stop()
        {
            if (this.buffer != null)
            {
                this.Enabled = false;
                this.buffer.Stop();
                this.State = Status.Stopped;
                FireStoppedEvent(null);
            }
        }

        public void Flush()
        {
            this.Reset();
        }

        public void Filler(int playedLength)
        {
            if (playedLength == 0)
            {
                return;
            }

            Sample s = SampleSource.GetSample(playedLength);
            this.TotalTime = s.TotalTime;
            this.SourceOffset = s.DataOffset;
            this.SourceTotalLength = s.DataTotalLength;

            if (s.IsEmpty)
            {
                if (this.State == Status.Playing || this.State == Status.Buffering)
                {
                     // Play silence into the buffer and signal as stopping...
                     Write(new Sample(playedLength).Data);
                     this.State = Status.Stopping;
                }
                else
                {
                    this.State = Status.Stopped;
                    FireFinishedEvent(null);
                }
            }
            else
            {
                this.Enabled = false;
                Write(s.Data);
                this.SamplesPlayed += s.DataLength;
                this.Enabled = true;
            }
        }

        public int GetPlayedLength()
        {
            if (buffer == null)
            {
                return 0;
            }

            int playPosition;
            int writePosition;

            buffer.GetCurrentPosition(out writePosition, out playPosition);

            if (playPosition < NextWritePosition)
            {
                return (playPosition + this.bufferSize) - NextWritePosition;
            }

            return playPosition - NextWritePosition;
        }

        public void Play(bool blockUntilFinished)
        {
            Play();

            if (blockUntilFinished)
            {
                waitUntilFinished = new ManualResetEvent(false);
                FinishedEvent += (source, args) => waitUntilFinished.Set();
                waitUntilFinished.WaitOne();
            }
        }

        private void Reset()
        {
            if (buffer == null) return;

            this.Enabled = false;
            this.buffer.Stop();
            this.buffer.CurrentPosition = this.NextWritePosition;
            Filler(bufferSize);
            buffer.Play(0, PlayFlags.Looping);
            this.State = Status.Playing;
        }

        public void Write(byte[] data)
        {
            // Don't write anything if we've stopped.
            if (buffer == null || this.State == Status.Stopped || data.Length == 0)
            {
                return;
            }

            if ((NextWritePosition + data.Length) <= bufferSize)
            {
                buffer.Write(data, NextWritePosition, LockFlags.None);
                lock (this.visualisationBuffer)
                {
                    Array.Copy(data, 0, visualisationBuffer, NextWritePosition, data.Length);
                }
            }
            else
            {
                int toWrite = bufferSize - NextWritePosition;
                buffer.Write(data, 0, toWrite, NextWritePosition, LockFlags.None);
                buffer.Write(data, toWrite, data.Length - toWrite, 0, LockFlags.None);

                lock (this.visualisationBuffer)
                {
                    Array.Copy(data, 0, visualisationBuffer, NextWritePosition, toWrite);
                    Array.Copy(data, toWrite, visualisationBuffer, 0, data.Length - toWrite);
                }
            }

            NextWritePosition += data.Length;

            // Now fix up the write position if we've wrapped around.
            if (NextWritePosition >= bufferSize)
            {
                NextWritePosition += -bufferSize;
            }
        }

        private void ThreadFunc(object o)
        {
            while (true)
            {
                if (this.Enabled)
                {
                    FillTimer_Elapsed(null, null);
                }

                Thread.Sleep((int)this.Interval);
            }
        }


        private void FillTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.State == Status.Stopped)
            {
                return;
            }

            if (this.firstWrite)
            {
                this.State = Status.Buffering;
                Filler(this.bufferSize);
                this.firstWrite = false;
            }
            else
            {
                Filler(GetPlayedLength());
            }
        }

        private void Init()
        {
            Sample s = SampleSource.GetSample(0);
            upstreamFormat = s.Format;

            var devices = DirectSound.GetDevices();

            var directSound = new DirectSound();

            directSound.SetCooperativeLevel(formWindow, CooperativeLevel.Priority);

            // Create PrimarySoundBuffer
            var primaryBufferDesc = new SoundBufferDescription
                                    {
                                        Flags = BufferFlags.PrimaryBuffer,
                                        AlgorithmFor3D = Guid.Empty,
                                    };

            var primarySoundBuffer = new PrimarySoundBuffer(directSound, primaryBufferDesc);

            // Play the PrimarySound Buffer
            primarySoundBuffer.Play(0, PlayFlags.Looping);

            // Default WaveFormat Stereo 44100 16 bit
            WaveFormat waveFormat = upstreamFormat != null
                ? new WaveFormat(upstreamFormat.SampleRate,
                                 upstreamFormat.BytesPerSample * 8,
                                 upstreamFormat.Channels)
                : new WaveFormat();

            // Create SecondarySoundBuffer
            var secondaryBufferDesc = new SoundBufferDescription
                                      {
                                          BufferBytes = waveFormat.ConvertLatencyToByteSize((int)playbackBufferLength.TotalMilliseconds),
                                          Format = waveFormat,
                                          Flags = BufferFlags.GetCurrentPosition2
                                                | BufferFlags.ControlPositionNotify
                                                | BufferFlags.GlobalFocus
                                                | BufferFlags.ControlVolume
                                                | BufferFlags.StickyFocus
                                      };

            this.buffer = new SecondarySoundBuffer(directSound, secondaryBufferDesc) { Volume = this.Volume };
            this.bufferSize = buffer.Capabilities.BufferBytes;
            this.NextWritePosition = 0;

            notifications = new[]
            {
                new NotificationPosition { Offset = bufferSize - 1, WaitHandle = new ManualResetEvent(false)},
                new NotificationPosition { Offset = (bufferSize / 2) + 1, WaitHandle = new ManualResetEvent(false)},
            };

            this.buffer.SetNotificationPositions(notifications);
            this.visualisationBuffer = new byte[bufferSize];
        }

        public FormatInformation GetFormat()
        {
            return this.SampleSource.GetFormat();
        }

        public Sample GetSample(int dataRequested)
        {
            if (buffer == null)
            {
                return null;
            }

            int playPosition;
            int writePosition;

            buffer.GetCurrentPosition(out writePosition, out playPosition);

            var s = new Sample(dataRequested)
                    {
                        Format = this.GetFormat(),
                        TotalTime = this.TotalTime,
                        DataOffset = this.SourceOffset,
                        DataTotalLength = this.SourceTotalLength
                    };

            lock (this.visualisationBuffer)
            {
                if (playPosition + dataRequested < this.visualisationBuffer.Length)
                {
                    Array.Copy(this.visualisationBuffer, playPosition, s.Data, 0, dataRequested);
                }
                else
                {
                    var toWrite = this.visualisationBuffer.Length - playPosition;
                    Array.Copy(this.visualisationBuffer, playPosition, s.Data, 0, toWrite);
                    Array.Copy(this.visualisationBuffer, 0, s.Data, toWrite, dataRequested - toWrite);
                }
            }

            return s;
        }

        public TimeSpan TotalTime { get; set; }

        public int Volume
        {
            get
            {
                return this.currentVolume;
            }

            set
            {
                if (value > 100 || value < 0)
                {
                    throw new ArgumentException("Invalid volume, must be 0-100.");
                }

                this.currentVolume = value * 100 - MaxVolume;

                if (this.buffer == null)
                {
                    return;
                }

                this.buffer.Volume = this.currentVolume;
            }
        }
    }
}