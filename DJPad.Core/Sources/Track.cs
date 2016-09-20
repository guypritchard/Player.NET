namespace DJPad.Sources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using DJPad.Core;
    using DJPad.Core.Interfaces;

    public class Track : IFileSource
    {
        private IFileSource source;
        private IList<Sample> samples = new List<Sample>();
        private IList<int> index = new List<int>(); 
        private bool complete;
        private int playbackPosition;
        public string FileName { get; set; }

        public FormatInformation GetFormat()
        {
            return this.source.GetFormat();
        }

        public Sample GetSample(int dataRequested)
        {
            if (this.samples.Any())
            {
                lock (this.samples)
                {
                    var blockNumber = this.index.First(i => i >= this.playbackPosition);
                    var sampleNumber = this.index.IndexOf(blockNumber);

                    // is it the last sample?
                    if ((sampleNumber == this.index.Count - 1) && this.complete)
                    {
                        var lastSample = this.samples[sampleNumber];
                        var amountUntilEnd = lastSample.DataLength - (this.playbackPosition - blockNumber);
                        var sample = new Sample(amountUntilEnd);
                        Array.Copy(lastSample.Data, lastSample.DataLength - amountUntilEnd, sample.Data, 0, amountUntilEnd);
                        this.playbackPosition += amountUntilEnd;
                        return sample;
                    }
                }
            }

            return null;
        }

        public IMetadata GetMetadata()
        {
            return this.source.GetMetadata();
        }

        public string GetFileType()
        {
            return this.source.GetFileType();
        }

        public void Load(string filename)
        {
            var fileSource = SourceRegistry.ResolveSource(filename);
            fileSource.Load(filename);
            this.source = fileSource;
            this.Duration = fileSource.Duration;
            this.Position = TimeSpan.Zero;
            this.Init();
        }

        public void Close()
        {
            if (!this.complete)
            {
                lock (this.samples)
                {
                    this.complete = true;
                }
            }

            this.source.Close();
        }

        private void Init()
        {
            this.complete = false;
            ThreadPool.QueueUserWorkItem(b => this.Decrypt());
        }

        public bool EndOfFile
        {
            get { return false; }
        }

        private void Decrypt()
        {
            const int seconds = 1;
            var chunk = source.GetFormat().ToWaveFormat().SamplesPerSec * seconds;

            if (!source.GetMetadata().Duration.TotalSeconds.Equals(0.0d))
            {
                var sample = source.GetSample(chunk);

                while (!sample.IsEmpty && !this.complete)
                {
                    lock (this.samples)
                    {
                        this.samples.Add(sample.Clone());
                        this.index.Add(this.index.Last() + sample.DataLength);
                    }
                }
            }

            this.complete = true;
        }

        public TimeSpan Duration { get; private set; }

        public TimeSpan Position { get; set; }
    }
}
