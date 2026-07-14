namespace DJPad.Output.DirectSound
{
    using System;
    using System.Threading;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Lib;
    using NAudio.Wave;
    using NAudioWaveFormat = NAudio.Wave.WaveFormat;

    public class DirectSoundOut : BaseOutput, IAudioOutput, IDisposable
    {
        private const int VisualizationBufferMilliseconds = 1000;

        private readonly object visualisationLock = new object();

        private readonly object transitionLock = new object();

        private byte[] visualisationBuffer = new byte[0];

        private int visualisationWritePosition;

        private WaveOutEvent outputDevice;

        private PullWaveProvider waveProvider;

        private FormatInformation upstreamFormat;

        private bool finishedSignaled;

        private int currentVolume = 100;

        private ISampleSource outgoingSource;

        private int transitionBytesRemaining;

        private int transitionBytesTotal;

        private Action transitionComplete;

        public DirectSoundOut()
            : this(IntPtr.Zero)
        {
        }

        public DirectSoundOut(IntPtr playerHwnd)
        {
            this.State = Status.Stopped;
        }

        public Status State { get; private set; }

        public long SourceOffset { get; set; }

        public long SourceTotalLength { get; set; }

        public TimeSpan TotalTime { get; set; }

        public void Play()
        {
            if (this.SampleSource == null)
            {
                throw new InvalidOperationException("No audio source defined.");
            }

            this.DisposeOutput();
            this.Init();
            this.State = Status.Playing;
            this.outputDevice.Play();
        }

        public void Play(bool blockUntilFinished)
        {
            this.Play();

            if (blockUntilFinished)
            {
                using (var waitUntilFinished = new ManualResetEvent(false))
                {
                    this.FinishedEvent += (source, args) => waitUntilFinished.Set();
                    waitUntilFinished.WaitOne();
                }
            }
        }

        public void Stop()
        {
            if (this.outputDevice == null)
            {
                return;
            }

            this.State = Status.Stopped;
            this.outputDevice.Stop();
            this.DisposeOutput();
            this.CompleteTransition();
            this.FireStoppedEvent(null);
        }

        public bool TryTransitionTo(ISampleSource source, Action completed, int durationMilliseconds = 250)
        {
            if (source == null || this.SampleSource == null || this.State != Status.Playing)
            {
                return false;
            }

            var currentFormat = this.SampleSource.GetFormat();
            var nextFormat = source.GetFormat();
            if (currentFormat.BytesPerSample != 2 || nextFormat.BytesPerSample != 2
                || currentFormat.SampleRate != nextFormat.SampleRate
                || currentFormat.Channels != nextFormat.Channels)
            {
                return false;
            }

            this.CompleteTransition();
            lock (this.transitionLock)
            {
                this.outgoingSource = this.SampleSource;
                this.SampleSource = source;
                var frameSize = nextFormat.BytesPerSample * nextFormat.Channels;
                this.transitionBytesTotal = Math.Max(frameSize,
                    nextFormat.SamplesPerSecond * durationMilliseconds / 1000 / frameSize * frameSize);
                this.transitionBytesRemaining = this.transitionBytesTotal;
                this.transitionComplete = completed;
            }
            return true;
        }

        public void Flush()
        {
            if (this.State == Status.Playing)
            {
                this.Stop();
                this.Play();
            }
        }

        public FormatInformation GetFormat()
        {
            return this.SampleSource.GetFormat();
        }

        public Sample GetSample(int dataRequested)
        {
            if (this.visualisationBuffer.Length == 0)
            {
                return null;
            }

            var sample = new Sample(dataRequested)
            {
                Format = this.GetFormat(),
                TotalTime = this.TotalTime,
                DataOffset = this.SourceOffset,
                DataTotalLength = this.SourceTotalLength
            };

            lock (this.visualisationLock)
            {
                var bytesToCopy = Math.Min(dataRequested, this.visualisationBuffer.Length);
                var readPosition = this.visualisationWritePosition - bytesToCopy;
                if (readPosition < 0)
                {
                    readPosition += this.visualisationBuffer.Length;
                }

                if (readPosition + bytesToCopy <= this.visualisationBuffer.Length)
                {
                    Array.Copy(this.visualisationBuffer, readPosition, sample.Data, 0, bytesToCopy);
                }
                else
                {
                    var firstChunk = this.visualisationBuffer.Length - readPosition;
                    Array.Copy(this.visualisationBuffer, readPosition, sample.Data, 0, firstChunk);
                    Array.Copy(this.visualisationBuffer, 0, sample.Data, firstChunk, bytesToCopy - firstChunk);
                }
            }

            return sample;
        }

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

                this.currentVolume = value;

                if (this.outputDevice != null)
                {
                    this.outputDevice.Volume = value / 100.0f;
                }
            }
        }

        public void Dispose()
        {
            this.DisposeOutput();
            GC.SuppressFinalize(this);
        }

        private void Init()
        {
            var sample = this.SampleSource.GetSample(0);
            this.upstreamFormat = sample.Format;
            this.finishedSignaled = false;
            this.SamplesPlayed = 0;
            this.SourceOffset = 0;
            this.SourceTotalLength = 0;
            this.TotalTime = TimeSpan.Zero;

            var bytesPerSecond = this.upstreamFormat.SamplesPerSecond;
            this.visualisationBuffer = new byte[bytesPerSecond * VisualizationBufferMilliseconds / 1000];
            this.visualisationWritePosition = 0;

            this.waveProvider = new PullWaveProvider(this, this.upstreamFormat);
            this.outputDevice = new WaveOutEvent
            {
                DesiredLatency = 100,
                NumberOfBuffers = 4,
                Volume = this.currentVolume / 100.0f
            };
            this.outputDevice.PlaybackStopped += this.OnPlaybackStopped;
            this.outputDevice.Init(this.waveProvider);
        }

        private Sample ReadFromSource(int bytesRequested)
        {
            Action completed = null;
            Sample sample;
            lock (this.transitionLock)
            {
                sample = this.SampleSource.GetSample(bytesRequested);
                if (this.outgoingSource != null)
                {
                    var outgoing = this.outgoingSource.GetSample(bytesRequested);
                    this.MixTransition(sample, outgoing);
                    this.transitionBytesRemaining -= sample == null ? bytesRequested : sample.DataLength;
                    if (this.transitionBytesRemaining <= 0)
                    {
                        this.outgoingSource = null;
                        completed = this.transitionComplete;
                        this.transitionComplete = null;
                    }
                }
            }

            if (completed != null)
            {
                completed();
            }

            if (sample == null || sample.IsEmpty)
            {
                return sample;
            }

            this.TotalTime = sample.TotalTime;
            this.SourceOffset = sample.DataOffset;
            this.SourceTotalLength = sample.DataTotalLength;
            this.SamplesPlayed += sample.DataLength;
            this.WriteVisualisationData(sample.Data, sample.DataLength);

            return sample;
        }

        private void MixTransition(Sample incoming, Sample outgoing)
        {
            if (incoming == null || incoming.IsEmpty)
            {
                return;
            }

            var outgoingLength = outgoing == null ? 0 : outgoing.DataLength;
            for (var offset = 0; offset < incoming.DataLength - 1; offset += 2)
            {
                var bytesElapsed = this.transitionBytesTotal - this.transitionBytesRemaining + offset;
                var incomingLevel = Math.Min(1.0f, bytesElapsed / (float)this.transitionBytesTotal);
                var incomingValue = BitConverter.ToInt16(incoming.Data, offset);
                var outgoingValue = offset < outgoingLength - 1 ? BitConverter.ToInt16(outgoing.Data, offset) : 0;
                var mixed = (int)((outgoingValue * (1.0f - incomingLevel)) + (incomingValue * incomingLevel));
                var encoded = BitConverter.GetBytes((short)Math.Max(short.MinValue, Math.Min(short.MaxValue, mixed)));
                incoming.Data[offset] = encoded[0];
                incoming.Data[offset + 1] = encoded[1];
            }
        }

        private void CompleteTransition()
        {
            Action completed;
            lock (this.transitionLock)
            {
                this.outgoingSource = null;
                this.transitionBytesRemaining = 0;
                completed = this.transitionComplete;
                this.transitionComplete = null;
            }
            if (completed != null)
            {
                completed();
            }
        }

        private void WriteVisualisationData(byte[] data, int length)
        {
            if (this.visualisationBuffer.Length == 0 || length == 0)
            {
                return;
            }

            lock (this.visualisationLock)
            {
                var sourceOffset = Math.Max(0, length - this.visualisationBuffer.Length);
                var remaining = length - sourceOffset;

                while (remaining > 0)
                {
                    var writable = Math.Min(remaining, this.visualisationBuffer.Length - this.visualisationWritePosition);
                    Array.Copy(data, sourceOffset, this.visualisationBuffer, this.visualisationWritePosition, writable);
                    this.visualisationWritePosition = (this.visualisationWritePosition + writable) % this.visualisationBuffer.Length;
                    sourceOffset += writable;
                    remaining -= writable;
                }
            }
        }

        private void SignalFinished()
        {
            if (this.finishedSignaled || this.State == Status.Stopped)
            {
                return;
            }

            this.finishedSignaled = true;
            this.State = Status.Stopped;
            this.FireFinishedEvent(null);
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs args)
        {
            if (ReferenceEquals(sender, this.outputDevice))
            {
                this.SignalFinished();
            }
        }

        private void DisposeOutput()
        {
            if (this.outputDevice != null)
            {
                var device = this.outputDevice;
                this.outputDevice = null;
                device.PlaybackStopped -= this.OnPlaybackStopped;
                device.Dispose();
            }

            this.waveProvider = null;
        }

        private sealed class PullWaveProvider : IWaveProvider
        {
            private readonly DirectSoundOut owner;

            public PullWaveProvider(DirectSoundOut owner, FormatInformation format)
            {
                this.owner = owner;
                this.WaveFormat = new NAudioWaveFormat(format.SampleRate, format.BytesPerSample * 8, format.Channels);
            }

            public NAudioWaveFormat WaveFormat { get; private set; }

            public int Read(byte[] buffer, int offset, int count)
            {
                var sample = this.owner.ReadFromSource(count);
                if (sample == null || sample.IsEmpty)
                {
                    this.owner.SignalFinished();
                    return 0;
                }

                Array.Copy(sample.Data, 0, buffer, offset, sample.DataLength);
                return sample.DataLength;
            }
        }
    }
}
