namespace DJPad.Sources
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using NAudio.Wave;
    using WindowsMediaLib;

    class WmaSource : IFileSource
    {
        private IMetadata metadata;
        private MediaFoundationReader reader;

        public string FileName { get; set; }

        public bool EndOfFile => this.reader != null && this.reader.Position >= this.reader.Length;

        public TimeSpan Duration
        {
            get
            {
                this.EnsureReader();
                return this.reader.TotalTime;
            }
        }

        public TimeSpan Position
        {
            get
            {
                this.EnsureReader();
                return this.reader.CurrentTime;
            }
            set
            {
                this.EnsureReader();
                this.reader.CurrentTime = value;
            }
        }

        public string GetFileType() => ".WMA";

        public void Load(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException(filename);
            }

            this.Close();
            this.FileName = filename;
            this.metadata = null;
        }

        public void Close()
        {
            this.reader?.Dispose();
            this.reader = null;
        }

        public FormatInformation GetFormat()
        {
            this.EnsureReader();
            return new FormatInformation
            {
                BytesPerSample = (ushort)(this.reader.WaveFormat.BitsPerSample / 8),
                Channels = this.reader.WaveFormat.Channels,
                SampleRate = this.reader.WaveFormat.SampleRate
            };
        }

        public Sample GetSample(int dataRequested)
        {
            this.EnsureReader();
            var sample = new Sample(dataRequested)
            {
                Format = this.GetFormat(),
                TotalTime = this.reader.TotalTime,
                DataOffset = this.reader.Position,
                DataTotalLength = this.reader.Length
            };

            if (dataRequested == 0)
            {
                return sample;
            }

            var bytesRead = this.reader.Read(sample.Data, 0, dataRequested);
            if (bytesRead != dataRequested)
            {
                sample.Resize(bytesRead);
            }

            sample.DataOffset = this.reader.Position;
            return sample;
        }

        public IMetadata GetMetadata()
        {
            if (this.metadata == null)
            {
                try
                {
                    WMUtils.WMCreateSyncReader(IntPtr.Zero, 0, out var metadataReader);
                    metadataReader.Open(this.FileName);
                    this.metadata = new SimpleMetadataSource(new WmaMetadataSource((IWMHeaderInfo3)metadataReader, this.FileName));
                    metadataReader.Close();
                }
                catch (COMException)
                {
                    this.metadata = new SimpleMetadataSource
                    {
                        Title = Path.GetFileNameWithoutExtension(this.FileName),
                        Duration = this.Duration
                    };
                }
            }

            return this.metadata;
        }

        private void EnsureReader()
        {
            if (this.reader == null)
            {
                this.reader = new MediaFoundationReader(this.FileName, new MediaFoundationReader.MediaFoundationReaderSettings
                {
                    RequestFloatOutput = false
                });
            }
        }
    }
}
