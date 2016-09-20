namespace DJPad.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Sample
    {
        public enum SampleState
        {
            Play,
            Seek,
            Stop,
        }

        public enum Channel
        {
            Both,
            Left,
            Right,
        };

        #region Constants

        public const int Sample16 = 2;

        #endregion

        #region Fields

        public byte[] Data;

        #endregion

        #region Constructors and Destructors

        public Sample()
        {
        }

        public Sample(int size)
        {
            this.Data = new byte[size];
            this.DataLength = size;
        }

        #endregion

        #region Public Properties

        public SampleState State { get; set; }

        public int DataLength { get; set; }

        public long DataOffset { get; set; }

        public long DataTotalLength { get; set; }

        public FormatInformation Format { get; set; }

        public TimeSpan PresentationTime
        {
            get
            {
                if (this.DataTotalLength == 0)
                {
                    return TimeSpan.Zero;
                }

                return TimeSpan.FromSeconds(this.TotalTime.TotalSeconds * ((float)this.DataOffset / this.DataTotalLength));
            }
        }

        public TimeSpan TotalTime { get; set; }

        public bool IsEmpty
        {
            get
            {
                return this.DataLength == 0;
            }
        }

        #endregion

        #region Public Methods and Operators

        public Sample Clone()
        {
            var s = new Sample
                    {
                        Data = new byte[this.Data.Length], 
                        DataLength = this.DataLength,
                        Format = this.Format,
                    };

            Array.Copy(this.Data, s.Data, this.Data.Length);

            return s;
        }

        public IEnumerable<short> Sample16BitStereo()
        {
            int bufferPosition = 0;
            while (bufferPosition < this.DataLength - 1)
            {
                short sample = BitConverter.ToInt16(this.Data, bufferPosition);
                bufferPosition += 2;
                yield return sample;
            }
        }
        
        public float[] ToFftArray(Channel channels)
        {
            var returnArray = new float[this.DataLength / 4];

            int readPosition = 0;

            for (int i = 0; i < returnArray.Length; i++)
            {
                var left = BitConverter.ToInt16(this.Data, readPosition);
                readPosition += 2;
                var right = BitConverter.ToInt16(this.Data, readPosition);

                if (channels == Channel.Left)
                {
                    returnArray[i] = left;
                }
                else if (channels == Channel.Right)
                {
                    returnArray[i] = right;
                }
                else
                {
                    returnArray[i] = (left + right)/2.0f;
                }
            }

            return returnArray;
        }

        #endregion

        #region Methods

        public void Resize(int dataRead)
        {
            if (this.Data != null)
            {
                Array.Resize(ref this.Data, dataRead);
                this.DataLength = dataRead;
            }
            else
            {
                throw new InvalidOperationException("Unable to resize unallocated array.");
            }
        }

        public void Split(out double[] leftSpect, out double[] rightSpect)
        {
            leftSpect = new double[this.DataLength / 4];
            rightSpect = new double[this.DataLength / 4];

            for (int i = 0; i < this.DataLength; i += 4)
            {
                leftSpect[i] = BitConverter.ToInt16(this.Data, i);
                rightSpect[i] = BitConverter.ToInt16(this.Data, i + 2);
            }
        }

        /// <summary>
        ///     An inplace transformation of current 16bit data.
        /// </summary>
        /// <param name="a"></param>
        public void Transform16(Func<short> a)
        {
            for (int i = 0; i < this.DataLength; i += Sample16)
            {
                byte[] data = BitConverter.GetBytes(a());
                this.Data[i] = data[0];
                this.Data[i + 1] = data[1];
            }
        }

        public void Write16(IEnumerable<short> a)
        {
            for (int i = 0; i < this.DataLength; i += Sample16)
            {
                short val = a.Take(1).FirstOrDefault();
                byte[] data = BitConverter.GetBytes(val);
                this.Data[i] = data[0];
                this.Data[i + 1] = data[1];
            }
        }

        #endregion
    }
}