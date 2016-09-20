namespace DJPad.Core
{
    using System;
    using System.Diagnostics;

    using DJPad.Core.Interfaces;

    public class CircularBuffer : ISampleSource, ISampleConsumer
    {
        #region Constants

        private const int DefaultSize = 256 * 1024;

        #endregion

        #region Fields

        private readonly byte[] buffer;

        private readonly int bufferSize;

        private readonly int refillBufferAt;

        private readonly int startSize;

        private int dataRemaining;

        private int readCursor;

        private int writeCursor;

        #endregion

        #region Constructors and Destructors

        public CircularBuffer(int bufferSize)
        {
            this.bufferSize = bufferSize;
            this.buffer = new byte[bufferSize];
            this.refillBufferAt = bufferSize / 2;
            this.startSize = this.refillBufferAt + (this.refillBufferAt / 2);
        }

        public CircularBuffer()
            : this(DefaultSize)
        {
        }

        #endregion

        #region Public Properties

        public ISampleSource SampleSource { get; set; }

        #endregion

        #region Public Methods and Operators

        public FormatInformation GetFormat()
        {
            return this.SampleSource.GetFormat();
        }

        public Sample GetSample(int dataRequested)
        {
            Trace.WriteLine(
                string.Format(
                    "PlayCursor: {0} WriteCursor: {1}\r\nDataRemaining: {2} DataRequested: {3}",
                    this.readCursor,
                    this.writeCursor,
                    this.dataRemaining,
                    dataRequested));

            this.MaintainBuffer();

            var s = new Sample();

            int amtReturned = dataRequested;

            // If we don't have enough data to serve - just give what we've got.
            if (this.dataRemaining < dataRequested)
            {
                amtReturned = this.dataRemaining;
            }

            s.Data = new byte[amtReturned];
            s.DataLength = amtReturned;

            if (this.readCursor + amtReturned > this.bufferSize)
            {
                int firstCopyAmount = this.bufferSize - this.readCursor;
                int secondCopyAmount = amtReturned - firstCopyAmount;
                Array.Copy(this.buffer, this.readCursor, s.Data, 0, firstCopyAmount);
                Array.Copy(this.buffer, 0, s.Data, firstCopyAmount, secondCopyAmount);
                this.readCursor = secondCopyAmount;
            }
            else
            {
                Array.Copy(this.buffer, this.readCursor, s.Data, 0, amtReturned);
                this.readCursor += amtReturned;
            }

            s.Format = this.GetFormat();

            this.dataRemaining -= amtReturned;

            return s;
        }

        #endregion

        #region Methods

        private void FillBuffer()
        {
            while (this.dataRemaining < this.startSize)
            {
                Sample s = this.SampleSource.GetSample(this.bufferSize - this.dataRemaining);

                // No data to read from upstream.
                if (s.DataLength == 0)
                {
                    return;
                }

                if (this.writeCursor + s.DataLength <= this.bufferSize)
                {
                    Array.Copy(s.Data, 0, this.buffer, this.writeCursor, s.DataLength);
                    this.writeCursor += s.DataLength;
                    Debug.WriteLine(string.Format("Filling buffer with {0} data", s.DataLength));
                }
                else
                {
                    int firstCopyAmount = s.DataLength - this.writeCursor;
                    int secondCopyAmount = s.DataLength - firstCopyAmount;

                    Array.Copy(s.Data, 0, this.buffer, this.writeCursor, firstCopyAmount);
                    Debug.WriteLine(string.Format("Fill 1 with {0} data", firstCopyAmount));

                    Array.Copy(s.Data, firstCopyAmount, this.buffer, 0, secondCopyAmount);
                    Debug.WriteLine(string.Format("Fill 2 with {0} data", secondCopyAmount));

                    this.writeCursor = secondCopyAmount;
                }

                this.dataRemaining += s.DataLength;
            }
        }

        private void MaintainBuffer()
        {
            if (this.dataRemaining == 0 && this.readCursor == 0 && this.writeCursor == 0)
            {
                this.FillBuffer();
                return;
            }

            if (this.dataRemaining < this.refillBufferAt)
            {
                int readAmount;
                if (this.writeCursor <= this.readCursor)
                {
                    readAmount = this.readCursor - this.writeCursor;
                }
                else
                {
                    readAmount = this.writeCursor - this.readCursor;
                }

                Sample s = this.SampleSource.GetSample(readAmount);

                // No data to read from upstream.
                if (s.DataLength == 0)
                {
                    Trace.WriteLine("No more data from upstream.");
                    return;
                }

                if (this.writeCursor + s.DataLength <= this.bufferSize)
                {
                    Array.Copy(s.Data, 0, this.buffer, this.writeCursor, s.DataLength);
                    this.writeCursor += s.DataLength;
                    Trace.WriteLine(string.Format("Refilling buffer with {0} data", s.DataLength));
                }
                else
                {
                    int firstCopyAmount = this.bufferSize - this.writeCursor;
                    int secondCopyAmount = s.DataLength - firstCopyAmount;

                    Array.Copy(s.Data, 0, this.buffer, this.writeCursor, firstCopyAmount);
                    Trace.WriteLine(string.Format("Refill 1 from {0} with {1} data", this.writeCursor, firstCopyAmount));

                    Array.Copy(s.Data, firstCopyAmount, this.buffer, 0, secondCopyAmount);
                    Trace.WriteLine(string.Format("Refill 2 from {0} with {1} data", 0, secondCopyAmount));
                    this.writeCursor = secondCopyAmount;
                }

                this.dataRemaining += s.DataLength;
            }
        }

        #endregion
    }
}