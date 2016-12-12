using DJPad.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DJPad.Core.Lib
{
    public class BufferedSource : IFileSource
    {
        private IFileSource source;

        public BufferedSource(IFileSource source)
        {
            this.source = source;
        }

        public TimeSpan Duration
        {
            get
            {
                return source.Duration;
            }
        }

        public bool EndOfFile
        {
            get
            {
                return this.source.EndOfFile;
            }
        }

        public string FileName
        {
            get
            {
                return this.source.FileName;
            }

            set
            {
                this.source.FileName = value;
            }
        }

        public TimeSpan Position
        {
            get
            {
                return this.source.Position;
            }

            set
            {
            }
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public string GetFileType()
        {
            throw new NotImplementedException();
        }

        public FormatInformation GetFormat()
        {
            throw new NotImplementedException();
        }

        public IMetadata GetMetadata()
        {
            throw new NotImplementedException();
        }

        public Sample GetSample(int dataRequested)
        {
            throw new NotImplementedException();
        }

        public void Load(string filename)
        {
            throw new NotImplementedException();
        }
    }
}
