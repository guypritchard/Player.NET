namespace DJPad.Core.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class SampleData
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
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
    }

    public class SampleBuffer
    {
        private List<SampleData> readData = new List<SampleData>();

        public void Clear()
        {
            this.readData.Clear();
        }

        public bool HasData
        {
            get
            {
                return this.readData.Any();
            }
        }

        public int Length
        {
            get
            {
                return this.readData.Any() ? this.readData.First().Data.Count() : 0;
            }
        }

        public void Add(SampleData sample)
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

        private byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        internal Sample GetData(int dataRequested)
        {
            var returnSample = new Sample();

            var readData = this.readData.First();
            this.readData.Remove(readData);

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

            return returnSample;
        }
    }
}
