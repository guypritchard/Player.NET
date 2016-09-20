namespace DJPad.Core.Mixer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using DJPad.Core;
    using DJPad.Core.Interfaces;

    public class Mixer : ISampleSource
    {
        #region Constants

        private const int DefaultBuffer = 512 * 1024;

        #endregion

        #region Fields

        private readonly int mixBufferLength;

        private readonly byte[] mixerBuffer;

        private readonly FormatInformation mixerFormat;

        private readonly int refillBufferAt;

        public List<ISampleSource> AddSources = new List<ISampleSource>();

        public List<ISampleSource> RemoveSources = new List<ISampleSource>();

        public List<ISampleSource> Sources = new List<ISampleSource>();

        private int dataRemaining;

        private int playCursor;

        #endregion

        #region Constructors and Destructors

        public Mixer(int bufferLength)
        {
            this.mixerFormat = new FormatInformation { Channels = 2, SampleRate = 44100, BytesPerSample = 2 };

            this.mixBufferLength = bufferLength;
            this.mixerBuffer = new byte[bufferLength];

            // Request a refill if we're half empty.
            this.refillBufferAt = this.mixBufferLength / 2;
        }

        public Mixer()
            : this(DefaultBuffer)
        {
        }

        #endregion

        #region Public Methods and Operators

        public FormatInformation GetFormat()
        {
            return this.mixerFormat;
        }

        public Sample GetSample(int dataLength)
        {
            if (this.dataRemaining <= this.refillBufferAt)
            {
                this.MaintainBuffer();
            }

            if (this.playCursor >= this.mixBufferLength)
            {
                // Wrap play cursor around.
                this.playCursor = 0;
            }

            int returnLength = dataLength < this.mixBufferLength ? dataLength : this.mixBufferLength;

            var s = new Sample(returnLength);

            Array.Copy(this.mixerBuffer, this.playCursor, s.Data, 0, returnLength);

            this.playCursor += returnLength;
            this.dataRemaining -= returnLength;

            return s;
        }

        public void AddSource(ISampleSource source)
        {
            this.AddSources.Add(source);
        }

        public void MaintainSources()
        {
            this.RemoveSources.ForEach(s => this.Sources.Remove(s));
            this.Sources.AddRange(this.AddSources);
            this.RemoveSources.Clear();
            this.AddSources.Clear();
        }

        public void RemoveSource(ISampleSource source)
        {
            this.RemoveSources.Add(source);
        }

        #endregion

        #region Methods

        private void MaintainBuffer()
        {
            int dataWritten = 0;

            while (this.dataRemaining < this.mixBufferLength)
            {
                int requestBytes = this.mixBufferLength - this.dataRemaining;
                IEnumerable<Sample> samples = this.Sources.Select(t => t.GetSample(requestBytes));

                Sample upstreamSample = this.MixSamples(samples.ToArray());

                if (upstreamSample.DataLength == 0)
                {
                    Debug.WriteLine("Upstream returning empty - filling buffer with silence.");
                    upstreamSample = new Sample(requestBytes);
                }

                this.dataRemaining += upstreamSample.DataLength;

                if (this.playCursor <= this.refillBufferAt)
                {
                    Array.Copy(upstreamSample.Data, 0, this.mixerBuffer, 0 + dataWritten, upstreamSample.DataLength);
                }
                else
                {
                    Array.Copy(
                        upstreamSample.Data,
                        0,
                        this.mixerBuffer,
                        this.refillBufferAt + dataWritten,
                        upstreamSample.DataLength);
                }

                dataWritten += upstreamSample.DataLength;
                Debug.WriteLine(
                    string.Format(
                        "Refilling {0} at {1} PlayPosition:{2}", upstreamSample.DataLength, dataWritten, this.playCursor));
            }
        }

        private Sample MixSamples(Sample[] samples)
        {
            if (samples.Length == 1)
            {
                return samples[0];
            }

            var mixedSamples = new Sample(samples.Max(s => s.DataLength));

            mixedSamples.Transform16(() => (short)samples.Average(s => s.Sample16BitStereo().First()));

            return mixedSamples;
        }

        #endregion
    }
}