namespace DJPad.Sources
{
    using System;
    using System.IO;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Sources;

    using Mp3Sharp;
    using System.Diagnostics;

    public class Mp3Source : IFileSource
    {
        private IMetadata metadata;
        private Lazy<Mp3Stream> mp3Stream;
        private FormatInformation format;

        public string FileName { get; set; }

        public long GetPosition()
        {
            return this.mp3Stream.Value.Position;
        }

        public string GetFileType()
        {
            return ".MP3";
        }

        public FormatInformation GetFormat()
        {
            if (this.format == null)
            {
                return new FormatInformation
                {
                    BytesPerSample = 2, 
                    Channels = this.mp3Stream.Value.ChannelCount, 
                    SampleRate = this.mp3Stream.Value.Frequency,
                };
            }

            return this.format;                       
        }

        public IMetadata GetMetadata()
        {
            if (this.metadata == null)
            {
                if (this.mp3Stream.Value.Position == 0)
                {
                    this.mp3Stream.Value.PreRoll();
                }

                this.metadata = new Id3MetadataSource(this.mp3Stream.Value.Id3Frames, this.FileName);
            }

            return new SimpleMetadataSource(this.metadata) { Duration = this.Duration };
        }
        
        public Mp3Source()
        {
            this.mp3Stream = new Lazy<Mp3Stream>(() =>
            {
                var mp3Stream = new Mp3Stream(this.FileName);
                this.FirstRead = false;
                return mp3Stream;
            });
        }

        public void Load(string fileName)
        {
            this.FileName = fileName;
            if (!File.Exists(this.FileName))
            {
                throw new FileNotFoundException(this.FileName);
            }
        }
        
        /// <summary>
        ///     Gets a sample containing uncompressed data.
        /// </summary>
        /// <param name="dataRequested">Amount of data requested.</param>
        /// <returns>A sample containing data (may or may not be the size we requested.)</returns>
        public Sample GetSample(int dataRequested)
        {
            var s = new Sample(dataRequested);

            int dataRead = this.mp3Stream.Value.Read(s.Data, 0, dataRequested);
            
            if (!this.FirstRead)
            {
                this.format = new FormatInformation() {
                    Channels = this.mp3Stream.Value.Format == SoundFormat.Pcm16BitStereo ? 2 : 1,
                    SampleRate = this.mp3Stream.Value.Frequency, 
                    BytesPerSample = 2 
                };
                this.FirstRead = true;
            }
            
            if (dataRead != dataRequested)
            {
                s.Resize(dataRead);
            }

            s.Format = this.GetFormat();
            s.DataOffset = this.mp3Stream.Value.Position - this.mp3Stream.Value.PlaybackStartPosition;
            s.DataTotalLength = this.mp3Stream.Value.Length - this.mp3Stream.Value.PlaybackStartPosition;
            s.TotalTime = this.Duration;

            return s;
        }

        public long GetTotalLength()
        {
            return this.mp3Stream.Value.Length;
        }

        public bool EndOfFile
        {
            get { return this.mp3Stream.Value.Length == this.mp3Stream.Value.Position; }
        }

        public void Close()
        {
            if (this.mp3Stream.IsValueCreated)
            {
                this.mp3Stream.Value.Close();
                this.FirstRead = false;
            }
        }

        public bool FirstRead { get; set; }

        public TimeSpan Duration
        {
            get { return TimeSpan.FromSeconds(this.mp3Stream.Value.DurationSeconds); }
        }

        public TimeSpan Position
        {
            set
            {
                var percentage = value.TotalMilliseconds/this.Duration.TotalMilliseconds;

                long seekPosition = 0;
                if (!double.IsNaN(percentage))
                {
                     seekPosition = (long)((this.mp3Stream.Value.Length - this.mp3Stream.Value.PlaybackStartPosition) * percentage);
                }

                Debug.WriteLine("Seeking to {0} {1:0.0}% at {2} out of {3} offset of {4}", value, percentage * 100, seekPosition, this.mp3Stream.Value.Length, this.mp3Stream.Value.PlaybackStartPosition);
                this.mp3Stream.Value.Seek(seekPosition + this.mp3Stream.Value.PlaybackStartPosition, SeekOrigin.Begin);
            }

            get
            {
                return
                    TimeSpan.FromSeconds(this.mp3Stream.Value.DurationSeconds *
                                         ((double)(this.mp3Stream.Value.Position - this.mp3Stream.Value.PlaybackStartPosition) /
                                          (double)(this.mp3Stream.Value.Length - this.mp3Stream.Value.PlaybackStartPosition)));
            }
        }
    }
}