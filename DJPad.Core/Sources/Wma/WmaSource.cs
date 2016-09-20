namespace DJPad.Sources
{
    using System.Threading;
    using System.Windows.Threading;
    using DJPad.Core;
    using DJPad.Core.Interfaces;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using WindowsMediaLib;
    using System.Diagnostics;

    // http://www.freecovers.net/
    internal class SampleData
    {
        public SampleData(int size = 0)
        {
            this.Data = new byte[size];
        }

        public byte[] Data { get; set; }
        public long SampleTime { get; set; }

        public void Combine(SampleData data)
        {
            this.SampleTime = data.SampleTime;
            this.Data = this.Combine(this.Data, data.Data);
        }

        private byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays) {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
    }


    class WmaSource : IFileSource
    {
        private IMetadata metadata;
        private IWMSyncReader reader;
        private List<SampleData> readData = new List<SampleData>();

        public string GetFileType()
        {
            return ".WMA";
        }

        public void Load(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException(filename);
            }

            this.EndOfFile = false;
            this.FileName = filename;
            this.currentPosition = TimeSpan.Zero;
        }

        public void Close()
        {
            //if (this.reader != null)
            //{
            //    this.reader.Dispose();
            //}
        }

        public TimeSpan Duration
        {
            get 
            {
                return this.GetMetadata().Duration;    
            }
        }

        private TimeSpan currentPosition;
        private bool changePosition;

        public TimeSpan Position 
        {
            get { return this.currentPosition; }
            set
            {
                this.changePosition = true;
                this.currentPosition = value;
            }
        }

        public string FileName
        {
            get; set;
        }

        public bool EndOfFile { 
            get;
            private set;
        }

        public FormatInformation GetFormat()
        {
            return new FormatInformation { BytesPerSample = 2, Channels = 2, SampleRate = 44100 };
        }

        public Sample GetSample(int dataRequested)
        {
            var returnSample = new Sample();
            returnSample.Format = this.GetFormat();
            returnSample.TotalTime = this.metadata.Duration;

            if (dataRequested > 0)
            {
                if (this.reader == null)
                {
                    WMUtils.WMCreateSyncReader(IntPtr.Zero, 0, out this.reader);
                    this.reader.Open(this.FileName);
                }

                if (this.changePosition)
                {
                    this.reader.SetRange((long)this.currentPosition.TotalMilliseconds*10000, 0);
                    this.readData.Clear();
                    this.changePosition = false;
                }

                if (!this.readData.Any() || this.readData.First().Data.Length >= dataRequested)
                {
                    var sample = this.ReadSampleData(dataRequested);
                    if (sample.Data.Length > 0)
                    {
                        if (!this.readData.Any())
                        {
                            this.readData.Add(sample);
                        }
                        else
                        {
                            this.readData.First().Combine(sample);
                        }
                    }
                }
                else
                {
                    Trace.WriteLine(string.Format("Requested: {0} Data Stored:{1}", dataRequested, this.readData.First().Data.Length));
                }

                if (this.readData.Any())
                {
                    var readData = this.readData.First();
                    this.readData.Remove(readData);
                    returnSample.DataTotalLength = (long)(this.metadata.Duration.TotalSeconds * 10000000);

                    if (dataRequested >= readData.Data.Length)
                    {
                        returnSample.Data = new byte[readData.Data.Length];
                        returnSample.DataLength = readData.Data.Length;
                        returnSample.DataOffset = readData.SampleTime;
                        Array.Copy(readData.Data, 0, returnSample.Data, 0, readData.Data.Length);
                    }
                    else
                    {
                        returnSample.Data = new byte[dataRequested];
                        returnSample.DataLength = dataRequested;
                        returnSample.DataOffset = readData.SampleTime;
                        Array.Copy(readData.Data, 0, returnSample.Data, 0, dataRequested);

                        var newData = new SampleData(readData.Data.Length - dataRequested)
                        {
                            SampleTime = readData.SampleTime
                        };

                        Array.Copy(readData.Data, dataRequested, newData.Data, 0, readData.Data.Length - dataRequested);
                        this.readData.Insert(0, newData);
                    }
                }
            }

            return returnSample;
        }

        public IMetadata GetMetadata()
        {
            if (this.metadata == null)
            {
                try
                {
                    IWMSyncReader metadataReader;
                    WMUtils.WMCreateSyncReader(IntPtr.Zero, 0, out metadataReader);
                    metadataReader.Open(this.FileName);
                    this.metadata = new SimpleMetadataSource(new WmaMetadataSource((IWMHeaderInfo3)metadataReader, this.FileName));
                    metadataReader.Close();
                }
                catch (COMException)
                {
                    this.metadata = new SimpleMetadataSource
                    {
                        Album = "Unknown",
                        Artist = "Unknown",
                        Title = this.FileName
                    };
                }
            }

            return this.metadata; 
        }


        private SampleData ReadSampleData(int dataRequested)
        {
            var sample = new SampleData();
            while (sample.Data.Length < dataRequested && !this.EndOfFile)
            {
                try
                {
                    INSSBuffer buffer;
                    long sampleTime;
                    long sampleDuration;
                    SampleFlag flags;
                    int outputNumber;
                    short streamNumber;

                    this.reader.GetNextSample(0, out buffer, out sampleTime, out sampleDuration, out flags, out outputNumber, out streamNumber);

                    if (sampleDuration > 0)
                    {
                        IntPtr data = IntPtr.Zero;
                        int length = 0;

                        buffer.GetBufferAndLength(out data, out length);

                        Trace.WriteLine(string.Format("Requested: {0} Data Read:{1}", dataRequested, length));

                        this.currentPosition = TimeSpan.FromMilliseconds(sampleTime / 10000);

                        // Add data
                        var newData = new SampleData(length) { SampleTime = sampleTime };
                        Marshal.Copy(data, newData.Data, 0, length);
                        sample.Combine(newData);
                    }
                }
                catch (COMException e)
                {
                    if ((uint)e.ErrorCode != 0xC00D0BCF)
                    {
                        throw;
                    }

                    this.EndOfFile = true;
                }
            }

            return sample;
        }
    }
}
