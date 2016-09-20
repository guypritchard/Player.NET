
namespace DJPad.Sources
{
    using System;
    using System.IO;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using DJPad.Sources;

    public class NullSource : IFileSource
    {
        public string GetFileType()
        {
            return string.Empty;
        }

        public void Load(string filename)
        {
        }

        public void Close()
        {
        }

        public TimeSpan Duration
        {
            get { return TimeSpan.Zero;  }
        }

        public TimeSpan Position
        {
            get
            {
                return TimeSpan.Zero;
            }
            set
            {
            }
        }

        public string FileName { get; set; }

        public bool EndOfFile
        {
            get { return true; }
        }

        public FormatInformation GetFormat()
        {
            return new FormatInformation();
        }

        public Sample GetSample(int dataRequested)
        {
            return new Sample(0);
        }

        public IMetadata GetMetadata()
        {
            return new SimpleMetadataSource()
            {
                Title = Path.GetFileName(this.FileName),
                Album = Path.GetDirectoryName(this.FileName)
            };
        }
    }
}
