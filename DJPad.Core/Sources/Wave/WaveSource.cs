namespace DJPad.Sources
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Input.Wave;

    public class WaveSource : IFileSource
    {
        private FormatInformation format;

        private WaveFileStream reader;

        public string GetFileType()
        {
            return ".WAV";
        }

        public FormatInformation GetFormat()
        {
            return this.format;
        }

        public TimeSpan Duration
        {
            get { return TimeSpan.FromSeconds(this.reader.Data.dSecLength); }
        }

        public TimeSpan Position
        {
            set
            {
                long seekPosition = (long)value.TotalSeconds * this.reader.Format.dwAvgBytesPerSec;
                this.reader.Position = seekPosition;
            }
            get
            {
                double currentSeconds = (double)this.reader.Position / this.reader.Format.dwAvgBytesPerSec;
                return TimeSpan.FromSeconds(currentSeconds);
            }
        }

        public Sample GetSample(int dataLength)
        {
            var sample = new Sample();

            sample.Data = this.reader.Read(dataLength);
            sample.TotalTime = this.Duration;
            sample.DataLength = sample.Data.Length;
            sample.Format = this.format;
            sample.DataOffset = this.reader.Position;
            sample.DataTotalLength = this.GetTotalLength();

            Debug.WriteLine("WaveSource: Read " + dataLength + " from " + this.FileName);

            return sample;
        }

        public long GetTotalLength()
        {
            return this.reader.Length;
        }

        public bool EndOfFile
        {
            get { return this.reader.Length == this.reader.Position; }
        }

        public void Load(string filename)
        {
            this.FileName = filename;
            this.reader = new WaveFileStream(this.FileName);
            this.reader.ParseHeadersAndPrepareToRead();

            this.format = new FormatInformation
                          {
                              Channels = this.reader.Format.wChannels,
                              SampleRate = (int) this.reader.Format.dwSamplesPerSec,
                              BytesPerSample = (ushort) (this.reader.Format.dwBitsPerSample/8)
                          };
        }

        public void Close()
        {
            if (this.reader != null)
            {
                this.reader.Close();
            }
        }

        public string FileName { get; set; }

        public IMetadata GetMetadata()
        {
            if (this.reader == null)
            {
                this.Load(this.FileName);
            }

            return new SimpleMetadataSource
                       {
                           Album = "Unknown",
                           Artist = "Unknown",
                           Title = Path.GetFileName(this.FileName),
                           Duration = this.Duration
                       };
        }
    }
}